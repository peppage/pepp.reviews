using CodeHollow.FeedReader;
using Common.Models;
using Generator.Mappers;
using Microsoft.Extensions.Configuration;
using Razor.Templating.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using Templates.Models;

namespace Generator
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", false)
                .Build();

            // Once it builds with these settings they are not read from the config again.
            var feedUrl = config["feed"];
            var location = config["location"];

            List<Video> videos = new List<Video>();

            var feed = await FeedReader.ReadAsync(feedUrl);
            var rss20feed = (CodeHollow.FeedReader.Feeds.AtomFeed)feed.SpecificFeed;
            foreach (var item in rss20feed.Items)
            {
                videos.Add(VideoMapper.XElementToVideo(item.Element));
            }

            var html = await RazorTemplateEngine.RenderAsync("~/Razor/index.cshtml", new IndexModel
            {
                Videos = videos,
            });

            await System.IO.File.WriteAllTextAsync($"{location}/Index.html", html);
        }
    }
}