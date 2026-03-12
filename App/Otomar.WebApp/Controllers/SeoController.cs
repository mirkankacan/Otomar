using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Otomar.WebApp.Services;
using System.Text;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("")]
    public class SeoController(IMemoryCache cache, ILogger<SeoController> logger) : Controller
    {
        [HttpGet("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public IActionResult SitemapIndex()
        {
            if (cache.TryGetValue(FeedGeneratorService.SitemapIndexCacheKey, out string? xml) && !string.IsNullOrEmpty(xml))
            {
                return Content(xml, "application/xml", Encoding.UTF8);
            }

            logger.LogWarning("Sitemap index henüz oluşturulmadı, cache boş");
            return StatusCode(503, "Sitemap hazırlanıyor, lütfen birkaç dakika sonra tekrar deneyin.");
        }

        [HttpGet("sitemap-{id:int}.xml")]
        [ResponseCache(Duration = 3600)]
        public IActionResult SitemapPart(int id)
        {
            var cacheKey = $"{FeedGeneratorService.SitemapPartCacheKeyPrefix}{id}";
            if (cache.TryGetValue(cacheKey, out string? xml) && !string.IsNullOrEmpty(xml))
            {
                return Content(xml, "application/xml", Encoding.UTF8);
            }

            logger.LogWarning("Sitemap parça {Id} bulunamadı", id);
            return NotFound();
        }

        [HttpGet("feed.xml")]
        [ResponseCache(Duration = 3600)]
        public IActionResult Feed()
        {
            if (cache.TryGetValue(FeedGeneratorService.FeedCacheKey, out string? xml) && !string.IsNullOrEmpty(xml))
            {
                return Content(xml, "application/xml", Encoding.UTF8);
            }

            logger.LogWarning("Feed henüz oluşturulmadı, cache boş");
            return StatusCode(503, "Feed hazırlanıyor, lütfen birkaç dakika sonra tekrar deneyin.");
        }
    }
}
