using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Signatures;

public sealed class UpdaterClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public UpdaterClient(Uri baseAddress, TlsPinningValidator tlsPinningValidator)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, sslPolicyErrors) =>
            {
                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    return false;
                }

                return tlsPinningValidator.ValidateCertificate(cert as X509Certificate2);
            }
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<SignedUpdatePackage> GetUpdatePackageAsync(string manifestPath, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(manifestPath, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var package = await JsonSerializer.DeserializeAsync<SignedUpdatePackage>(stream, cancellationToken: ct).ConfigureAwait(false);
        return package ?? throw new InvalidDataException("Could not deserialize signed update package.");
    }

    public async Task DownloadArtifactAsync(string remotePath, string destinationPath, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var response = await _httpClient.GetAsync(remotePath, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
