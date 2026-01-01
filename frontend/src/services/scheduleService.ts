import apiClient from '../api/axios';

export interface ScheduleSlot {
  id?: number;
  dayOfWeek: number;
  startTime: string;
  courseName: string;
  slot: string; // Format: "Pzt-09.00-09.50"
}

export interface SaveScheduleRequest {
  slots: Array<{
    slot: string;
    courseName?: string;
  }>;
}

export const getMySchedule = async (): Promise<ScheduleSlot[]> => {
  try {
    const response = await apiClient.get("/Schedule/my-schedule");
    return response.data;
  } catch (error: any) {
    console.error("Ders programı yükleme hatası:", error);
    throw {
      message: error.response?.data?.message || "Ders programı yüklenirken bir hata oluştu",
      status: error.response?.status,
    };
  }
};

export const saveSchedule = async (slots: string[]): Promise<void> => {
  try {
    const request: SaveScheduleRequest = {
      slots: slots.map((slot) => ({
        slot,
        courseName: "", // İsteğe bağlı: ders adı eklenebilir
      })),
    };
    await apiClient.post("/Schedule/save", request);
  } catch (error: any) {
    console.error("Ders programı kaydetme hatası:", error);
    throw {
      message: error.response?.data?.message || "Ders programı kaydedilirken bir hata oluştu",
      status: error.response?.status,
    };
  }
};

