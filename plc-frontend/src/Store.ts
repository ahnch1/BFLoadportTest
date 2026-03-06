import { create } from 'zustand';

// PLC 데이터 구조 정의 (C#의 DeviceStatus와 일치해야 함)
export interface PlcData {
  Di1: boolean; // Tipbox In 센서 상태
  Di2: boolean; // DeepWell In 센서 상태
  Di3: boolean; // DeepWell Out 센서 상태
  Di4: boolean; // Agar In 센서 상태
  ConnectionState: string;
}

// 애플리케이션 전체 상태 정의 (데모 상태 + PLC I/O 상태)
interface AppState {
  // 1. 데모 루틴 상태
  machineState: 'Stop' | 'Ready' | 'Run';
  tipBoxCount: number;
  deepWellCount: number;
  tipLoadTimer: number;
  deepWellInTimer: number;
  deepWellOutTimer: number;
  agarPlateTimer: number;

  // 2. PLC I/O 데이터 상태
  plcData: PlcData;

  // 3. 상태 업데이트 액션
  setDemoState: (state: Partial<Omit<AppState, 'plcData' | 'setDemoState' | 'setPlcData'>>) => void;
  setPlcData: (data: Partial<PlcData>) => void;
}

export const useStore = create<AppState>((set) => ({
  // 데모 루틴 초기값
  machineState: 'Stop',
  tipBoxCount: 0,
  deepWellCount: 0,
  tipLoadTimer: 0,
  deepWellInTimer: 0,
  deepWellOutTimer: 0,
  agarPlateTimer: 0,

  // PLC 데이터 초기값
  plcData: {
    Di1: false,
    Di2: false,
    Di3: false,
    Di4: false,
    ConnectionState: 'Disconnected',
  },

  // 데모 상태 업데이트
  setDemoState: (state) => set((prev) => ({ ...prev, ...state })),
  
  // PLC I/O 데이터 업데이트 (SSE 수신 시 호출)
  setPlcData: (data) => set((prev) => ({
    plcData: { ...prev.plcData, ...data }
  })),
}));