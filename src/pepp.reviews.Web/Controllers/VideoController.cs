using System;
using System.Collections.Generic;
using CodeHollow.FeedReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace pepp.reviews.Web.Controllers
{
    public class VideoController : Controller
    {
        private IMemoryCache _cache;
        private static string FeedCacheKey => "_Feed";

        public VideoController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        public ActionResult Videos()
        {
            ICollection<FeedItem> cacheEntry;

            if (!_cache.TryGetValue(FeedCacheKey, out cacheEntry))
            {
                var feed = FeedReader.Read("https://www.youtube.com/feeds/videos.xml?channel_id=UCmkCPjKpngDHZp0orr7GuGA");
                cacheEntry = feed.Items;
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromDays(3));

                _cache.Set(FeedCacheKey, cacheEntry, cacheEntryOptions);
            }

            return Json(cacheEntry);
        }
    }
}
