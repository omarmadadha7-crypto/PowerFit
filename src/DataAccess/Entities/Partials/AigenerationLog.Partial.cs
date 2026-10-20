using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DataAccess.Entities;

public partial class AigenerationLog
{
    [NotMapped] public bool IsSuccess => Status == GenerationStatuses.Success;
    [NotMapped] public bool IsFailed => Status == GenerationStatuses.Failed;
    [NotMapped] public bool IsTimeout => Status == GenerationStatuses.Timeout;

    /// <summary>
    /// Validates the same rules as the SQL CHECKs (ISJSON, status list, Success => response)
    /// so you get a clear exception instead of a SQL constraint error.
    /// </summary>
    public static AigenerationLog Create(
        int planId, string model, string requestJson, string? responseJson, string status)
    {
        if (planId <= 0)
            throw new ArgumentOutOfRangeException(nameof(planId));

        if (string.IsNullOrWhiteSpace(model) || model.Trim().Length > 50)
            throw new ArgumentException("Model is required (max 50 chars).", nameof(model));

        if (!IsValidJson(requestJson))
            throw new ArgumentException("RequestJson must be a valid JSON object or array.", nameof(requestJson));

        if (responseJson is not null && !IsValidJson(responseJson))
            throw new ArgumentException("ResponseJson must be a valid JSON object or array.", nameof(responseJson));

        if (status is not (GenerationStatuses.Success or GenerationStatuses.Failed or GenerationStatuses.Timeout))
            throw new ArgumentException("Status must be Success, Failed or Timeout.", nameof(status));

        if (status == GenerationStatuses.Success && responseJson is null)
            throw new ArgumentException("A Success log requires ResponseJson.", nameof(responseJson));

        return new AigenerationLog
        {
            PlanId = planId,
            Model = model.Trim(),
            RequestJson = requestJson,
            ResponseJson = responseJson,
            Status = status
        };
    }

    /// <summary>SQL ISJSON() only accepts objects/arrays by default, so we do the same.</summary>
    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
