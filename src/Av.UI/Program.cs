namespace Av.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // This placeholder entrypoint exists so `dotnet publish src/Av.UI/Av.UI.csproj`
        // can produce a Windows executable while the TypeScript UI is hosted by a
        // separate frontend runtime/electron/webview layer.
        Console.WriteLine("Av.UI executable scaffold generated successfully.");
    }
}
