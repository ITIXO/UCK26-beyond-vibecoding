namespace UCK26.Ui.Tests;

internal static class TestConfig
{
    public static string FrontendUrl =>
        Environment.GetEnvironmentVariable("UCK26_FRONTEND_URL")?.TrimEnd('/')
        ?? "http://localhost:3000";

    public static string ApiUrl =>
        Environment.GetEnvironmentVariable("UCK26_API_URL")?.TrimEnd('/')
        ?? "http://localhost:5080";

    public static string AdminUserName =>
        Environment.GetEnvironmentVariable("UCK26_ADMIN_USER") ?? "admin";

    public static string AdminPassword =>
        Environment.GetEnvironmentVariable("UCK26_ADMIN_PASSWORD") ?? "Demo!2026";

    public static string Route(string path)
    {
        if (string.IsNullOrEmpty(path)) return FrontendUrl;
        return path.StartsWith('/') ? $"{FrontendUrl}{path}" : $"{FrontendUrl}/{path}";
    }
}
