using FootballTransfer.Api.Data;
using FootballTransfer.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddDbContext<FootballTransferDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<NewsService>();
            builder.Services.AddScoped<NewsCrawlerService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<OpenAiAnalysisService>();
            builder.Services.AddHostedService<NewsBackgroundService>();
            builder.Services.AddScoped<ArticleContentService>();
            builder.Services.AddOpenApi();
            builder.Services.AddScoped<AiAnalysisService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
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
            app.UseCors("AllowFrontend");
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}