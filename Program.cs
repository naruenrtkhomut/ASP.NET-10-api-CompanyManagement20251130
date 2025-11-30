
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Threading.Tasks;

namespace api
{
    public class Program
    {
        // depedance configuration
        public static Models.Configuration? configuration { get; set; }

        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            configuration = new Models.Configuration(builder);

            /** database connections */
            await configuration.AddDatabaseConnections(builder);

            NpgsqlConnection conn = new NpgsqlConnection();
            if (conn != null) Console.WriteLine(conn.State);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();


            app.MapControllers().RequireRateLimiting("FastPolicy");
            configuration.Cors.ForEach(getCor =>
            {
                app.UseCors(getCor.Name);
                Console.WriteLine($"Add cor name: {getCor.Name}");
                Console.WriteLine($"Add cor link: {getCor.Link}");
            });


            /** web application run */
            Console.WriteLine("/////////////////////////////////////////////////////////////");
            Console.WriteLine("*************************************************************");
            Console.WriteLine("                   web application on running                ");
            Console.WriteLine("*************************************************************");
            Console.WriteLine("/////////////////////////////////////////////////////////////");
            app.Run();
        }
    }
}
