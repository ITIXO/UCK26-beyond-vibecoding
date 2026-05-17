using Microsoft.AspNetCore.DataProtection;
using UCK26.Api.Persistence;

namespace UCK26.Api.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public class DataProtectionPasswordHasher(IDataProtectionProvider provider) : IPasswordHasher
{
    private const string Purpose = "UCK26.Api.Passwords.v1";
    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Hash(string password) => _protector.Protect(password);

    public bool Verify(string password, string hash)
    {
        if (hash == SeedData.AdminPasswordHash)
        {
            return password == SeedData.AdminPassword;
        }

        try
        {
            return _protector.Unprotect(hash) == password;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }
}
