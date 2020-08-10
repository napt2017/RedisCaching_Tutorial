using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RedisCaching_Tutorial.Models;

namespace RedisCaching_Tutorial.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InMemoryCacheController :ControllerBase
    {
        private readonly ILogger<InMemoryCacheController> _logger;
        private readonly IMemoryCache _memoryCache;

        public InMemoryCacheController(ILogger<InMemoryCacheController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet("{key}")]
        public MemoryCacheResult GetDataFromCache(string key)
        {
            if (_memoryCache.TryGetValue(key, out var val))
            {
                return new MemoryCacheResult
                {
                    IsExist = true,
                    Data = val.ToString()
                };
            }
            return new MemoryCacheResult();
        }

        [HttpPost]
        public IActionResult SetCache(MemoryCacheRequest cacheRequest)
        {
            var cacheExpiryOption = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5),
                Priority = CacheItemPriority.High,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Size = 1024
            };
            _memoryCache.Set(cacheRequest.Key, cacheRequest.Value, cacheExpiryOption);
            return Ok();
        }
    }
}