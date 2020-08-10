using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RedisCaching_Tutorial.Models;

namespace RedisCaching_Tutorial.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FibonacciController : ControllerBase
    {
        //private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<FibonacciController> _logger;

        public FibonacciController(  /*IMemoryCache memoryCache, */
                                    IDistributedCache distributedCache, 
                                    ILogger<FibonacciController> logger)
        {
            //_memoryCache = memoryCache;
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
}