import apiClient from '../api/axios';

// Frontend'den gönderilen format
export interface AppointmentRequest {
  lecturerName: string;
  course: string;
  reason: string;
  date: string;
  time: string;
  note?: string;
}

// Backend'in beklediği format - direkt root seviyesinde
// Backend artık time'ı string formatında ("HH:mm") kabul ediyor
// studentId göndermemize gerek yok, JWT token'dan otomatik alınıyor
// teacherName, teacherId veya teacherEmail gönderebiliriz
export interface BackendAppointmentRequest {
  teacherName?: string; // Öğretmen adı ile
  teacherId?: number; // Öğretmen ID ile
  teacherEmail?: string; // Öğretmen email ile
  date: string; // ISO datetime string: "2025-12-24T17:55:50.911Z"
  time: string; // TimeSpan string formatı: "HH:mm" (örn: "14:30")
  subject: string;
  requestReason?: string; // Öğrencinin yazdığı sebep (diğer seçeneğinde özel metin)
}

export interface Appointment {
  id: string;
  studentId: string;
  instructorId: string;
  course: string;
  reason: string;
  date: string;
  time: string;
  note?: string;
  status: 'pending' | 'approved' | 'rejected';
  createdAt: string;
}

export interface ApiError {
  message: string;
  status?: number;
}

// Helper to map backend DTO to frontend Appointment interface
const mapDtoToAppointment = (dto: any): Appointment => {
  return {
    ...dto,
    // Handle both camelCase and PascalCase from backend
    reason: dto.requestReason || dto.RequestReason || dto.reason || '',
    course: dto.subject || dto.Subject || dto.course || '',
    note: dto.note || dto.rejectionReason || dto.RejectionReason,
    studentName: dto.studentName || dto.StudentName,
    teacherName: dto.teacherName || dto.TeacherName,
    date: dto.date || dto.Date,
    time: dto.time || dto.Time,
    status: dto.status || dto.Status,
  };
};

// Randevu oluştur
export const createAppointment = async (
  appointment: AppointmentRequest
): Promise<Appointment> => {
  try {
    // Backend endpoint'i /Appointment (tekil, büyük A ile başlıyor)
    // Backend'in beklediği formata dönüştür

    // Date ve time'ı birleştir (ISO format)
    let isoDateTime = '';
    if (appointment.date && appointment.time) {
      // Tarih formatını düzelt (DD.MM.YYYY -> YYYY-MM-DD)
      const dateParts = appointment.date.split('.');
      if (dateParts.length === 3) {
        const [day, month, year] = dateParts;
        const isoDate = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
        isoDateTime = `${isoDate}T${appointment.time}:00.000Z`;
      } else {
        // Eğer zaten YYYY-MM-DD formatındaysa
        isoDateTime = `${appointment.date}T${appointment.time}:00.000Z`;
      }
    }

    // Backend artık time'ı string formatında ("HH:mm") kabul ediyor
    // Time'ı direkt string olarak gönder (örn: "14:30")
    const timeString = appointment.time; // Zaten "HH:mm" formatında

    // Course değerini kontrol et
    if (!appointment.course || appointment.course.trim() === '') {
      throw {
        message: 'Ders alanı boş olamaz',
        status: 400,
      } as ApiError;
    }

    // Öğretmen adını kontrol et
    if (!appointment.lecturerName || appointment.lecturerName.trim() === '') {
      throw {
        message: 'Öğretim elemanı adı boş olamaz',
        status: 400,
      } as ApiError;
    }

    // Backend'in beklediği format - direkt root seviyesinde
    // studentId göndermemize gerek yok, JWT token'dan otomatik alınıyor
    // teacherName kullanıyoruz (formdan gelen lecturerName)
    const teacherNameValue = appointment.lecturerName.trim();

    if (!teacherNameValue) {
      throw {
        message: 'Öğretim elemanı adı boş olamaz',
        status: 400,
      } as ApiError;
    }

    const backendRequest: BackendAppointmentRequest = {
      teacherName: teacherNameValue, // Öğretmen adı ile
      date: isoDateTime,
      time: timeString, // String format: "HH:mm" (örn: "14:30")
      subject: appointment.course.trim(), // course -> subject, boşlukları temizle
      requestReason: appointment.reason, // Öğrencinin yazdığı sebep (diğer seçeneğinde özel metin)
    };

    console.log('Lecturer name:', appointment.lecturerName);
    console.log('Backend request body:', JSON.stringify(backendRequest, null, 2)); // Debug için - detaylı göster

    const response = await apiClient.post<any>('/Appointment', backendRequest);
    return mapDtoToAppointment(response.data);
  } catch (error: any) {
    console.error('Create appointment error:', error.response?.data); // Debug için

    // Backend validation hatalarını parse et
    let errorMessage = error.response?.data?.title || error.response?.data?.message || 'Randevu oluşturulurken bir hata oluştu';

    // 500 hatası için daha detaylı mesaj
    if (error.response?.status === 500) {
      const detailMessage = error.response?.data?.message || error.response?.data?.detail || '';
      if (detailMessage) {
        errorMessage = `Randevu oluşturulurken bir hata oluştu: ${detailMessage}`;
      } else {
        errorMessage = 'Randevu oluşturulurken bir hata oluştu. Lütfen öğretim elemanı adını kontrol edin.';
      }
    } else if (error.response?.data?.errors) {
      // Validation hatalarını birleştir
      const validationErrors: string[] = [];
      Object.keys(error.response.data.errors).forEach((key) => {
        const fieldErrors = error.response.data.errors[key];
        if (Array.isArray(fieldErrors)) {
          fieldErrors.forEach((err: string) => {
            validationErrors.push(`${key}: ${err}`);
          });
        }
      });

      if (validationErrors.length > 0) {
        errorMessage = `Validation hataları:\n${validationErrors.join('\n')}`;
      }
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    }

    throw {
      message: errorMessage,
      status: error.response?.status,
    } as ApiError;
  }
};

// Öğrencinin randevularını getir
export const getStudentAppointments = async (): Promise<Appointment[]> => {
  try {
    // Backend'de my-appointments endpoint'i var
    const response = await apiClient.get<any[]>('/Appointment/my-appointments');
    return response.data.map(mapDtoToAppointment);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Randevular yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Öğretim elemanının randevularını getir
export const getInstructorAppointments = async (): Promise<Appointment[]> => {
  try {
    // Backend'de my-appointments endpoint'i var (hem öğrenci hem öğretim elemanı için)
    const response = await apiClient.get<any[]>('/Appointment/my-appointments');
    return response.data.map(mapDtoToAppointment);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Randevular yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Öğretim elemanının bekleyen randevu taleplerini getir
export const getPendingRequests = async (): Promise<Appointment[]> => {
  try {
    // Backend'de pending-requests endpoint'i var (sadece öğretim elemanı için)
    const response = await apiClient.get<any[]>('/Appointment/pending-requests');
    return response.data.map(mapDtoToAppointment);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Bekleyen randevu talepleri yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Randevu durumunu güncelle (onayla/reddet)
export const updateAppointmentStatus = async (
  appointmentId: string,
  status: 'approved' | 'rejected',
  appointment?: Appointment, // Mevcut appointment bilgileri (opsiyonel)
  rejectionReason?: string
): Promise<Appointment> => {
  try {
    // Backend'de PUT /api/Appointment/{id} kullanılıyor
    // Backend tüm appointment bilgilerini bekliyor (date, time, subject, status)

    let updateData: any;

    if (appointment) {
      // Mevcut appointment bilgilerini kullan
      // Date'i ISO formatına çevir
      let isoDate = appointment.date;
      if (!isoDate.includes('T')) {
        // Eğer sadece tarih varsa, time ile birleştir
        isoDate = `${appointment.date}T${appointment.time}:00.000Z`;
      }

      // Backend artık time'ı string formatında ("HH:mm") kabul ediyor
      const timeString = appointment.time; // Zaten "HH:mm" formatında

      // Status'u number'a çevir (0: pending, 1: approved, 2: rejected gibi)
      const statusNumber = status === 'approved' ? 1 : status === 'rejected' ? 2 : 0;

      updateData = {
        date: isoDate,
        time: timeString, // String format: "HH:mm" (örn: "14:30")
        subject: appointment.course || '', // Backend'de subject, frontend'de course
        status: statusNumber,
      };

      if (status === 'rejected' && rejectionReason) {
        updateData.rejectionReason = rejectionReason;
      }
    } else {
      // Eğer appointment bilgisi yoksa, sadece status gönder (backend kabul ederse)
      const statusNumber = status === 'approved' ? 1 : status === 'rejected' ? 2 : 0;
      updateData = {
        status: statusNumber,
      };
      if (status === 'rejected' && rejectionReason) {
        updateData.rejectionReason = rejectionReason;
      }
    }

    const response = await apiClient.put<any>(`/Appointment/${appointmentId}`, updateData);
    return mapDtoToAppointment(response.data);
  } catch (error: any) {
    console.error('Update appointment error:', error.response?.data);
    throw {
      message: error.response?.data?.message || 'Randevu durumu güncellenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Randevu sil
export const deleteAppointment = async (appointmentId: string): Promise<void> => {
  try {
    await apiClient.delete(`/Appointment/${appointmentId}`);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Randevu silinirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

