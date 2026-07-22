using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Services;

namespace UTNGolCoin.Api
{
    public class Program
    {
        private const string CorsPolicyName = "AllowAll";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "UTNGolCoin API",
                    Version = "v1",
                    Description = "Servicio de moneda virtual y apuestas - UTN GolMundial 2026"
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddScoped<BilleteraService>();
            builder.Services.AddScoped<PrediccionService>();
            builder.Services.AddScoped<LiquidacionService>();
            builder.Services.AddScoped<RankingService>();
            builder.Services.AddScoped<BonoDiarioService>();
            builder.Services.AddScoped<ReporteService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "UTNGolCoin API v1");
                options.RoutePrefix = string.Empty; // Swagger UI como página de inicio
            });

            app.UseCors(CorsPolicyName);

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
