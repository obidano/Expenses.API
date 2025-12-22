using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Expenses.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RedisTestController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisTestController(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            try
            {
                var db = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                // Test basic operations
                var pingResult = server.Ping();
                var testKey = "test:connection";
                var testValue = $"Test at {DateTime.UtcNow:O}";
                
                // Test SET operation
                db.StringSet(testKey, testValue, TimeSpan.FromMinutes(1));
                
                // Test GET operation
                var retrievedValue = db.StringGet(testKey);
                
                // Test DELETE operation
                db.KeyDelete(testKey);
                
                return Ok(new
                {
                    success = true,
                    message = "Redis connection is working",
                    pingLatency = pingResult.TotalMilliseconds,
                    serverInfo = new
                    {
                        endpoints = _redis.GetEndPoints().Select(e => e.ToString()),
                        isConnected = _redis.IsConnected,
                        clientName = _redis.ClientName
                    },
                    testOperations = new
                    {
                        set = "Success",
                        get = retrievedValue.HasValue ? "Success" : "Failed",
                        retrievedValue = retrievedValue.ToString(),
                        delete = "Success"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Redis connection failed",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("info")]
        public IActionResult Info()
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = server.Info();
                
                return Ok(new
                {
                    success = true,
                    isConnected = _redis.IsConnected,
                    endpoints = _redis.GetEndPoints().Select(e => e.ToString()),
                    clientName = _redis.ClientName,
                    serverInfo = info.Select(i => new { section = i.Key, values = i.Value })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get Redis info",
                    error = ex.Message
                });
            }
        }
    }
}
