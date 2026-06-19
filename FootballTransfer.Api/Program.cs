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

            builder.Services.AddOpenApi();
            builder.Services.AddScoped<AiAnalysisService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}