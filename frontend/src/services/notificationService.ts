import apiClient from '../api/axios';

export interface Notification {
  id: number;
  title: string;
  message: string;
  type: string;
  appointmentId?: number;
  isRead: boolean;
  createdAt: string;
}

export interface ApiError {
  message: string;
  status?: number;
}

// Bildirimleri getir
export const getNotifications = async (): Promise<Notification[]> => {
  try {
    const response = await apiClient.get<Notification[]>('/Notification');
    return response.data;
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Bildirimler yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Bildirimi okundu olarak işaretle
export const markNotificationAsRead = async (notificationId: number): Promise<void> => {
  try {
    await apiClient.put(`/Notification/${notificationId}/read`);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Bildirim okundu olarak işaretlenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

