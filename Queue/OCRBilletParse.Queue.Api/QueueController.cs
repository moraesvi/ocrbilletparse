using Microsoft.AspNetCore.Mvc;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Queue.Interface;

namespace OCRBilletParse.Queue.Api;

[Route("[controller]")]
[ApiController]
public class QueueController : ControllerBase
{
    private readonly IBillParseQueueLogic _billParseLogic;
    public QueueController(IBillParseQueueLogic billParseLogic) 
    {
        _billParseLogic = billParseLogic;
    }
    [HttpGet]
    [Route("healthcheck")]
    public async Task<ActionResult> Healhcheck() => Ok(await Task.Run(() => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));
    [HttpGet]
    [Route("{transactionId}/check")]
    public async Task<ActionResult> Check(string transactionId)
    {
        var itemProcessed = await _billParseLogic.CheckItemWasProcessed(transactionId);
        return Ok(itemProcessed);
    }
    [HttpPost]
    [Route("send")]
    public async Task<ActionResult> Send([FromBody] ImageParam imageParam)
    {
        string transactionId = await Task.Run(() => _billParseLogic.SendToQueue(imageParam));
        return Ok(transactionId);
    }
}
