using Microsoft.AspNetCore.Mvc;

namespace BackendPLCAPP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class PLCController : ControllerBase
    {
        private readonly C6015Work _plcService;

        public PLCController(C6015Work plcService) => _plcService = plcService;

        [HttpGet("stream")]
        public async Task GetPlcStream(CancellationToken ct)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            while (!ct.IsCancellationRequested)
            {
                // 기존 C# 코드의 ReadInputsAsync 호출
                var data = await _plcService.ReadInputsAsync(ct);
                var json = System.Text.Json.JsonSerializer.Serialize(data);

                // SSE 규격: "data: {json}\n\n"
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);

                await Task.Delay(100, ct); // 10Hz 업데이트 (제안서 사양급 속도)
            }
        }
    }
}
