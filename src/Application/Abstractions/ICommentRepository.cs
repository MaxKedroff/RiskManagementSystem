using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface ICommentRepository
    {
        Task<int> CreateAsync(int authorId, string text, string entityType, int entityId, CancellationToken cancellationToken = default);
    }
}
