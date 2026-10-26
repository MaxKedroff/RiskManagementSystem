using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Risk;

namespace Application.Abstractions
{
    public interface ICommentRepository
    {
        Task<int> CreateAsync(int authorId, string text, string entityType, int entityId, CancellationToken cancellationToken = default);
        Task<RiskComment?> GetByIdAsync(int commentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RiskComment>> GetAllByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
        Task<bool> EditAsync(int commentId, string text, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(int commentId, CancellationToken cancellationToken = default);
    }
}
