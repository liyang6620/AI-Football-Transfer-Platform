using FootballTransfer.Api.Data;
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

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors("Frontend");
            app.UseMiddleware<AdminApiKeyMiddleware>();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
