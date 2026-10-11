using System.Data.Common;

namespace Infrastructure.Database
{
    public interface IDbConnectionFactory
    {
        ValueTask<DbConnection> CreateOpenConnectionAsync(
            CancellationToken cancellationToken = default);
    }
}
