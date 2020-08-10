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
## Add `CustomerController.cs`

```c#
   [ApiController]
   [Route("[controller]")]
   public class FibonacciController : ControllerBase
   {
       private readonly IMemoryCache _memoryCache;
       private readonly IDistributedCache _distributedCache;
       private readonly ILogger<FibonacciController> _logger;

       public FibonacciController(  IMemoryCache memoryCache, 
                                   IDistributedCache distributedCache, 
                                   ILogger<FibonacciController> logger)
       {
           _memoryCache = memoryCache;
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
fetch("https://localhost:5001/fibonacci/fibonacci-redis", {
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