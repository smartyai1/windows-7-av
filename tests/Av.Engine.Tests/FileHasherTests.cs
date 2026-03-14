using System.Security.Cryptography;
using System.Text;
using Av.Engine;

namespace Av.Engine.Tests;

public class FileHasherTests
{
    [Fact]
    public async Task ComputeSha256Async_ReturnsExpectedHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "hello-antivirus");
            var hasher = new FileHasher();

            var actual = await hasher.ComputeSha256Async(tempFile);

            var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes("hello-antivirus"));
            var expected = Convert.ToHexString(expectedBytes);
            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
