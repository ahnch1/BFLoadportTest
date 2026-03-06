using System;
using System.Threading;
using System.Threading.Tasks;

namespace BackendPLCAPP.Services
{
    // 장비 상태 정의 [1]
    public enum MachineState
    {
        Stop,
        Ready,
        Run
    }

    // 프론트엔드와 공유할 상태 모델 [1]
    public class DemoStateModel
    {
        public MachineState CurrentState { get; set; } = MachineState.Stop;
        public int TipBoxCount { get; set; } = 0;
        public int DeepWellCount { get; set; } = 0;

        public int TipLoadTimer { get; set; } = 0;
        public int DeepWellInTimer { get; set; } = 0;
        public int DeepWellOutTimer { get; set; } = 0;
        public int AgarPlateTimer { get; set; } = 0;
    }

    public class DemoRoutineService
    {
        public DemoStateModel State { get; private set; } = new DemoStateModel();

        // 싱글톤으로 등록된 PLC 통신 서비스를 주입받음
        private readonly C6015Work _plcService;

        public DemoRoutineService(C6015Work plcService)
        {
            _plcService = plcService;
        }

        // --------------------------------------------------------
        // 1. 기본 장비 상태 제어 (Start / Stop) [1]
        // --------------------------------------------------------
        public void PressStartButton()
        {
            if (State.CurrentState == MachineState.Stop)
            {
                State.CurrentState = MachineState.Ready;
                Console.WriteLine("장비 상태: Ready (Tipbox, Deep Well, Agar Plate 로딩 대기)");
            }
        }

        public void PressStopButton()
        {
            State.CurrentState = MachineState.Stop;
            Console.WriteLine("장비 상태: Stop");
        }

        // --------------------------------------------------------
        // 2. 외부 동작 명령 (포트별 로딩 로직) [2, 3]
        // --------------------------------------------------------

        // TipLoad 요청 처리 [2]
        public async Task ExecuteTipLoadAsync(CancellationToken cancellationToken)
        {
            if (State.CurrentState != MachineState.Ready) return;

            var plcData = _plcService.GetDeviceStatus();
            if (plcData.Di1) // 로드 포트에 Tipbox가 있으면 넣지 못함 (센서 ON)
            {
                Console.WriteLine("TipLoad 불가: 로드 포트에 이미 제품이 있습니다.");
                return;
            }

            bool initialState = plcData.Di1;

            try
            {
                // 5초 카운트다운 [2]
                for (int i = 5; i > 0; i--)
                {
                    State.TipLoadTimer = i;
                    await Task.Delay(1000, cancellationToken);

                    // 5초간 제품감지 상태가 변경되면 에러 메세지 후 Stop [2]
                    if (_plcService.GetDeviceStatus().Di1 != initialState)
                    {
                        Console.WriteLine("에러: TipLoad 5초 대기 중 상태가 변경되었습니다.");
                        State.CurrentState = MachineState.Stop;
                        State.TipLoadTimer = 0;
                        return;
                    }
                }

                State.TipBoxCount++; // 로딩 성공 후 카운터 증가

                // 30초 뒤 TipLoad Off (UI 표시) 비동기 실행 [2]
                State.TipLoadTimer = 30;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000);
                    State.TipLoadTimer = 0;
                });
            }
            catch (TaskCanceledException)
            {
                State.TipLoadTimer = 0;
            }
        }

        // DeepWell In 요청 처리 [2]
        public async Task ExecuteDeepWellInAsync(CancellationToken cancellationToken)
        {
            if (State.CurrentState != MachineState.Ready) return;

            var plcData = _plcService.GetDeviceStatus();
            if (plcData.Di2) // 센서 ON이면 넣지 못함 [2]
            {
                Console.WriteLine("DeepWell In 불가: 포트에 이미 제품이 있습니다.");
                return;
            }

            bool initialState = plcData.Di2;

            try
            {
                for (int i = 5; i > 0; i--)
                {
                    State.DeepWellInTimer = i;
                    await Task.Delay(1000, cancellationToken);

                    if (_plcService.GetDeviceStatus().Di2 != initialState)
                    {
                        Console.WriteLine("에러: DeepWell In 대기 중 상태 변경됨.");
                        State.CurrentState = MachineState.Stop;
                        State.DeepWellInTimer = 0;
                        return;
                    }
                }

                State.DeepWellCount++;

                State.DeepWellInTimer = 30;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000);
                    State.DeepWellInTimer = 0;
                });
            }
            catch (TaskCanceledException) { State.DeepWellInTimer = 0; }
        }

        // DeepWell Out 요청 처리 [2]
        public async Task ExecuteDeepWellOutAsync(CancellationToken cancellationToken)
        {
            if (State.CurrentState != MachineState.Ready) return;

            var plcData = _plcService.GetDeviceStatus();
            if (!plcData.Di3) // 센서가 OFF이면 가져가지 못함 [2]
            {
                Console.WriteLine("DeepWell Out 불가: 가져갈 제품이 없습니다.");
                return;
            }

            bool initialState = plcData.Di3;

            try
            {
                for (int i = 5; i > 0; i--)
                {
                    State.DeepWellOutTimer = i;
                    await Task.Delay(1000, cancellationToken);

                    if (_plcService.GetDeviceStatus().Di3 != initialState)
                    {
                        Console.WriteLine("에러: DeepWell Out 대기 중 상태 변경됨.");
                        State.CurrentState = MachineState.Stop;
                        State.DeepWellOutTimer = 0;
                        return;
                    }
                }

                State.DeepWellOutTimer = 30;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000);
                    State.DeepWellOutTimer = 0;
                });
            }
            catch (TaskCanceledException) { State.DeepWellOutTimer = 0; }
        }

        // Agar Plate In 요청 처리 및 메인 루틴 진입 [3]
        public async Task ExecuteAgarPlateInAsync(CancellationToken cancellationToken)
        {
            if (State.CurrentState != MachineState.Ready) return;

            var plcData = _plcService.GetDeviceStatus();
            if (plcData.Di4) // 로드 포트에 제품이 있으면 넣지 못함 [3]
            {
                Console.WriteLine("Agar Plate In 불가: 포트에 이미 제품이 있습니다.");
                return;
            }

            bool initialState = plcData.Di4;

            try
            {
                for (int i = 5; i > 0; i--)
                {
                    State.AgarPlateTimer = i;
                    await Task.Delay(1000, cancellationToken);

                    if (_plcService.GetDeviceStatus().Di4 != initialState)
                    {
                        Console.WriteLine("에러: Agar Plate In 대기 중 상태 변경됨.");
                        State.CurrentState = MachineState.Stop;
                        State.AgarPlateTimer = 0;
                        return;
                    }
                }

                State.AgarPlateTimer = 30;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000);
                    State.AgarPlateTimer = 0;
                });

                // Agar Plate가 들어오면 메인 루틴 실행 (Run 상태 전환) [1]
                await StartMainRoutineAsync(cancellationToken);
            }
            catch (TaskCanceledException) { State.AgarPlateTimer = 0; }
        }

        // --------------------------------------------------------
        // 3. 메인 루틴 실행 로직 [1]
        // --------------------------------------------------------
        private async Task StartMainRoutineAsync(CancellationToken cancellationToken)
        {
            // 시작 전 카운터 확인 로직 [1]
            if (State.TipBoxCount == 0 || State.DeepWellCount == 0)
            {
                Console.WriteLine("에러: TipBox 또는 Deep Well 제품을 로딩하세요.");
                State.CurrentState = MachineState.Stop;
                return;
            }

            // Agar Plate가 들어오면 Run 상태가 됨 [1]
            State.CurrentState = MachineState.Run;

            try
            {
                // 1. Agar Plate Off (Work Start 가정) [1]
                Console.WriteLine("MainRoutine 1: Agar Plate Off (Work Start)");

                // 2. 내부 타이머 진행 10초 [1]
                for (int i = 10; i > 0; i--)
                {
                    if (State.CurrentState == MachineState.Stop) return; // 중간에 Stop 명령 발생 시 중단
                    await Task.Delay(1000, cancellationToken);
                }

                // 3. Agar Plate On (Work done 가정) [1]
                Console.WriteLine("MainRoutine 3: Agar Plate On (Work done)");

                // 4. 카운터 감소 [1]
                State.TipBoxCount--;
                State.DeepWellCount--;
                Console.WriteLine("MainRoutine 4: Tipbox, Deep Well 카운터 -1");

                // 5. Deep Well Out On [1]
                Console.WriteLine("MainRoutine 5: Deep Well Out On");

                // 6. 장비 상태는 Stop으로 [1]
                State.CurrentState = MachineState.Stop;
                Console.WriteLine("MainRoutine 종료: 장비 상태 Stop");
            }
            catch (TaskCanceledException)
            {
                State.CurrentState = MachineState.Stop;
            }
        }
    }
}