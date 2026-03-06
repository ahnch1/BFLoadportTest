namespace BackendPLCAPP
{
    // 로드포트 상태
    public class DeviceStatus
    {
        public bool Di1 { get; set; }
        public bool Di2 { get; set; }
        public bool Di3 { get; set; }
        public bool Di4 { get; set; }
        public string ConnectionState { get; set; } = "Disconnected";
    }
    // 장비 상태
    public enum MachineState
    {
        Stop,
        Ready,
        Run
    }
    // UI로 SSE를 통해 전송될 데모 상태 모델
    public class DemoStateModel
    {
        public MachineState CurrentState { get; set; } = MachineState.Stop;
        public int TipBoxCount { get; set; } = 0;
        public int DeepWellCount { get; set; } = 0;

        // 포트별 카운트다운 타이머 (초 단위)
        public int TipLoadTimer { get; set; } = 0;
        public int DeepWellInTimer { get; set; } = 0;
        public int DeepWellOutTimer { get; set; } = 0;
        public int AgarPlateTimer { get; set; } = 0;
    }
}
