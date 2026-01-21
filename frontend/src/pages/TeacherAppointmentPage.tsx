import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import {
  createAppointment,
  getStudentAppointments,
  Appointment,
  ApiError,
} from "../services/appointmentService";
import { getTeachers, Teacher } from "../services/teacherService";
import { getInstructorSchedule, ScheduleSlot } from "../services/scheduleService";

type Reason = "question" | "exam" | "other";
type TabType = "request" | "myAppointments";

const TeacherAppointmentPage: React.FC = () => {


  const [activeTab, setActiveTab] = useState<TabType>("request");

  const [lecturerName, setLecturerName] = useState("");
  const [course, setCourse] = useState("");
  const [reason, setReason] = useState<Reason>("question");
  const [otherReason, setOtherReason] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("");
  const [note, setNote] = useState("");

  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [schedule, setSchedule] = useState<ScheduleSlot[]>([]);

  const [loading, setLoading] = useState(false);
  const [loadingAppointments, setLoadingAppointments] = useState(false);
  const [error, setError] = useState("");

  // Sabit saat listesi (09:00 - 17:00, 30dk ara ile)
  const ALL_TIME_SLOTS = [
    "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
    "12:00", "12:30", "13:00", "13:30", "14:00", "14:30",
    "15:00", "15:30", "16:00", "16:30"
  ];

  /* ============================
     DATA LOAD
  ============================ */
  useEffect(() => {
    const loadTeachers = async () => {

      try {
        const data = await getTeachers();
        setTeachers(data);
      } catch (err) {
        console.error("Öğretmen yükleme hatası:", err);
      } finally {

      }
    };
    loadTeachers();
  }, []);

  const loadAppointments = async () => {
    setLoadingAppointments(true);
    try {
      const data = await getStudentAppointments();
      setAppointments(
        data.filter(
          (a) =>
            a.status?.toLowerCase() === "approved" ||
            a.status?.toLowerCase() === "rejected"
        )
      );
    } catch (err) {
      console.error("Randevu yükleme hatası:", err);
    } finally {
      setLoadingAppointments(false);
    }
  };

  useEffect(() => {
    loadAppointments();
  }, []);

  // Öğretmen seçildiğinde schedule'ı yükle
  useEffect(() => {
    const fetchSchedule = async () => {
      if (lecturerName && teachers.length > 0) {
        const teacher = teachers.find((t) => t.name === lecturerName);
        if (teacher) {
          try {
            const data = await getInstructorSchedule(teacher.id);
            setSchedule(data);
          } catch (err) {
            console.error("Schedule loading error:", err);
          }
        }
      } else {
        setSchedule([]);
      }
    };
    fetchSchedule();
  }, [lecturerName, teachers]);

  // Tarih değiştiğinde saati sıfırla (seçilen saat artık müsait olmayabilir)
  useEffect(() => {
    setTime("");
  }, [date, lecturerName]);

  // Seçilen tarih için müsait saatleri hesapla
  const getAvailableTimes = () => {
    // Tarih seçili değilse boş dön
    if (!date) return [];

    // Eğer schedule henüz yüklenmediyse veya boşsa, tüm saatler müsaittir (varsayım)
    if (!schedule || schedule.length === 0) return ALL_TIME_SLOTS;

    const dateObj = new Date(date);
    const day = dateObj.getDay(); // 0=Sun, 1=Mon, ..., 6=Sat

    // Haftasonu ise (0 veya 6), boş dön
    if (day === 0 || day === 6) return [];

    // O güne ait DOLU slotları (hocanın derslerini) bul
    // Format: "09:00" string'i
    const busyStarts = schedule
      .filter((s) => s.dayOfWeek === day)
      .map(s => s.startTime);

    // Dersler 50 dk olduğu için, başlangıç saati VE bir sonraki 30 dk'lık dilim doludur.
    // Örn: 09:00 dersi -> 09:00 ve 09:30 slotlarını kapatır.
    const allBusySlots: string[] = [];

    busyStarts.forEach((start) => {
      allBusySlots.push(start);
      // Bir sonraki 30 dk slotunu bul
      const parts = start.split(":");
      if (parts.length === 2) {
        let h = parseInt(parts[0]);
        let m = parseInt(parts[1]);

        // 30 dk ekle
        m += 30;
        if (m >= 60) {
          m -= 60;
          h += 1;
        }

        const nextSlot = `${String(h).padStart(2, "0")}:${String(m).padStart(
          2,
          "0"
        )}`;
        allBusySlots.push(nextSlot);
      }
    });

    // Tüm saatlerden dolu olanları çıkar
    // Dolu olmayanlar = Müsait olanlar
    const freeSlots = ALL_TIME_SLOTS.filter(
      (slot) => !allBusySlots.includes(slot)
    );

    return freeSlots;
  };

  const availableTimes = getAvailableTimes();

  /* ============================
     SUBMIT
  ============================ */
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

      await createAppointment({
        lecturerName,
        course,
        reason: finalReason,
        date,
        time,
        note: note || undefined,
      });

      alert("Randevu talebiniz başarıyla oluşturuldu.");

      setLecturerName("");
      setCourse("");
      setReason("question");
      setOtherReason("");
      setDate("");
      setTime("");
      setNote("");

      await loadAppointments();
      setActiveTab("myAppointments");
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Randevu oluşturulurken hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  /* ============================
     UI
  ============================ */
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* ÜST BAR */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto px-6 py-6 flex justify-between">
          <h1 className="text-2xl font-semibold">Öğretim Elemanıyla Görüşme</h1>
          <Link to="/ogrenci" className="underline text-sm">
            Öğrenci anasayfasına dön
          </Link>
        </div>
      </header>

      <main className="flex-1 p-8">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-8 max-w-6xl mx-auto">
          {/* SOL ANA KART */}
          <section className="lg:col-span-3 bg-white rounded-xl border shadow">
            {/* SEKME BAŞLIKLARI */}
            <div className="flex border-b">
              <button
                onClick={() => setActiveTab("request")}
                className={`flex-1 py-3 text-sm font-medium ${activeTab === "request"
                  ? "border-b-2 border-[#d71920] text-[#d71920]"
                  : "text-slate-500"
                  }`}
              >
                Randevu Talebi
              </button>

              <button
                onClick={() => setActiveTab("myAppointments")}
                className={`flex-1 py-3 text-sm font-medium ${activeTab === "myAppointments"
                  ? "border-b-2 border-[#d71920] text-[#d71920]"
                  : "text-slate-500"
                  }`}
              >
                Randevularım
              </button>
            </div>

            <div className="p-6">
              {error && (
                <div className="bg-red-100 border border-red-400 text-red-700 p-3 rounded mb-4">
                  {error}
                </div>
              )}

              {/* ================= RANDEVU TALEBİ ================= */}
              {activeTab === "request" && (
                <form onSubmit={handleSubmit} className="space-y-4">
                  <select
                    value={lecturerName}
                    onChange={(e) => setLecturerName(e.target.value)}
                    required
                    className="w-full border rounded px-3 py-2 text-sm"
                  >
                    <option value="">Öğretim elemanı seçiniz</option>
                    {teachers.map((t) => (
                      <option key={t.id} value={t.name}>
                        {t.name}
                      </option>
                    ))}
                  </select>

                  <input
                    value={course}
                    onChange={(e) => setCourse(e.target.value)}
                    placeholder="Ders"
                    className="w-full border rounded px-3 py-2 text-sm"
                  />

                  <div className="flex gap-4 text-sm">
                    <label>
                      <input
                        type="radio"
                        checked={reason === "question"}
                        onChange={() => setReason("question")}
                      />{" "}
                      Soru
                    </label>
                    <label>
                      <input
                        type="radio"
                        checked={reason === "exam"}
                        onChange={() => setReason("exam")}
                      />{" "}
                      Sınav
                    </label>
                    <label>
                      <input
                        type="radio"
                        checked={reason === "other"}
                        onChange={() => setReason("other")}
                      />{" "}
                      Diğer
                    </label>
                  </div>

                  {reason === "other" && (
                    <textarea
                      value={otherReason}
                      onChange={(e) => setOtherReason(e.target.value)}
                      className="w-full border rounded px-3 py-2 text-sm"
                    />
                  )}

                  <div className="grid grid-cols-2 gap-4">
                    <input
                      type="date"
                      value={date}
                      onChange={(e) => setDate(e.target.value)}
                      required
                      className="border rounded px-3 py-2 text-sm"
                    />
                    <select
                      value={time}
                      onChange={(e) => setTime(e.target.value)}
                      required
                      className="border rounded px-3 py-2 text-sm w-full"
                      disabled={!date || availableTimes.length === 0}
                    >
                      <option value="">
                        {!date
                          ? "Önce Tarih Seçiniz"
                          : availableTimes.length === 0
                            ? "Müsaitlik Yok"
                            : "Saat Seçiniz"}
                      </option>
                      {availableTimes.map((t) => (
                        <option key={t} value={t}>
                          {t}
                        </option>
                      ))}
                    </select>
                  </div>

                  <button
                    disabled={loading}
                    className="w-full bg-[#d71920] text-white py-2 rounded"
                  >
                    {loading ? "Gönderiliyor..." : "Randevu Talep Et"}
                  </button>
                </form>
              )}

              {/* ================= RANDEVULARIM ================= */}
              {activeTab === "myAppointments" && (
                <div className="space-y-3">
                  {loadingAppointments ? (
                    <p className="text-sm text-slate-500">Yükleniyor...</p>
                  ) : appointments.length === 0 ? (
                    <p className="text-sm text-slate-500">
                      Henüz randevu yok.
                    </p>
                  ) : (
                    appointments.map((apt) => (
                      <div
                        key={apt.id}
                        className={`p-4 rounded border ${apt.status?.toLowerCase() === "approved"
                          ? "bg-green-50 border-green-200"
                          : "bg-red-50 border-red-200"
                          }`}
                      >
                        {/* Ders */}
                        <p className="font-semibold text-sm text-slate-900">
                          {(apt as any).subject ||
                            apt.course ||
                            "Ders belirtilmemiş"}
                        </p>

                        {/* Hoca */}
                        <p className="text-xs text-slate-600 mt-1">
                          Öğretim Elemanı:{" "}
                          {(apt as any).teacherName ||
                            (apt as any).lecturerName ||
                            "Bilinmiyor"}
                        </p>

                        {/* Tarih – Saat */}
                        <p className="text-xs text-slate-600">
                          {apt.date
                            ? new Date(apt.date).toLocaleDateString("tr-TR")
                            : "Tarih yok"}{" "}
                          –{" "}
                          {apt.time
                            ? typeof apt.time === "string"
                              ? apt.time
                              : `${String(
                                (apt.time as any).hours || 0
                              ).padStart(2, "0")}:${String(
                                (apt.time as any).minutes || 0
                              ).padStart(2, "0")}`
                            : "Saat yok"}
                        </p>

                        {/* Durum */}
                        <span
                          className={`inline-block mt-2 px-2 py-0.5 rounded text-[11px] font-medium ${apt.status?.toLowerCase() === "approved"
                            ? "bg-green-100 text-green-800"
                            : "bg-red-100 text-red-800"
                            }`}
                        >
                          {apt.status?.toLowerCase() === "approved"
                            ? "Onaylandı"
                            : "Reddedildi"}
                        </span>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </section>

          {/* SAĞ BİLGİ PANELİ */}
          <section className="lg:col-span-2 bg-white rounded-xl border p-5 shadow">
            <h2 className="text-sm font-semibold mb-3">Bilgilendirme</h2>

            <ul className="text-sm text-slate-600 space-y-2 list-disc pl-4">
              <li>Randevular öğretim elemanı tarafından onaylanır veya reddedilir.</li>
              <li>Randevu saatleri <strong>09:00 – 17:00</strong> arasıdır.</li>
              <li>Seçilebilir saatler <strong>30 dakika</strong> aralıklarla listelenir.</li>
              <li>
                Randevu durumunu <strong>“Randevularım”</strong> sekmesinden takip
                edebilirsiniz.
              </li>
            </ul>
          </section>

        </div>
      </main>
    </div>
  );
};

export default TeacherAppointmentPage;
