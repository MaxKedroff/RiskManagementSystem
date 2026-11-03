using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IRiskEventRepository
    {
        /// <param name="authorId">NULL for system events.</param>
        Task AddAsync(int riskId, string kind, string text, int? authorId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RiskEventItem>> GetAllAsync(int? riskId, CancellationToken cancellationToken = default);
    }

    public sealed record RiskEventItem(
        int EventId,
        int RiskId,
        string Kind,
        string Text,
        int? AuthorId,
        string? AuthorName,
        string? AuthorPosition,
        DateTimeOffset CreatedAt);
}
