import { useEffect } from 'react';
import { usePlcStore } from './Store';

function App() {
  const { status, setStatus } = usePlcStore();

  useEffect(() => {
    // 백엔드 주소
    const eventSource = new EventSource("https://localhost:7191/api/plc/stream");

    eventSource.onmessage = (event) => {
      try {
        const parsedData = JSON.parse(event.data);
        setStatus(parsedData); // Zustand 스토어 업데이트
      } catch (err) {
        console.error("Data parsing error:", err);
      }
    };

    eventSource.onerror = (err) => {
      console.error("SSE Connection Error:", err);
      eventSource.close();
    };

    return () => eventSource.close();
  }, [setStatus]);

  return (
    <div style={{ padding: '20px', fontFamily: 'sans-serif' }}>
      <h1>PLC Real-time Monitor (React + SSE)</h1>
      <hr />
      <div style={{ display: 'flex', gap: '20px', marginTop: '20px' }}>
        {/* 제안서 사양을 위한 시각화 기초 단계: LED 표현 */}
        <div style={{ padding: '20px', background: status.Di1 ? '#4CAF50' : '#ddd', borderRadius: '8px' }}>DI 1</div>
        <div style={{ padding: '20px', background: status.Di2 ? '#4CAF50' : '#ddd', borderRadius: '8px' }}>DI 2</div>
        <div style={{ padding: '20px', background: status.Di3 ? '#4CAF50' : '#ddd', borderRadius: '8px' }}>DI 3</div>
        <div style={{ padding: '20px', background: status.Di4 ? '#4CAF50' : '#ddd', borderRadius: '8px' }}>DI 4</div>
      </div>
      <p style={{ marginTop: '30px' }}>
        <strong>통신 상태:</strong> {status.ConnectionState}
      </p>
    </div>
  );
}

export default App;