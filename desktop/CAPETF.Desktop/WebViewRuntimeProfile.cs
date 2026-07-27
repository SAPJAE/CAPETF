using System.IO;
using Microsoft.Web.WebView2.Core;

namespace CAPETF.Desktop;

internal static class WebViewRuntimeProfile
{
    internal static string GetUserDataFolder(string? localAppData = null)
    {
        var root = string.IsNullOrWhiteSpace(localAppData)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : localAppData;
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Local application data is unavailable.");
        return Path.GetFullPath(Path.Combine(root, "CAPETF", "WebView2"));
    }

    internal static async Task<CoreWebView2Environment> CreateEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: GetUserDataFolder());
        cancellationToken.ThrowIfCancellationRequested();
        return environment;
    }
}
