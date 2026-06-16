using Microsoft.AspNetCore.Mvc;
using FootballTransfer.Api.Models;

namespace FootballTransfer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var news = new List<TransferNews>
            {
                new TransferNews
                {
                    Id = 1,
                    Title = "Liverpool interested in Florian Wirtz",
                    Content = "Liverpool are monitoring Florian Wirtz.",
                    Source = "BBC Sport",
                    Url = "https://bbc.com",
                    PublishedAt = DateTime.UtcNow
                }
            };

            return Ok(news);
        }
    }
}