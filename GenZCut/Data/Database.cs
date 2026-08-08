using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GenZCut.Data
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection was not found in appsettings.json.");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}