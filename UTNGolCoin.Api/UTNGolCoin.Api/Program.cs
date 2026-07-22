using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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

            // RNF10: cualquier error de validación automática de ASP.NET Core (ej. JSON mal
            // formado) responde con el mismo formato { mensaje } que usan los controladores,
            // en vez del ValidationProblemDetails por defecto.
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var mensaje = string.Join(" ", context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    if (string.IsNullOrWhiteSpace(mensaje))
                    {
                        mensaje = "La solicitud tiene datos inválidos.";
                    }

                    return new BadRequestObjectResult(new { mensaje });
                };
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "UTNGolCoin API",
                    Version = "v1",
                    Description = "Servicio de moneda virtual y apuestas del Mundial 2026 - UTN GolMundial. " +
                        "Gestiona billeteras, apuestas (predicciones), liquidación de premios, bono antibancarrota, " +
                        "ranking y reportes. Ver ESTADO_PROYECTO.md en el repositorio para el contrato completo."
                });

                var archivoXmlComentarios = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
                if (File.Exists(archivoXmlComentarios))
                {
                    options.IncludeXmlComments(archivoXmlComentarios);
                }
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

            // RNF10: cualquier excepción no controlada (ej. la base de datos caída) responde
            // con el mismo formato { mensaje } en vez de una página de error o un 500 vacío.
            // El detalle técnico queda en los logs del servidor, no en la respuesta al cliente.
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var excepcion = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    if (excepcion is not null)
                    {
                        logger.LogError(excepcion, "Error no controlado procesando {Path}", context.Request.Path);
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { mensaje = "Ocurrió un error inesperado en el servidor." });
                });
            });

            // RNF10: rutas inexistentes (404) o métodos no permitidos (405) también responden
            // con el mismo formato { mensaje }, en vez de un cuerpo vacío.
            app.UseStatusCodePages(async context =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new { mensaje = $"No se pudo procesar la solicitud (HTTP {context.HttpContext.Response.StatusCode})." });
            });

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
