using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace api.Models
{
    /** application config model */
    public class Configuration
    {
        /** model data public */
        public List<WebCor> Cors = new List<WebCor>()
        {
            new WebCor() { Name = "AllowAngularOrigins", Link = "http://localhost:4200" }
        };


        /** private data */
        private TimeSpan SessionTimout = TimeSpan.FromMinutes(30);
        private string? JWTSecretKey { get; set; }
        private string? JWTIssue { get; set; }
        private string? JWTAudience { get; set; }
        private string? EncryptionKey { get; set; }
        private NpgsqlConnection? PSQLMain { get; set; }


        /** configuration model */
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Configuration(WebApplicationBuilder webBuilder)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
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

        /** getting psql conection */
        public async Task<NpgsqlConnection> GetPSQLMain(ILogger inLogger)
        {
            if (PSQLMain == null) PSQLMain = new NpgsqlConnection(new Models.Modeling.Database.PSQL.MAIN.CONNECTION.MAIN().ToString());
            if (PSQLMain.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    await PSQLMain.OpenAsync();
                    inLogger.LogInformation("Database connection is success");
                }
                catch (Exception getError)
                {
                    inLogger.LogError($"Database connection error: {getError.Message}");
                } 
            }
            return PSQLMain;
        }

        /** data encryption */
        public string GetEncryption(string? inValue)
        {
            inValue = (inValue ?? string.Empty);
            if (string.IsNullOrEmpty(inValue)) return string.Empty;
            if (string.IsNullOrEmpty(EncryptionKey)) return string.Empty;
            byte[] iv = new byte[16];
            byte[] array;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(inValue.Trim());
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(array);
        }

        /** data decryption */
        public string GetDecryption(string? inValue)
        {
            inValue = (inValue ?? string.Empty);
            if (string.IsNullOrEmpty(inValue)) return string.Empty;
            if (string.IsNullOrEmpty(EncryptionKey)) return string.Empty;
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(inValue);
            string getData = string.Empty;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            getData = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            return getData;
        }

        /** JWT generator */
        public string GenerateJWT(string? inUsername, string? inRule)
        {
            inUsername = (inUsername ?? string.Empty).Trim();
            inRule = (inRule ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(inUsername)) return string.Empty;
            if (string.IsNullOrEmpty(inRule)) return string.Empty;
            if (string.IsNullOrEmpty(JWTSecretKey)) return string.Empty;

            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Name, inUsername),
                new Claim(ClaimTypes.Role, inRule)
            };
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSecretKey));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken newToken = new JwtSecurityToken(
                issuer: JWTIssue,
                audience: JWTAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(newToken);
        }

        /** password regex data */
        public string GeneratePassword(string? inPassword)
        {
            string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,15}$";
            inPassword = (inPassword ?? "").Trim();
            if (string.IsNullOrEmpty(inPassword)) return string.Empty;
            if (!Regex.IsMatch(inPassword, PasswordRegex)) return string.Empty;
            return inPassword;
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EncryptionKey)),
                    RoleClaimType = ClaimTypes.Role
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
        private async Task<NpgsqlConnection> GetPSQLConnection(WebApplicationBuilder webBuilder, string configurationKey)
        {
            string databaseConnectionString = GetValue(webBuilder, configurationKey);
            if (string.IsNullOrEmpty(databaseConnectionString)) return new NpgsqlConnection();
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
