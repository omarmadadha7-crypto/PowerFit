using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Powerfit.API.Responses;

/// <summary>
/// One error shape for the whole API (same field names as ProblemDetails):
/// { "status": 404, "title": "Not found", "detail": "...", "errors": { "field": ["..."] }, "traceId": "..." }
/// </summary>
public sealed record ErrorResponse(
    int Status,
    string Title,
    string Detail,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors,
    string? TraceId)
{
    public static ErrorResponse From(
        HttpContext context,
        int status,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(status, title, detail, errors, Activity.Current?.Id ?? context.TraceIdentifier);

    /// <summary>Writes this error as the JSON body of the response (also sets the status code).</summary>
    public Task WriteToAsync(HttpContext context, CancellationToken ct = default)
    {
        context.Response.StatusCode = Status;
        return context.Response.WriteAsJsonAsync(this, ct);
    }
}
