using System.Data;
using System.Data.SqlClient;

namespace DIT.IntegrationService.Data
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
            => _connectionString = connectionString;

        public IDbConnection CreateOpenConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(_connectionString);
            sqlConnection.Open();
            return sqlConnection;
        }
    }
}
