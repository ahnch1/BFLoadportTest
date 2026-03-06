using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BackendPLCAPP.Services
{
    // BackgroundService를 상속받아 앱 동작 내내 백그라운드에서 실행되는 클래스
    public class PlcBackgroundWorker : BackgroundService
    {
        private readonly C6015Work _plcWork;

        // 싱글톤으로 등록된 C6015Work를 주입받음
        public PlcBackgroundWorker(C6015Work plcWork)
        {
            _plcWork = plcWork;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 프로그램이 종료될 때까지(cancellation이 요청될 때까지) 무한 루프
            while (!stoppingToken.IsCancellationRequested)
            {
                // PLC 데이터를 읽어서 C6015Work 내부의 _currentStatus 캐시를 갱신
                await _plcWork.ReadInputsAsync(stoppingToken);

                // 통신 부하를 줄이기 위해 50ms 대기 (초당 20회 갱신)
                await Task.Delay(50, stoppingToken);
            }
        }
    }
}