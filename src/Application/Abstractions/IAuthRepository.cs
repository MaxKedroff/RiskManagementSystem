using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public sealed record AuthUser(
    int UserId,
    string Email,
    string PasswordHash,
    IReadOnlyList<string> Roles);


    public interface IAuthRepository
    {
        Task<AuthUser?> FindByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
    }
}
