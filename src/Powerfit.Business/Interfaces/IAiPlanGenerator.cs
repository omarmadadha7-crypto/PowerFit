using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.AI;

namespace Powerfit.Business.Interfaces;

public interface IAiPlanGenerator
{
    /// <summary>
    /// Calls the AI model and validates its answer. It does NOT throw for provider problems:
    /// it returns an outcome with Status Failed / Timeout so the caller can log it in AIGenerationLog.
    /// </summary>
    Task<AiGenerationOutcome> GenerateAsync(AiPlanRequest request, CancellationToken ct = default);
}
