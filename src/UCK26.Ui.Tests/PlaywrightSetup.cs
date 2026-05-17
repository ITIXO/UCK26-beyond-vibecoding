namespace UCK26.Ui.Tests;

internal static class PlaywrightSetup
{
    private static readonly SemaphoreSlim InstallLock = new(1, 1);
    private static bool _installed;

    public static async Task EnsureInstalledAsync(params string[] browserNames)
    {
        if (_installed)
        {
            return;
        }

        await InstallLock.WaitAsync();
        try
        {
            if (_installed)
            {
                return;
            }

            foreach (var browser in browserNames)
            {
                var exitCode = Microsoft.Playwright.Program.Main(["install", browser]);
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Playwright install failed (exit {exitCode}) for '{browser}'.");
                }
            }
            _installed = true;
        }
        finally
        {
            InstallLock.Release();
        }
    }
}
