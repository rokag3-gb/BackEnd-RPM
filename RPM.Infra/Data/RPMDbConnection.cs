using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RPM.Infra.Data;

public class RPMDbConnection
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public RPMDbConnection(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("RPMDbConnection");
    }
    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}