using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence.Dapper;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "The SQL Server connection string is required.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}