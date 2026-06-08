using EstateSync.Api.Interfaces.Services.Auth;

namespace EstateSync.Api.Services.Auth;

public class PasswordService : IPasswordService
{
    private const int WORK_FACTOR = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WORK_FACTOR);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
