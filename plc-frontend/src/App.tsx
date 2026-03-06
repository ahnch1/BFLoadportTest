import React, { useEffect, useState } from 'react';
import { useStore } from './Store'; // 통합된 상태 저장소 사용

const App: React.FC = () => {
  // 전역 상태(Store)에서 데모 상태, PLC 데이터, 업데이트 함수들을 가져옵니다.
  const { 
    machineState, tipBoxCount, deepWellCount, 
    tipLoadTimer, deepWellInTimer, deepWellOutTimer, agarPlateTimer, 
    plcData, setDemoState, setPlcData 
  } = useStore();
  
  const [isConnected, setIsConnected] = useState(false);

  // SSE 서버 연결 및 데이터 수신 [2, 3]
  useEffect(() => {
    const eventSource = new EventSource('http://localhost:7191/api/plc/stream'); 

    eventSource.onopen = () => setIsConnected(true);
    eventSource.onerror = () => setIsConnected(false);
    
    eventSource.onmessage = (event) => {
      const data = JSON.parse(event.data);
      // 백엔드에서 전송하는 JSON 구조에 맞춰 각각의 상태 업데이트
      if (data.demoState) {
        setDemoState(data.demoState);
      }
      if (data.plcData) {
        setPlcData(data.plcData);
      }
    };

    return () => {
      eventSource.close();
      setIsConnected(false);
    };
  }, [setDemoState, setPlcData]);

  // REST API 명령 전송 함수 [1]
  const sendCommand = async (command: string) => {
    try {
      await fetch(`http://localhost:7191/api/demo/${command}`, { method: 'POST' });
    } catch (error) {
      console.error('명령 전송 실패:', error);
    }
  };

  // 장비 상태 토글 핸들러 [4]
  const toggleMachineState = () => {
    if (machineState === 'Stop') {
      sendCommand('start');
    } else {
      sendCommand('stop');
    }
  };

  return (
    <div style={styles.container}>
      {/* 상단 레이아웃 [5] */}
      <div style={styles.topSection}>
        <div style={styles.topLeft}>
          <div style={styles.connectionRow}>
            <button style={styles.blueButton}>장비 연결</button>
            <div style={styles.radioGroup}>
              <div style={styles.radioItem}>
                <div style={{ ...styles.radioCircle, backgroundColor: isConnected ? '#3b82f6' : 'transparent' }} />
                <span>ON</span>
              </div>
              <div style={styles.radioItem}>
                <div style={{ ...styles.radioCircle, backgroundColor: !isConnected ? '#ef4444' : 'transparent' }} />
                <span>OFF</span>
              </div>
            </div>
          </div>
          <div style={styles.counterBox}>Tipbox Counter : {tipBoxCount}</div>
          <div style={styles.counterBox}>Deepwell Counter : {deepWellCount}</div>
        </div>

        <div style={styles.topRight}>
          <div style={styles.blueButton}>장비 상태</div>
          <button 
            style={{ 
              ...styles.stateButton, 
              backgroundColor: machineState === 'Stop' ? '#ef4444' : (machineState === 'Run' ? '#22c55e' : '#f59e0b') 
            }}
            onClick={toggleMachineState}
          >
            {machineState === 'Stop' ? 'STOP' : (machineState === 'Run' ? 'RUN' : 'READY')}
          </button>
        </div>
      </div>

      {/* 하단 4개 포트 레이아웃 [5] */}
      <div style={styles.bottomSection}>
        <PortBlock 
          timerLabel="Timer Tipbox In" 
          timerValue={tipLoadTimer} 
          portName="TIPBOX" 
          sensorState={plcData.Di1} // Tipbox In 센서 매핑
          onClick={() => sendCommand('tipload')} 
        />
        <PortBlock 
          timerLabel="Timer DW In" 
          timerValue={deepWellInTimer} 
          portName="DEEP WELL IN" 
          sensorState={plcData.Di2} // DeepWell In 센서 매핑
          onClick={() => sendCommand('deepwell-in')} 
        />
        <PortBlock 
          timerLabel="Timer DW Out" 
          timerValue={deepWellOutTimer} 
          portName="DEEP WELL OUT" 
          sensorState={plcData.Di3} // DeepWell Out 센서 매핑
          onClick={() => sendCommand('deepwell-out')} 
        />
        <PortBlock 
          timerLabel="Timer Agar In" 
          timerValue={agarPlateTimer} 
          portName="AGAR IN" 
          sensorState={plcData.Di4} // Agar In 센서 매핑
          onClick={() => sendCommand('agarplate-in')} 
        />
      </div>
    </div>
  );
};

// 개별 포트 컴포넌트 렌더링 함수 (센서 상태 표시 LED 추가) [1, 5]
const PortBlock = ({ 
  timerLabel, timerValue, portName, sensorState, onClick 
}: { 
  timerLabel: string, timerValue: number, portName: string, sensorState: boolean, onClick: () => void 
}) => (
  <div style={styles.portWrapper}>
    <div style={styles.timerBox}>
      <div>{timerLabel.split(' ')}</div>
      <div>{timerLabel.split(' ').slice(1).join(' ')}</div>
      <div style={{ color: 'red', fontWeight: 'bold', marginTop: '5px' }}>
        {timerValue > 0 ? `${timerValue}s` : '\u00A0'}
      </div>
    </div>
    
    <div style={styles.yellowRectangle} onClick={onClick}>
      {/* 센서 상태를 나타내는 LED 인디케이터 */}
      <div style={{ 
        ...styles.sensorLed, 
        backgroundColor: sensorState ? '#22c55e' : '#94a3b8',
        boxShadow: sensorState ? '0 0 8px #22c55e' : 'none'
      }} title={sensorState ? "제품 감지됨" : "제품 없음"} />
      
      {/* 포트 이름 렌더링 */}
      <div style={{ marginTop: '15px' }}>
        {portName.split(' ').map((line, idx) => (
          <div key={idx} style={{ textAlign: 'center' }}>{line}</div>
        ))}
      </div>
    </div>
  </div>
);

// 시각적 레이아웃을 위한 인라인 스타일 객체 [5]
const styles: { [key: string]: React.CSSProperties } = {
  container: {
    border: '3px solid #60a5fa',
    padding: '30px',
    width: '800px',
    fontFamily: 'sans-serif',
    margin: '20px auto',
    backgroundColor: '#ffffff'
  },
  topSection: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: '50px'
  },
  topLeft: {
    display: 'flex',
    flexDirection: 'column',
    gap: '10px'
  },
  connectionRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '20px',
    marginBottom: '10px'
  },
  blueButton: {
    backgroundColor: '#3b82f6',
    color: 'white',
    border: 'none',
    padding: '10px 20px',
    fontSize: '18px',
    borderRadius: '8px',
    fontWeight: 'bold',
  },
  radioGroup: {
    display: 'flex',
    gap: '15px',
    fontSize: '18px',
    alignItems: 'center'
  },
  radioItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px'
  },
  radioCircle: {
    width: '20px',
    height: '20px',
    borderRadius: '50%',
    border: '2px solid #94a3b8'
  },
  counterBox: {
    border: '1px solid #3b82f6',
    padding: '5px 15px',
    width: 'max-content',
    fontSize: '14px'
  },
  topRight: {
    display: 'flex',
    gap: '20px',
    alignItems: 'flex-start'
  },
  stateButton: {
    color: 'white',
    border: 'none',
    padding: '10px 30px',
    fontSize: '18px',
    borderRadius: '8px',
    fontWeight: 'bold',
    cursor: 'pointer'
  },
  bottomSection: {
    display: 'flex',
    justifyContent: 'space-between',
    gap: '15px'
  },
  portWrapper: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    width: '130px'
  },
  timerBox: {
    border: '2px solid #60a5fa',
    borderRadius: '12px',
    padding: '10px',
    textAlign: 'center',
    width: '100%',
    marginBottom: '15px',
    fontSize: '14px',
    backgroundColor: '#ffffff'
  },
  yellowRectangle: {
    backgroundColor: '#fde08b',
    width: '100%',
    height: '220px',
    borderRadius: '16px',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '18px',
    fontWeight: 'bold',
    cursor: 'pointer',
    boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
    position: 'relative' // 내부 LED 절대 위치 지정을 위해 추가
  },
  sensorLed: {
    width: '16px',
    height: '16px',
    borderRadius: '50%',
    position: 'absolute',
    top: '15px',
    right: '15px',
    border: '2px solid rgba(0,0,0,0.2)'
  }
};

export default App;