using Microsoft.AspNetCore.Mvc;
using OCRBilletParse.Common;
using OCRBilletParse.Storage.Interface;

namespace OCRBilletParse.Storage.Api
{
    [Route("[controller]")]
    [ApiController]
    public class RedisStorageController : ControllerBase
    {
        private INoSqlStorageLogic NoSqlStorageLogic { get; }
        public RedisStorageController(INoSqlStorageLogic noSqlStorageLogic)
        {
            NoSqlStorageLogic = noSqlStorageLogic;
        }
        [HttpGet]
        [Route("healthcheck")]
        public async Task<ActionResult> Healhcheck() => Ok(await Task.Run(() => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));
        [HttpGet("{key}")]
        public async Task<ActionResult> Get(string key)
        {
            return await Task.Run(() =>
            {
                var item = NoSqlStorageLogic.Get(key);
                return Ok(item);
            });
        }
        [HttpGet]
        [Route("{key}/check")]
        public async Task<ActionResult> Check(string key)
        {
            return await Task.Run(() =>
            {
                bool itemExists = NoSqlStorageLogic.Exists(key);
                return Ok(itemExists);
            });
        }
        [HttpPost]
        public async Task<ActionResult> Post([FromBody]KeyValueItem redisItem)
        {
            return await Task.Run(() =>
            {
                NoSqlStorageLogic.Save(redisItem);
                return Ok();
            });
        }
        [HttpDelete("{key}")]
        public async Task<ActionResult> Delete(string key)
        {
            return await Task.Run(() =>
            {
                NoSqlStorageLogic.Remove(key);
                return Ok();
            });
        }
    }
}
