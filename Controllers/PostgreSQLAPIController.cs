using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace api.Controllers
{
    [Route("api/database/postgresql")]
    [ApiController]
    public class PostgreSQLAPIController : ControllerBase
    {
        /*
        [HttpGet]
        [Route("get-version")]
        public async Task<IActionResult> GetVersion()
        {
            NpgsqlConnection? getConn = await Program.configuration.GetDatabaseConnection_POSTGRESQL(Models.ApplicationENUM.DATABASE_CONNECTION.POSTGRESQL_COMPANY_MANAGEMENT);
            if (getConn == null) return Ok(new { result = 0, message = "Not found database" });
        }
        */
    }
}
