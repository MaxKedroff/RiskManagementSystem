using Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security
{
    public sealed class PasswordHashService : IPasswordHashService
    {
        private readonly PasswordHasher<object> _hasher = new();

        public bool Verify(string password, string passwordHash)
        {
            var result = _hasher.VerifyHashedPassword(
                new object(),
                passwordHash,
                password);

            return result != PasswordVerificationResult.Failed;
        }

        public string Hash(string password)
        {
            return _hasher.HashPassword(new object(), password);
        }
    }
}
