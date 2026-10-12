using FootballTransfer.Api.Data;
using FootballTransfer.Api.Infrastructure;
using FootballTransfer.Api.Services;
using FootballTransfer.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required. Set ConnectionStrings__DefaultConnection.");
            }

            builder.Services.AddDbContext<FootballTransferDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<NewsService>();
            builder.Services.AddHttpClient<NewsCrawlerService>(client =>
                client.Timeout = TimeSpan.FromSeconds(20));
            builder.Services.AddHttpClient<ArticleContentService>(client =>
                client.Timeout = TimeSpan.FromSeconds(20));
            builder.Services.AddScoped<OpenAiAnalysisService>();
            builder.Services.AddHostedService<NewsBackgroundService>();
            builder.Services.AddOpenApi();
            builder.Services.AddScoped<AiAnalysisService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173",
                                    "https://ai-football-transfer-platform.pages.dev")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            var app = builder.Build();

            if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
            {
                using var scope = app.Services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<FootballTransferDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    database.Database.Migrate();
                    logger.LogInformation("Database migrations are up to date.");
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Database migration failed. The API will stay online and report not ready.");
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseCors("Frontend");
            app.UseMiddleware<AdminApiKeyMiddleware>();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
