using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Threading.RateLimiting;

namespace api.Models
{
    /** application config model */
    public class Configuration
    {
        private TimeSpan SessionTimout = TimeSpan.FromMinutes(30);
        private string? JWTSecretKey { get; set; }
        private string? JWTIssue { get; set; }
        private string? JWTAudience { get; set; }
        private string? EncryptionKey { get; set; }

        /** model data */
        public List<WebCor> Cors = new List<WebCor>()
        {
            new WebCor() { Name = "AllowAngularOrigins", Link = "http://localhost:4200" }
        };
        public Dictionary<ApplicationENUM.DATABASE_CONNECTION, object>? DatabaseConnections { get; set; }



        /** configuration model */
        public Configuration(WebApplicationBuilder webBuilder)
        {
            JWTSecretKey = GetValue(webBuilder, "JWT:SecretSupperKey");
            JWTIssue = GetValue(webBuilder, "JWT:Issues");
            JWTAudience = GetValue(webBuilder, "JWT:Audience");
            EncryptionKey = GetValue(webBuilder, "Encryption:key");

            if (string.IsNullOrEmpty(JWTSecretKey)) Console.WriteLine("Missing JWT supper key");
            if (string.IsNullOrEmpty(JWTIssue)) Console.WriteLine("Missing JWT issue");
            if (string.IsNullOrEmpty(JWTAudience)) Console.WriteLine("Missing JWT audience");
            if (string.IsNullOrEmpty(EncryptionKey)) Console.WriteLine("Missing encryption key");

            if (string.IsNullOrEmpty(JWTSecretKey)
                || string.IsNullOrEmpty(JWTIssue)
                || string.IsNullOrEmpty(JWTAudience)
                || string.IsNullOrEmpty(EncryptionKey)) return;
            Console.WriteLine($"Got JWT key: {JWTSecretKey}");
            Console.WriteLine($"Got JWT issue: {JWTIssue}");
            Console.WriteLine($"Got JWT audience: {JWTAudience}");
            Console.WriteLine($"Got encryption key: {EncryptionKey}");


            AddJWT(webBuilder);
            AddRateLimit(webBuilder);
            AddSession(webBuilder);
            AddCor(webBuilder);
        }





        /** adding reuse database */
        public async Task AddDatabaseConnections(WebApplicationBuilder webBuilder)
        {
            /** create new database connection list */
            DatabaseConnections = new Dictionary<ApplicationENUM.DATABASE_CONNECTION, object>();

            /** postgresql company management */
            NpgsqlConnection? PostgreSQLCompanymanagement = await SetDatabaseConnection_POSTGRESQL(webBuilder, "ConnectionStrings:PostgreSQL_CompanyManagement");
            if (PostgreSQLCompanymanagement != null) DatabaseConnections.Add(ApplicationENUM.DATABASE_CONNECTION.POSTGRESQL_COMPANY_MANAGEMENT, PostgreSQLCompanymanagement);
        }
        /** get database connected */
        public async Task<NpgsqlConnection>? GetDatabaseConnection_POSTGRESQL(ApplicationENUM.DATABASE_CONNECTION connectionNode)
        {
            NpgsqlConnection getConnection = new NpgsqlConnection();
            if (DatabaseConnections == null) return getConnection;
            if (!DatabaseConnections.ContainsKey(connectionNode)) return getConnection;
            object getObject = DatabaseConnections[connectionNode];
            try
            {
                getConnection = (NpgsqlConnection)getObject;
                if (getConnection.State != System.Data.ConnectionState.Open) await getConnection.OpenAsync();
            }
            catch { }
            return getConnection;
        }






        /** getting value from configuration by configuration key */
        private string GetValue(WebApplicationBuilder webBuilder, string key) => (webBuilder.Configuration[key]?.ToString() ?? string.Empty).Trim();

        /** add jwt to web service */
        private void AddJWT(WebApplicationBuilder webBuilder)
        {
            if (string.IsNullOrEmpty(EncryptionKey)) return;
            // add JWT
            webBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JWTIssue,
                    ValidAudience = JWTAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EncryptionKey))
                };
            });
            webBuilder.Services.AddAuthorization();
        }
        
        /** add rate limit */
        private void AddRateLimit(WebApplicationBuilder webBuilder)
        {
            webBuilder.Services.AddRateLimiter(options =>
            {
                /** global limit */
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
                    factory: _ => new FixedWindowRateLimiterOptions {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

                /** rate limit in name policy */
                options.AddFixedWindowLimiter("FastPolicy", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromSeconds(10);
                    opt.QueueLimit = 0;
                });

                /** too many request */
                options.OnRejected = (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.WriteAsync("Rate limit exceeded.");
                    return new ValueTask();
                };

                /** window limit */
                options.AddFixedWindowLimiter("ApiPolicy", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromSeconds(10);
                });
            });
        }
        /** add session */
        private void AddSession(WebApplicationBuilder webBuilder)
        {
            webBuilder.Services.AddSession(options =>
            {
                options.IdleTimeout = SessionTimout;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            webBuilder.Services.AddDistributedMemoryCache();
        }
        /** add cor */
        private void AddCor(WebApplicationBuilder webBuilder)
        {
            webBuilder.Services.AddCors(options =>
            {
                Cors.ForEach(getCor =>
                {
                    options.AddPolicy(getCor.Name, builder =>
                    {
                        builder.WithOrigins(getCor.Link)
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                    });
                });
            });
        } 
        /** connection database */
        private async Task<NpgsqlConnection?> SetDatabaseConnection_POSTGRESQL(WebApplicationBuilder webBuilder, string configurationKey)
        {
            string databaseConnectionString = GetValue(webBuilder, configurationKey);
            if (string.IsNullOrEmpty(databaseConnectionString)) return null;
            NpgsqlConnection databaseConnection = new NpgsqlConnection(databaseConnectionString);
            try
            {
                await databaseConnection.OpenAsync();
                Console.WriteLine($"Database connected: {databaseConnectionString}");
            }
            catch (Exception getError)
            {
                Console.WriteLine($"Database connection error: {configurationKey}");
                Console.WriteLine(getError.Message);
            }
            return databaseConnection;
        }
    }
}
