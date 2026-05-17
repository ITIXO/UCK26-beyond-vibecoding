using System.Net.Http.Json;
using System.Text.Json;

namespace UCK26.Ui.Tests;

internal sealed record AdminStorageState(string Token, string UserName, string Role);

internal static class AuthSetup
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static AdminStorageState? _cached;

    public static async Task<AdminStorageState> GetAdminStorageStateAsync(bool _ = true)
    {
        if (_cached is not null) return _cached;

        await Gate.WaitAsync();
        try
        {
            if (_cached is not null) return _cached;

            using var client = new HttpClient { BaseAddress = new Uri(TestConfig.ApiUrl) };
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                userName = TestConfig.AdminUserName,
                password = TestConfig.AdminPassword
            });

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to obtain admin JWT from {TestConfig.ApiUrl}/api/auth/login (status {(int)response.StatusCode}). " +
                    "Ensure the API is running and admin credentials are correct.");
            }

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = body.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("Login response missing accessToken.");
            var user = body.GetProperty("user");
            var name = user.GetProperty("userName").GetString() ?? TestConfig.AdminUserName;
            var role = user.GetProperty("role").GetString() ?? "Admin";

            _cached = new AdminStorageState(token, name, role);
            return _cached;
        }
        finally
        {
            Gate.Release();
        }
    }
}
