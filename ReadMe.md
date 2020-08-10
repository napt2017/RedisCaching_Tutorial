## 1. Redis Distributed Cache
### Full tutorial link:
https://www.codewithmukesh.com/blog/redis-caching-in-aspnet-core/?fbclid=IwAR1q9mx2IYyt1gF5mzbt5H6rIcu6hbXfvx2CkDdravi3AKh-GsAfhDuVAGQ

## Install dependency
Install-Package Microsoft.Extensions.Caching.StackExchangeRedis

## Add to `Startup.cs` file

```c#
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
``` 
## Add `FibonacciRedisController.cs`

```c#
   [ApiController]
   [Route("[controller]")]
   public class FibonacciRedisController : ControllerBase
   { 
       private readonly IDistributedCache _distributedCache;
       private readonly ILogger<FibonacciRedisController> _logger;

       public FibonacciRedisController( IDistributedCache distributedCache, 
                                   ILogger<FibonacciRedisController> logger)
       { 
           _distributedCache = distributedCache;
           _logger = logger;
       }

       [HttpPost("fibonacci-redis")]
       public async Task<FibonacciResponseResult> ComputeFibonacci(FibonacciRequest fibRequest)
       {
           var st = new Stopwatch();
           st.Start();
           var fibRedisKey = $"Fib_of_{fibRequest.TargetNumber}";
           var oldResult = await _distributedCache.GetAsync(fibRedisKey);
           if (oldResult != null && int.TryParse(Encoding.UTF8.GetString(oldResult), out var fibResult))
           {
               st.Stop();
               return new FibonacciResponseResult
               {
                   IsGetFromRedis = true,
                   Result = fibResult,
                   ExecutionTime = $"{st.ElapsedMilliseconds} ms"
               };
           }

           var fibComputeResult = InnerComputeFibonacci(fibRequest.TargetNumber);
           var cacheOption = new DistributedCacheEntryOptions()
           {
               AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
               SlidingExpiration = TimeSpan.FromMinutes(2)
           };
           await _distributedCache.SetAsync(fibRedisKey, Encoding.UTF8.GetBytes(fibRequest.TargetNumber.ToString()), cacheOption);
           st.Stop();
           return new FibonacciResponseResult
           {
               IsGetFromRedis = true,
               Result = fibComputeResult,
               ExecutionTime = $"{st.ElapsedMilliseconds} ms"
           }; 
       }

       private int InnerComputeFibonacci(int number)
       {
           if (number == 0 || number == 1)
           {
               return number;
           }
           return InnerComputeFibonacci(number -1) + InnerComputeFibonacci(number -2);
       }
   }
```

## Start redis server then test
```javascript
fetch("https://localhost:5001/fibonacciredis/fibonacci-redis", {
        "headers": {
            'Content-Type': 'application/json'
        },
        "referrerPolicy": "no-referrer",
        "body": JSON.stringify({
            targetNumber: 5
        }),
        "method": "POST"
    }).then(resp => resp.json()).then(json => console.table(json));
```

## 2. In-memory Cache
### Add to `Startup.cs` file
```c#
 public void ConfigureServices(IServiceCollection services)
 { 
    services.AddControllers();
    services.AddMemoryCache(); 
 }
```

### Add `InMemoryCacheController.cs` file
```c#
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
```

### Test it
```javascript
// Add key
fetch("https://localhost:5001/inmemorycache", {
        "headers": {
            'Content-Type': 'application/json'
        },
        "referrerPolicy": "no-referrer",
        "body": JSON.stringify({
            key: "lab",
            value: "jade-leader"
        }),
        "method": "POST"
    });
// Get key
let response = await fetch("https://localhost:5001/inmemorycache/lab")
let data = await response.json();
console.log(data);
```