import { create } from 'zustand';

// PLC 데이터 구조 정의 (C#의 DeviceStatus와 일치해야 함)
interface PlcData {
  Di1: boolean;
  Di2: boolean;
  Di3: boolean;
  Di4: boolean;
  ConnectionState: string;
}

interface PlcStore {
  status: PlcData;
  setStatus: (data: PlcData) => void;
}

export const usePlcStore = create<PlcStore>((set) => ({
  status: { 
    Di1: false, 
    Di2: false, 
    Di3: false, 
    Di4: false, 
    ConnectionState: "Initializing..." 
  },
  setStatus: (data) => set({ status: data }),
}));