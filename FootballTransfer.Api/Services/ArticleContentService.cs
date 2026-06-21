using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace FootballTransfer.Api.Services;

public class ArticleContentService
{
    private readonly HttpClient _httpClient;

    public ArticleContentService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        );
    }

    public async Task<string> GetArticleContentAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var paragraphs = doc.DocumentNode.SelectNodes("//p");

            if (paragraphs == null || paragraphs.Count == 0)
            {
                return string.Empty;
            }

            var text = string.Join("\n", paragraphs
                .Select(p => WebUtility.HtmlDecode(p.InnerText).Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => t.Length > 30));

            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (text.Length > 4000)
            {
                text = text.Substring(0, 4000);
            }

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }
}