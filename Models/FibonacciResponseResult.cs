namespace RedisCaching_Tutorial.Models
{
    public class FibonacciResponseResult
    {
        public int Result { get; set; }
        public bool IsGetFromRedis { get; set; }
        public string ExecutionTime { get; set; }
    }
}