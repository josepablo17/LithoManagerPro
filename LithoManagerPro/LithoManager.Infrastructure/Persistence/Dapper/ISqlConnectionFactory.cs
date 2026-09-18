using System.Data.Common;

namespace LithoManager.Infrastructure.Persistence.Dapper;

public interface ISqlConnectionFactory
{
    DbConnection CreateConnection();
}
