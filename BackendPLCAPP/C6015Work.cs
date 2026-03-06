using TwinCAT;
using TwinCAT.Ads;

namespace BackendPLCAPP
{
    public class C6015Work : IDisposable
    {
        private AdsClient _client = new AdsClient();
        private readonly string _amsNetId = "172.18.238.122.1.1";
        private readonly int _port = 851;
        private readonly uint[] _handles = new uint[4];
        private bool _handlesCreated = false;
        private readonly object _lock = new object(); // 동기화를 위한 락 객체

        // 1. 최신 PLC 상태를 보관하는 내부 캐시 변수 추가
        private DeviceStatus _currentStatus = new DeviceStatus();

        // 2. 외부에서 비동기 대기 없이 즉시 최신 상태를 가져갈 수 있는 동기 메서드 추가
        public DeviceStatus GetDeviceStatus()
        {
            return _currentStatus;
        }

        public async Task<DeviceStatus> ReadInputsAsync(CancellationToken ct = default)
        {
            var status = new DeviceStatus();

            // 락을 사용하여 Disconnect와 충돌 방지
            lock (_lock)
            {
                if (_client == null || _client.IsDisposed)
                {
                    _client = new AdsClient();
                    _handlesCreated = false; // 클라이언트가 새로 생성되면 핸들 플래그 초기화
                }
            }

            try
            {
                if (!_client.IsConnected)
                {
                    await _client.ConnectAsync(AmsNetId.Parse(_amsNetId), _port, ct);
                    _handlesCreated = false; // 연결 직후 핸들 플래그 초기화
                }

                // 핸들이 없거나 유효하지 않으면 새로 생성
                if (!_handlesCreated)
                {
                    _handles[0] = _client.CreateVariableHandle("GVL.aInputs[0]");
                    _handles[1] = _client.CreateVariableHandle("GVL.aInputs[1]");
                    _handles[2] = _client.CreateVariableHandle("GVL.aInputs[2]");
                    _handles[3] = _client.CreateVariableHandle("GVL.aInputs[3]");
                    _handlesCreated = true;
                }

                status.Di1 = (bool)_client.ReadAny(_handles[0], typeof(bool));
                status.Di2 = (bool)_client.ReadAny(_handles[1], typeof(bool));
                status.Di3 = (bool)_client.ReadAny(_handles[2], typeof(bool));
                status.Di4 = (bool)_client.ReadAny(_handles[3], typeof(bool));
                status.ConnectionState = "Connected";
            }
            catch (Exception ex)
            {
                // 3. 0x711(1809) 에러가 메시지에 포함되어 있다면 강제 초기화
                if (ex.Message.Contains("0x711") || ex.Message.Contains("0x745") || ex.Message.Contains("1809"))
                {
                    status.ConnectionState = "Error: Symbol Version Invalid. Resetting handles...";
                    _handlesCreated = false; // 다음 루프에서 CreateVariableHandle을 다시 타도록 유도
                    Disconnect();
                }
                else
                {
                    status.ConnectionState = $"Error: {ex.Message}";
                }
            }
            _currentStatus = status;
            return status;
        }

        public void Disconnect()
        {
            lock (_lock) // 타이머 루프와 겹치지 않게 보호
            {
                try
                {
                    if (_handlesCreated && _client != null && _client.IsConnected)
                    {
                        for (int i = 0; i < _handles.Length; i++)
                        {
                            if (_handles[i] != 0)
                                _client.DeleteVariableHandle(_handles[i]);
                            _handles[i] = 0;
                        }
                    }
                    _handlesCreated = false;

                    if (_client != null && !_client.IsDisposed)
                    {
                        _client.Dispose();
                    }
                    _client = new AdsClient(); // 즉시 새 객체 준비 (Reconnect 대비)
                }
                catch { }
            }
        }

        public void Dispose() => Disconnect();
    }
}