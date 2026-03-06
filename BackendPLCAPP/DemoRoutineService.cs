using System;
using System.Threading;
using System.Threading.Tasks;

namespace BackendPLCAPP.Services
{
    public enum MachineState
    {
        Stop,
        Ready,
        Run
    }

    public class DemoStateModel
    {
        public MachineState CurrentState { get; set; } = MachineState.Stop;
        public int TipBoxCount { get; set; } = 0;
        public int DeepWellCount { get; set; } = 0;

        public int TipLoadTimer { get; set; } = 0;
        public string TipLoadMessage { get; set; } = "";

        public int DeepWellInTimer { get; set; } = 0;
        public string DeepWellInMessage { get; set; } = "";

        public int DeepWellOutTimer { get; set; } = 0;
        public string DeepWellOutMessage { get; set; } = "";

        public int AgarPlateTimer { get; set; } = 0;
        public string AgarPlateMessage { get; set; } = "";

    }

    public class DemoRoutineService
    {
        public DemoStateModel State { get; private set; } = new DemoStateModel();

        // 센서 이전 상태 기억용 변수
        private bool _prevTipLoad = false, _waitTipLoadRemove = false;
        private bool _prevDwIn = false, _waitDwInRemove = false;
        private bool _prevDwOut = false, _waitDwOutRemove = false;
        private bool _prevAgarIn = false, _waitAgarInRemove = false;
        // 각 포트별 타이머 취소 토큰
        private CancellationTokenSource _ctsTipLoad;
        private CancellationTokenSource _ctsDwIn;
        private CancellationTokenSource _ctsDwOut;
        private CancellationTokenSource _ctsAgarIn;

        //// 싱글톤으로 등록된 PLC 통신 서비스를 주입받음
        //private readonly C6015Work _plcService;

        //public DemoRoutineService(C6015Work plcService)
        //{
        //    _plcService = plcService;
        //}

        // 백그라운드 워커에서 50ms마다 호출되는 통합 센서 체크 메서드
        public void CheckSensorState(DeviceStatus plcData)
        {
            // ----------------------------------------------------
            // 1. Tipbox In (Di1)
            // ----------------------------------------------------
            if (plcData.Di1 && !_prevTipLoad)
            {
                _ctsTipLoad?.Cancel();
                _ctsTipLoad = new CancellationTokenSource();
                _ = StartCountdownAsync(1, _ctsTipLoad.Token);
            }
            else if (!plcData.Di1 && _prevTipLoad)
            {
                _ctsTipLoad?.Cancel();
                State.TipLoadTimer = 0;
                if (_waitTipLoadRemove)
                {
                    State.TipBoxCount++; // 제거 시 카운터 증가
                    State.TipLoadMessage = "";
                    _waitTipLoadRemove = false;
                }
            }
            _prevTipLoad = plcData.Di1;

            // ----------------------------------------------------
            // 2. DeepWell In (Di2)
            // ----------------------------------------------------
            if (plcData.Di2 && !_prevDwIn)
            {
                _ctsDwIn?.Cancel();
                _ctsDwIn = new CancellationTokenSource();
                _ = StartCountdownAsync(2, _ctsDwIn.Token);
            }
            else if (!plcData.Di2 && _prevDwIn)
            {
                _ctsDwIn?.Cancel();
                State.DeepWellInTimer = 0;
                if (_waitDwInRemove)
                {
                    State.DeepWellCount++; // 제거 시 DeepWell 카운터 증가
                    State.DeepWellInMessage = "";
                    _waitDwInRemove = false;
                }
            }
            _prevDwIn = plcData.Di2;

            // ----------------------------------------------------
            // 3. DeepWell Out (Di3)
            // ----------------------------------------------------
            if (plcData.Di3 && !_prevDwOut)
            {
                _ctsDwOut?.Cancel();
                _ctsDwOut = new CancellationTokenSource();
                _ = StartCountdownAsync(3, _ctsDwOut.Token);
            }
            else if (!plcData.Di3 && _prevDwOut)
            {
                _ctsDwOut?.Cancel();
                State.DeepWellOutTimer = 0;
                if (_waitDwOutRemove)
                {
                    // Out의 경우 가져갔으므로 카운터 감소 (0 이하로 떨어지지 않게 방어)
                    if (State.DeepWellCount > 0) State.DeepWellCount--;
                    State.DeepWellOutMessage = "";
                    _waitDwOutRemove = false;
                }
            }
            _prevDwOut = plcData.Di3;

            // ----------------------------------------------------
            // 4. Agar Plate In (Di4)
            // ----------------------------------------------------
            if (plcData.Di4 && !_prevAgarIn)
            {
                _ctsAgarIn?.Cancel();
                _ctsAgarIn = new CancellationTokenSource();
                _ = StartCountdownAsync(4, _ctsAgarIn.Token);
            }
            else if (!plcData.Di4 && _prevAgarIn)
            {
                _ctsAgarIn?.Cancel();
                State.AgarPlateTimer = 0;
                if (_waitAgarInRemove)
                {
                    // Agar Plate는 별도 카운터가 없으므로 메세지만 삭제
                    State.AgarPlateMessage = "";
                    _waitAgarInRemove = false;
                }
            }
            _prevAgarIn = plcData.Di4;
        }

        // 💡 4개 포트가 공통으로 사용하는 5초 카운트다운 로직
        private async Task StartCountdownAsync(int portType, CancellationToken ct)
        {
            try
            {
                // 5초 카운트다운
                for (int i = 5; i > 0; i--)
                {
                    if (portType == 1) State.TipLoadTimer = i;
                    else if (portType == 2) State.DeepWellInTimer = i;
                    else if (portType == 3) State.DeepWellOutTimer = i;
                    else if (portType == 4) State.AgarPlateTimer = i;

                    await Task.Delay(1000, ct);
                }

                // 5초 경과 후 타이머 0으로 만들고 제거 요청 메세지 출력
                if (portType == 1)
                {
                    State.TipLoadTimer = 0;
                    State.TipLoadMessage = "Tipbox 제거 요망";
                    _waitTipLoadRemove = true;
                }
                else if (portType == 2)
                {
                    State.DeepWellInTimer = 0;
                    State.DeepWellInMessage = "DW In 제거 요망";
                    _waitDwInRemove = true;
                }
                else if (portType == 3)
                {
                    State.DeepWellOutTimer = 0;
                    State.DeepWellOutMessage = "DW Out 제거 요망";
                    _waitDwOutRemove = true;
                }
                else if (portType == 4)
                {
                    State.AgarPlateTimer = 0;
                    State.AgarPlateMessage = "Agar 제거 요망";
                    _waitAgarInRemove = true;
                }
            }
            catch (TaskCanceledException)
            {
                // 5초가 되기 전에 센서가 OFF 되면 아무 작업도 하지 않음 (초기화는 CheckSensorState에서 수행)
            }
        }

        // 장비 상태 제어 버튼 로직 (기존 유지)
        public void PressStartButton() => State.CurrentState = MachineState.Ready;
        public void PressStopButton() => State.CurrentState = MachineState.Stop;
    }
}