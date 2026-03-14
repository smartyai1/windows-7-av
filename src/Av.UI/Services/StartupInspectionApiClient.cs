using Av.Agent.StartupInspection;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Av.UI.Services;

public sealed class StartupInspectionApiClient(HttpClient httpClient)
{
    public async Task<StartupInspectionReport?> GetFindingsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<StartupInspectionReport>("api/startup-inspection/findings", cancellationToken);
    }

    public async Task<RemediationResult?> DisableEntryAsync(string entryId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/startup-inspection/entries/{entryId}/disable", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RemediationResult>(cancellationToken: cancellationToken);
    }

    public async Task<RemediationResult?> QuarantineTargetAsync(string entryId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/startup-inspection/entries/{entryId}/quarantine", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RemediationResult>(cancellationToken: cancellationToken);
    }

    public async Task<RemediationResult?> RestoreBackupAsync(string entryId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/startup-inspection/entries/{entryId}/restore", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RemediationResult>(cancellationToken: cancellationToken);
    }
}
