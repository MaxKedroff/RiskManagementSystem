using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data.Common;

namespace Infrastructure.Database
{
    internal sealed class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly NpgsqlDataSource _dataSource;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Не задана строка подключения ConnectionStrings:Default.");
            }

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            _dataSource = builder.Build();
        }

        public async ValueTask<DbConnection> CreateOpenConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dataSource.OpenConnectionAsync(cancellationToken);
        }
    }
}
