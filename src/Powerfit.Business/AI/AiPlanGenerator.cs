using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Settings;

namespace Powerfit.Business.AI;

/// <summary>
/// Builds the prompt, calls the model (Anthropic Messages API), extracts the JSON answer and validates it.
/// To use another provider only change <see cref="CallModelAsync"/> and the body built in
/// <see cref="BuildRequestJson"/>; everything else is provider independent.
/// Registered as a typed HttpClient (see DependencyInjection.cs).
/// </summary>
public sealed class AiPlanGenerator(
    HttpClient http,
    IOptions<AiSettings> options,
    ILogger<AiPlanGenerator> logger) : IAiPlanGenerator
{
    private readonly AiSettings _settings = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AiGenerationOutcome> GenerateAsync(AiPlanRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Ai:ApiKey is not configured.");

        // Keep the prompt small: spread the catalogs evenly over muscle groups / food categories.
        var limited = request with
        {
            Exercises = Spread(request.Exercises, e => e.MuscleGroup, _settings.MaxExercisesInPrompt),
            Foods = Spread(request.Foods, f => f.Category, _settings.MaxFoodsInPrompt)
        };

        var requestJson = BuildRequestJson(limited);
        string? responseJson = null;

        try
        {
            var raw = await CallModelAsync(requestJson, ct);
            if (raw.Error is not null)
                return Failed(requestJson, null, raw.Error);

            var (text, truncated) = ReadText(raw.Body!);
            if (truncated)
                return Failed(requestJson, null, "The AI answer was cut off (max tokens reached). Please try again.");

            responseJson = ExtractJsonObject(text);
            if (responseJson is null)
                return Failed(requestJson, null, "The AI answer did not contain a valid JSON object.");

            var draft = JsonSerializer.Deserialize<AiPlanDraft>(responseJson, JsonOptions);
            AiPlanValidator.Normalize(draft);

            var errors = AiPlanValidator.Validate(draft, limited);
            if (errors.Count > 0)
                return Failed(requestJson, responseJson, "The AI returned an invalid plan: " + string.Join(" ", errors.Take(5)));

            return new AiGenerationOutcome(
                GenerationStatuses.Success, _settings.Model, requestJson, responseJson, draft, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient.Timeout elapsed (the caller did not cancel).
            logger.LogWarning("AI request timed out after {Seconds}s.", _settings.TimeoutSeconds);
            return new AiGenerationOutcome(
                GenerationStatuses.Timeout, _settings.Model, requestJson, null, null,
                "The AI provider took too long to answer.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not reach the AI provider.");
            return Failed(requestJson, null, "Could not reach the AI provider.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "The AI answer could not be parsed.");
            return Failed(requestJson, responseJson, "The AI answer could not be parsed into a plan.");
        }
    }

    // ------------------------------------------------------------------------
    //  Provider call (Anthropic Messages API)
    // ------------------------------------------------------------------------

    private async Task<(string? Body, string? Error)> CallModelAsync(string requestJson, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        message.Headers.Add("x-api-key", _settings.ApiKey);
        message.Headers.Add("anthropic-version", _settings.AnthropicVersion);

        using var response = await http.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI provider returned HTTP {Status}.", (int)response.StatusCode);
            return (null, $"The AI provider returned HTTP {(int)response.StatusCode}.");
        }

        return (body, null);
    }

    private string BuildRequestJson(AiPlanRequest request)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = _settings.Model,
            ["max_tokens"] = _settings.MaxTokens,
            ["system"] = BuildSystemPrompt(request),
            ["messages"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = BuildUserPrompt(request)
                }
            }
        };

        return JsonSerializer.Serialize(body, JsonOptions);
    }

    /// <summary>Concatenates the text blocks of the answer. Truncated = the model hit max_tokens.</summary>
    private static (string Text, bool Truncated) ReadText(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        var truncated = root.TryGetProperty("stop_reason", out var stop)
                        && stop.ValueKind == JsonValueKind.String
                        && stop.GetString() == "max_tokens";

        var sb = new StringBuilder();
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type)
                    && type.GetString() == "text"
                    && block.TryGetProperty("text", out var text))
                {
                    sb.Append(text.GetString());
                }
            }
        }

        return (sb.ToString(), truncated);
    }

    /// <summary>Takes everything between the first '{' and the last '}' (also removes ``` fences).</summary>
    private static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        var candidate = text[start..(end + 1)];
        return AigenerationLog.IsValidJson(candidate) ? candidate : null;
    }

    private AiGenerationOutcome Failed(string requestJson, string? responseJson, string error) =>
        new(GenerationStatuses.Failed, _settings.Model, requestJson, responseJson, null, error);

    // ------------------------------------------------------------------------
    //  Prompts
    // ------------------------------------------------------------------------

    private static string BuildSystemPrompt(AiPlanRequest r)
    {
        var lines = new List<string>
        {
            "You are an expert strength & conditioning coach and sports dietitian.",
            "Create a personalised weekly fitness plan (training + nutrition) for the user described in the user message.",
            "",
            "OUTPUT RULES",
            "- Reply with ONE JSON object only: no markdown, no code fences, no commentary.",
            "- Use ONLY exercises and foods from the supplied catalogs and reference them by their numeric \"id\" (exerciseId / foodId).",
            $"- Every free-text \"notes\" value must be written in {LanguageName(r.Language)} and be short (max 300 characters).",
            "- All numbers must be plain numbers (no units, no ranges, no strings).",
            "",
            "TRAINING RULES",
            $"- Use exactly {r.DaysPerWeek} distinct training days. dayNumber is 1-7 (day of the week). Spread the days over the week and avoid training the same muscle group on consecutive days.",
            "- 4 to 8 exercises per training day, in a logical order (compound lifts first). orderIndex starts at 1 on every day.",
            "- Respect the user's injuries, experience level, training location / equipment and goal. Never choose an exercise that stresses an injured area.",
            "- sets: 1-20. reps: 1-100 (for timed exercises use the number of seconds as reps).",
            "",
            "NUTRITION RULES",
            "- Base dailyCalories on estimatedTdee and the goal (Fat Loss: about 15-20% deficit, Muscle Gain: about 10-15% surplus, Strength/Fitness: about maintenance). Never go below 1200 kcal.",
            "- proteinG, carbsG and fatG are in grams. (4 x protein + 4 x carbs + 9 x fat) must be within 10% of dailyCalories.",
            "- Provide all 7 days (dayNumber 1-7). Every day has Breakfast, Lunch and Dinner and may have Snack items. mealType must be exactly one of: Breakfast, Lunch, Dinner, Snack.",
            "- quantityGrams is a number between 1 and 2000. Use the per-100g values in foodCatalog so that the real calories of every day land within 10% of dailyCalories.",
            "- Respect dietaryRestrictions. Prefer foods whose \"meals\" value contains the mealType (empty meals = any meal). Vary the foods across the days.",
            "",
            "JSON SHAPE",
            "{",
            "  \"exercisePlan\": {",
            "    \"notes\": \"string\",",
            "    \"items\": [ { \"dayNumber\": 1, \"orderIndex\": 1, \"exerciseId\": 0, \"sets\": 3, \"reps\": 10, \"notes\": \"string or null\" } ]",
            "  },",
            "  \"nutritionPlan\": {",
            "    \"dailyCalories\": 0, \"proteinG\": 0, \"carbsG\": 0, \"fatG\": 0,",
            "    \"items\": [ { \"dayNumber\": 1, \"mealType\": \"Breakfast\", \"foodId\": 0, \"quantityGrams\": 0 } ]",
            "  }",
            "}"
        };

        return string.Join("\n", lines);
    }

    private static string BuildUserPrompt(AiPlanRequest r)
    {
        var payload = new
        {
            profile = r.Profile,
            daysPerWeek = r.DaysPerWeek,
            additionalNotes = r.AdditionalNotes,
            exerciseCatalog = r.Exercises,
            foodCatalog = r.Foods
        };

        return "Create the plan for this user. Input data (JSON):\n" + JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string LanguageName(string code) => code.Trim().ToLowerInvariant() switch
    {
        "ar" => "Arabic",
        "en" => "English",
        "fr" => "French",
        "tr" => "Turkish",
        var other => other
    };

    /// <summary>Takes up to <paramref name="limit"/> items, round-robin over the groups (so no muscle group / category disappears).</summary>
    private static List<T> Spread<T>(IReadOnlyList<T> items, Func<T, string> groupKey, int limit)
    {
        if (limit <= 0 || items.Count <= limit) return items.ToList();

        var groups = items.GroupBy(groupKey).Select(g => new Queue<T>(g)).ToList();
        var result = new List<T>(limit);

        while (result.Count < limit && groups.Any(g => g.Count > 0))
        {
            foreach (var group in groups)
            {
                if (result.Count >= limit) break;
                if (group.Count > 0) result.Add(group.Dequeue());
            }
        }

        return result;
    }
}
