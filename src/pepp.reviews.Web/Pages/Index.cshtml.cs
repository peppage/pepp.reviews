using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using pepp.reviews.Web.Models;
using pepp.reviews.Web.Mappers;

namespace pepp.reviews.Web.Pages
{
    public class IndexModel : PageModel
    {
        private IMemoryCache _cache;
        private static string FeedCacheKey => "_Feed";
        private readonly ILogger<IndexModel> _logger;
        public List<Video> Videos { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _cache = memoryCache;
        }

        public async Task OnGetAsync()
        {
            List<Video> cacheEntry;

            if (!_cache.TryGetValue(FeedCacheKey, out cacheEntry))
            {
                var feed = await FeedReader.ReadAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UCmkCPjKpngDHZp0orr7GuGA");
                var rss20feed = (CodeHollow.FeedReader.Feeds.AtomFeed)feed.SpecificFeed;
                cacheEntry = new List<Video>();
                foreach (var item in rss20feed.Items)
                {
                    cacheEntry.Add(VideoMapper.XElementToVideo(item.Element));
                }
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromDays(3));

                _cache.Set(FeedCacheKey, cacheEntry, cacheEntryOptions);
            }

            Videos = cacheEntry;
        }
    }
}
