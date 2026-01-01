import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createAppointment, getStudentAppointments, Appointment, ApiError } from "../services/appointmentService";
import { getTeachers, Teacher } from "../services/teacherService";

type Reason = "question" | "exam" | "other";

const TeacherAppointmentPage: React.FC = () => {
  const navigate = useNavigate();
  const [lecturerName, setLecturerName] = useState("");
  const [course, setCourse] = useState("");
  const [reason, setReason] = useState<Reason>("question");
  const [otherReason, setOtherReason] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("");
  const [note, setNote] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [loadingTeachers, setLoadingTeachers] = useState(false);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loadingAppointments, setLoadingAppointments] = useState(false);

  // Öğretmen listesini yükle
  useEffect(() => {
    const loadTeachers = async () => {
      setLoadingTeachers(true);
      try {
        const data = await getTeachers();
        setTeachers(data);
      } catch (err) {
        console.error("Öğretmenler yüklenirken hata:", err);
        // Hata olsa bile devam et, manuel giriş yapılabilir
      } finally {
        setLoadingTeachers(false);
      }
    };
    loadTeachers();
  }, []);

  // Randevuları yükle
  useEffect(() => {
    const loadAppointments = async () => {
      setLoadingAppointments(true);
      try {
        const data = await getStudentAppointments();
        // Sadece onaylanan ve reddedilen randevuları göster
        const filteredData = data.filter(
          (apt) => apt.status?.toLowerCase() === "approved" || apt.status?.toLowerCase() === "rejected"
        );
        setAppointments(filteredData);
      } catch (err) {
        console.error("Randevular yüklenirken hata:", err);
      } finally {
        setLoadingAppointments(false);
      }
    };
    loadAppointments();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const finalReason =
        reason === "other"
          ? otherReason
          : reason === "question"
          ? "Soru sorma"
          : "Sınav kağıdına bakma";

      // Token kontrolü
      const token = localStorage.getItem("token");
      if (!token) {
        throw {
          message: "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.",
        } as ApiError;
      }

      // Saat validation: 09:00-17:00 arası ve 30 dk aralıklarla (17:00 dahil)
      if (!time) {
        setError("Lütfen bir saat seçin.");
        setLoading(false);
        return;
      }

      const [hourStr, minuteStr] = time.split(":");
      const hour = parseInt(hourStr, 10);
      const minute = parseInt(minuteStr, 10);

      // 09:00-17:00 arası kontrol (17:00 dahil)
      if (hour < 9 || hour > 17) {
        setError(
          "Lütfen 09:00 ile 17:00 arasında bir saat seçin."
        );
        setLoading(false);
        return;
      }

      // 17:00'dan sonraki saatler kontrolü
      if (hour === 17 && minute > 0) {
        setError(
          "Lütfen 17:00'a kadar bir saat seçin."
        );
        setLoading(false);
        return;
      }

      // 30 dakika aralıklarla kontrol (00 veya 30)
      if (minute !== 0 && minute !== 30) {
        setError(
          "Lütfen 30 dakika aralıklarla bir saat seçin (örn: 09:00, 09:30, 10:00)."
        );
        setLoading(false);
        return;
      }

      // Backend artık studentId'yi JWT token'dan otomatik alıyor
      // teacherName gönderiyoruz (formdan gelen lecturerName)
      await createAppointment({
        lecturerName,
        course,
        reason: finalReason,
        date,
        time,
        note: note || undefined,
      });

      alert("Randevu talebiniz başarıyla oluşturuldu!");

      // Formu temizle
      setLecturerName("");
      setCourse("");
      setReason("question");
      setOtherReason("");
      setDate("");
      setTime("");
      setNote("");

      // Randevuları yenile
      const data = await getStudentAppointments();
      const filteredData = data.filter(
        (apt) => apt.status?.toLowerCase() === "approved" || apt.status?.toLowerCase() === "rejected"
      );
      setAppointments(filteredData);

      // Öğrenci dashboard'una dön
      navigate("/ogrenci");
    } catch (err) {
      const apiError = err as ApiError;
      // Hata mesajını göster (validation hataları için)
      const errorMsg =
        apiError.message || "Randevu oluşturulurken bir hata oluştu";
      setError(errorMsg);
      console.error("Randevu oluşturma hatası:", errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const isOtherSelected = reason === "other";

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Öğretim Elemanıyla Görüşme</h1>
          <Link to="/ogrenci" className="text-sm underline hover:opacity-90">
            Öğrenci anasayfasına dön
          </Link>
        </div>
      </header>

      {/* İçerik */}
      <main className="flex-1 px-4 py-8">
        <div className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Sol taraf - Form */}
          <div className="lg:col-span-2">
            <form
              onSubmit={handleSubmit}
              className="w-full bg-white rounded-2xl shadow-md border border-slate-200 p-6 space-y-4"
            >
          <h2 className="text-xl font-semibold text-slate-900 mb-2">
            Randevu Talep Formu
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Görüşme yapmak istediğiniz öğretim elemanını, ders bilgisini ve
            uygun olduğunuz tarih/saat bilgisini giriniz.
          </p>

          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {/* Öğretim elemanı adı */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Öğretim Elemanı
            </label>
            {loadingTeachers ? (
              <div className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-500">
                Öğretmenler yükleniyor...
              </div>
            ) : teachers.length > 0 ? (
              <select
                value={lecturerName}
                onChange={(e) => setLecturerName(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              >
                <option value="">Öğretim elemanı seçiniz</option>
                {teachers.map((teacher) => (
                  <option key={teacher.id} value={teacher.name}>
                    {teacher.name}
                  </option>
                ))}
              </select>
            ) : (
              <input
                type="text"
                value={lecturerName}
                onChange={(e) => setLecturerName(e.target.value)}
                placeholder="Öğretim elemanı adı"
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              />
            )}
          </div>

          {/* Ders */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ders
            </label>
            <input
              type="text"
              value={course}
              onChange={(e) => setCourse(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Görüşme sebebi */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Görüşme sebebi
            </label>
            <div className="flex flex-col gap-2 text-sm text-slate-700">
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="question"
                  checked={reason === "question"}
                  onChange={() => setReason("question")}
                />
                <span>Soru sorma</span>
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="exam"
                  checked={reason === "exam"}
                  onChange={() => setReason("exam")}
                />
                <span>Sınav kağıdına bakma</span>
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="other"
                  checked={reason === "other"}
                  onChange={() => setReason("other")}
                />
                <span>Diğer</span>
              </label>
            </div>

            {isOtherSelected && (
              <textarea
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                rows={2}
                //kaç elemanlık metin giriliyosa ekle
                value={otherReason}
                onChange={(e) => setOtherReason(e.target.value)}
              />
            )}
          </div>

          {/* Tarih - Saat */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Tarih
              </label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Saat
              </label>
              <select
                value={time}
                onChange={(e) => setTime(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              >
                <option value="">Saat seçiniz</option>
                {(() => {
                  const times = [];
                  for (let hour = 9; hour <= 17; hour++) {
                    times.push(`${String(hour).padStart(2, '0')}:00`);
                    if (hour < 17) {
                      times.push(`${String(hour).padStart(2, '0')}:30`);
                    }
                  }
                  return times.map((timeValue) => (
                    <option key={timeValue} value={timeValue}>
                      {timeValue}
                    </option>
                  ));
                })()}
              </select>
            </div>
          </div>

          {/* Gönder butonu */}
          <div className="pt-2">
            <button
              type="submit"
              disabled={loading}
              className="w-full rounded-lg bg-blue-600 text-white text-sm font-medium py-2.5 hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? "Gönderiliyor..." : "Randevu talebi gönder"}
            </button>
          </div>
        </form>
          </div>

          {/* Sağ taraf - Randevularım */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-6">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">
                Randevularım
              </h2>
              
              {loadingAppointments ? (
                <div className="text-center py-8">
                  <p className="text-slate-500 text-sm">Yükleniyor...</p>
                </div>
              ) : appointments.length === 0 ? (
                <p className="text-slate-500 text-sm">
                  Henüz onaylanan veya reddedilen randevu yok.
                </p>
              ) : (
                <div className="space-y-3 max-h-[600px] overflow-y-auto">
                  {appointments.map((apt) => (
                    <div
                      key={apt.id}
                      className={`border rounded-lg p-4 ${
                        apt.status?.toLowerCase() === "approved"
                          ? "border-green-200 bg-green-50"
                          : "border-red-200 bg-red-50"
                      }`}
                    >
                      <div className="flex items-start justify-between mb-2">
                        <div className="flex-1">
                          <p className="font-semibold text-slate-900 text-sm">
                            {(apt as any).subject || apt.course || "Ders belirtilmemiş"}
                          </p>
                          <p className="text-xs text-slate-600 mt-1">
                            Öğretmen: {(apt as any).teacherName || "Bilinmiyor"}
                          </p>
                          <p className="text-xs text-slate-600">
                            {apt.date ? new Date(apt.date).toLocaleDateString('tr-TR') : 'Tarih belirtilmemiş'} - {apt.time ? (typeof apt.time === 'string' ? apt.time : (typeof apt.time === 'object' && apt.time !== null ? `${String((apt.time as any).hours || 0).padStart(2, '0')}:${String((apt.time as any).minutes || 0).padStart(2, '0')}` : 'Saat belirtilmemiş')) : 'Saat belirtilmemiş'}
                          </p>
                          {(apt as any).rejectionReason && (
                            <p className="text-xs text-red-600 mt-1 italic">
                              Red Nedeni: {(apt as any).rejectionReason}
                            </p>
                          )}
                        </div>
                        <span
                          className={`px-2 py-1 rounded text-xs font-medium ${
                            apt.status?.toLowerCase() === "approved"
                              ? "bg-green-100 text-green-800"
                              : "bg-red-100 text-red-800"
                          }`}
                        >
                          {apt.status?.toLowerCase() === "approved" ? "Onaylandı" : "Reddedildi"}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default TeacherAppointmentPage;
