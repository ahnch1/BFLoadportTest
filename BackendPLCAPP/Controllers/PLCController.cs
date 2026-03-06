using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using BackendPLCAPP.Services;

namespace BackendPLCAPP.Controllers
{
    [ApiController]
    public class PLCController : ControllerBase
    {
        private readonly C6015Work _plcService; // 기존 PLC 통신 서비스
        private readonly DemoRoutineService _demoService; // 새로 추가된 데모 루틴 서비스

        // 의존성 주입(DI)을 통해 두 서비스 모두 컨트롤러에 연결
        public PLCController(C6015Work plcService, DemoRoutineService demoService)
        {
            _plcService = plcService;
            _demoService = demoService;
        }

        // 1. 프론트엔드로 데이터를 전달하는 SSE 엔드포인트
        [HttpGet("api/plc/stream")]
        public async Task GetStream(CancellationToken cancellationToken)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            while (!cancellationToken.IsCancellationRequested)
            {
                // PLC의 현재 I/O 상태 가져오기 (C6015Work 내부 메서드명에 맞춰 조정 필요)
                var plcData = _plcService.GetDeviceStatus();

                // 데모 루틴의 현재 상태 가져오기
                var demoState = _demoService.State;

                // 프론트엔드가 기대하는 통합 JSON 구조 생성
                var combinedData = new
                {
                    plcData = plcData,
                    demoState = demoState
                };

                // SSE 규격 "data: {json}\n\n" 에 맞춰 직렬화 및 전송
                var json = JsonSerializer.Serialize(combinedData);
                var message = $"data: {json}\n\n";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);

                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // 10Hz 속도로 송출 (100ms 대기)
                await Task.Delay(100, cancellationToken);
            }
        }

        // 2. 데모 루틴 제어를 위한 REST API 엔드포인트들
        [HttpPost("api/demo/start")]
        public IActionResult StartDemo()
        {
            _demoService.PressStartButton();
            return Ok(new { message = "Started" });
        }

        [HttpPost("api/demo/stop")]
        public IActionResult StopDemo()
        {
            _demoService.PressStopButton();
            return Ok(new { message = "Stopped" });
        }

        [HttpPost("api/demo/tipload")]
        public IActionResult TipLoad()
        {
            // 비동기 작업으로 백그라운드에서 타이머(5초/30초) 동작 수행
            _ = _demoService.ExecuteTipLoadAsync(CancellationToken.None);
            return Ok();
        }

        [HttpPost("api/demo/deepwell-in")]
        public IActionResult DeepWellIn()
        {
            _ = _demoService.ExecuteDeepWellInAsync(CancellationToken.None);
            return Ok();
        }

        [HttpPost("api/demo/deepwell-out")]
        public IActionResult DeepWellOut()
        {
            _ = _demoService.ExecuteDeepWellOutAsync(CancellationToken.None);
            return Ok();
        }

        [HttpPost("api/demo/agarplate-in")]
        public IActionResult AgarPlateIn()
        {
            _ = _demoService.ExecuteAgarPlateInAsync(CancellationToken.None);
            return Ok();
        }
    }
}