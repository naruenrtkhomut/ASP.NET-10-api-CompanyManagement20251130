using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace api.Controllers
{
    [Route("api/admin")]
    [EnableRateLimiting("FastPolicy")]
    [ApiController]
    public class AdminAPIController : ControllerBase
    {
        private readonly ILogger<AdminAPIController> logger;
        public AdminAPIController(ILogger<AdminAPIController> inLogger)
        {
            logger = inLogger;
        }

        /** admin login */
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> GetLogin([FromBody] Models.Data.UserLogin? inUserLogin)
        {
            if (Program.configuration == null) return Ok(new { result = 0, message = "Web application invalid configuration" });
            if (inUserLogin == null) return Ok(new { result = 0, message = "Username and Password is null" });
            inUserLogin.Username = (inUserLogin.Username ?? "").Trim();
            inUserLogin.Password = (inUserLogin.Password ?? "").Trim();
            if (string.IsNullOrEmpty(inUserLogin.Username)) return Ok(new { result = 0, message = "Username is null or empty" });
            if (string.IsNullOrEmpty(inUserLogin.Password)) return Ok(new { result = 0, message = "Password is null or empty" });
            inUserLogin.Password = Program.configuration.GeneratePassword(inUserLogin.Password);
            if (string.IsNullOrEmpty(inUserLogin.Password)) return Ok(new { result = 0, message = "Password invalid in format" });
            int inMode = new Models.Modeling.Database.PSQL.MAIN.PRC.INIT.GETTING().ADMIN_LOGIN;
            JObject getData = await new Models.Database.POSTGRESQL().Prc_Admin(logger, inMode, new JObject()
            {
                { "username", Program.configuration.GetEncryption(inUserLogin.Username) },
                { "password", Program.configuration.GetEncryption(inUserLogin.Password) }
            });
            int getResult = int.TryParse((getData["result"] ?? "").ToObject<string>() ?? "", out getResult) ? getResult : 0;
            string getMessage = (getData["message"] ?? "").ToObject<string>() ?? "";
            if (getResult != 1) return Ok(new { result = getResult, message = "Admin login failed" });
            string getJWTToken = Program.configuration.GenerateJWT(inUserLogin.Username, getMessage);
            return Ok(new { result = getResult, message = getJWTToken });
        }


        /** getting postgresql server version */
        [Authorize(Roles = "admin")]
        [HttpGet]
        [Route("postgresql/server-version")]
        public async Task<IActionResult> GetPSQL_SERVER_VERSION()
        {
            int inMode = new Models.Modeling.Database.PSQL.MAIN.PRC.INIT.GETTING().SERVER_VERSION;
            logger.LogInformation($"{Request.HttpContext.Connection.RemoteIpAddress?.ToString()}: Admin mode(get database version PSQLMain)");
            JObject getData = await new Models.Database.POSTGRESQL().Prc_Admin(logger, inMode);
            int getResult = int.TryParse((getData["result"] ?? "").ToObject<string>() ?? "", out getResult) ? getResult : 0;
            return Ok(new { result = getResult, message = (getData["message"] ?? "").ToObject<string>() ?? "" });
        }
    }
}
