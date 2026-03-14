using Av.Agent.StartupInspection;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Agent.Api;

[ApiController]
[Route("api/startup-inspection")]
public sealed class StartupFindingsController(IStartupInspectionService inspectionService) : ControllerBase
{
    [HttpGet("findings")]
    public Task<StartupInspectionReport> GetFindings(CancellationToken cancellationToken)
    {
        return inspectionService.ScanAsync(cancellationToken);
    }

    [HttpPost("entries/{entryId}/disable")]
    public Task<RemediationResult> DisableEntry([FromRoute] string entryId, CancellationToken cancellationToken)
    {
        return inspectionService.DisableEntryAsync(entryId, cancellationToken);
    }

    [HttpPost("entries/{entryId}/quarantine")]
    public Task<RemediationResult> QuarantineTarget([FromRoute] string entryId, CancellationToken cancellationToken)
    {
        return inspectionService.QuarantineTargetAsync(entryId, cancellationToken);
    }

    [HttpPost("entries/{entryId}/restore")]
    public Task<RemediationResult> RestoreBackup([FromRoute] string entryId, CancellationToken cancellationToken)
    {
        return inspectionService.RestoreBackupAsync(entryId, cancellationToken);
    }
}
