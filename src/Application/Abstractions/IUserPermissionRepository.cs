using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IUserPermissionRepository
    {
        Task<bool> HasRightAsync(int userId, string rightCode, CancellationToken cancellationToken = default);
    }
}
