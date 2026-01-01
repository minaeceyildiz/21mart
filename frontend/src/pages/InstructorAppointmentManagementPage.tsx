import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import {
  getInstructorAppointments,
  getPendingRequests,
  updateAppointmentStatus,
  Appointment,
  ApiError,
} from "../services/appointmentService";
import { getMySchedule, saveSchedule, ScheduleSlot } from "../services/scheduleService";

const days = ["Pzt", "Sal", "Çar", "Per", "Cum"];
const times = [
  "09.00-09.50",
  "10.00-10.50",
  "11.00-11.50",
  "12.00-12.50",
  "13.00-13.50",
  "14.00-14.50",
  "15.00-15.50",
  "16.00-16.50",
];

type TabType = "requests" | "myAppointments";

const InstructorAppointmentManagement: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>("requests");
  const [selectedSlots, setSelectedSlots] = useState<string[]>([]);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");
  const [savingSchedule, setSavingSchedule] = useState(false);

  useEffect(() => {
    loadAppointments();
  }, [activeTab]);

  useEffect(() => {
    loadSchedule();
  }, []); // Sadece sayfa ilk yüklendiğinde çağrıl

  const loadAppointments = async () => {
    setLoading(true);
    setError("");
    try {
      // "Gelen Talepler" sekmesi için pending-requests endpoint'ini çağır
      // "Randevularım" sekmesi için my-appointments endpoint'ini çağır
      let data: Appointment[];
      if (activeTab === "requests") {
        console.log("Pending requests yükleniyor...");
        data = await getPendingRequests();
        console.log("Pending requests yüklendi:", data);
      } else {
        console.log("Tüm randevular yükleniyor...");
        data = await getInstructorAppointments();
        console.log("Tüm randevular yüklendi:", data);
      }
      setAppointments(data);
    } catch (err) {
      const apiError = err as ApiError;
      console.error("Randevu yükleme hatası:", err);
      setError(apiError.message || "Randevular yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async (
    appointmentId: string,
    status: "approved" | "rejected"
  ) => {
    try {
      // Mevcut appointment'ı bul
      const appointment = appointments.find((apt) => apt.id === appointmentId);
      if (!appointment) {
        alert("Randevu bulunamadı");
        return;
      }

      await updateAppointmentStatus(appointmentId, status, appointment);
      await loadAppointments(); // Listeyi yenile
    } catch (err) {
      const apiError = err as ApiError;
      alert(
        apiError.message || "Randevu durumu güncellenirken bir hata oluştu"
      );
    }
  };

  const toggleSlot = (key: string) => {
    setSelectedSlots((prev) =>
      prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]
    );
  };

  const loadSchedule = async () => {
    try {
      const schedule = await getMySchedule();
      const slots = schedule.map((s) => s.slot);
      setSelectedSlots(slots);
    } catch (err) {
      console.error("Ders programı yükleme hatası:", err);
      // Hata olsa bile devam et, sadece boş liste ile başla
    }
  };

  const handleSaveSchedule = async () => {
    if (selectedSlots.length === 0) {
      alert("Lütfen en az bir saat seçin.");
      return;
    }

    setSavingSchedule(true);
    try {
      await saveSchedule(selectedSlots);
      alert("Ders programı başarıyla kaydedildi!");
    } catch (err: any) {
      const apiError = err as ApiError;
      alert(apiError.message || "Ders programı kaydedilirken bir hata oluştu");
    } finally {
      setSavingSchedule(false);
    }
  };

  // Pending randevular (gelen talepler)
  // Not: "requests" sekmesi için zaten pending-requests endpoint'i çağrılıyor,
  // bu yüzden tüm appointments zaten pending. Ama yine de filtreleme yapalım güvenlik için.
  const pendingAppointments = activeTab === "requests" 
    ? appointments // pending-requests endpoint'i zaten sadece pending döndürüyor
    : appointments.filter((apt) => apt.status?.toLowerCase() === "pending");

  // Approved randevular (onaylanmış randevular)
  const approvedAppointments = appointments.filter(
    (apt) => apt.status?.toLowerCase() === "approved"
  );
  
  // Debug: Randevuları console'a yazdır
  console.log("Appointments state:", appointments);
  console.log("Pending appointments:", pendingAppointments);
  console.log("Active tab:", activeTab);

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Randevu Yönetimi</h1>
          <Link
            to="/ogretim-elemani"
            className="text-sm underline hover:opacity-90"
          >
            Öğretim elemanı anasayfasına dön
          </Link>
        </div>
      </header>

      <div className="flex-1 p-8">
        <div className="grid grid-cols-1 lg:grid-cols-5 gap-8">
          {/* SOL TARAF */}
          <section className="lg:col-span-3 bg-white rounded-xl border shadow">
            {/* Sekmeler */}
            <div className="flex border-b">
              <button
                onClick={() => setActiveTab("requests")}
                className={`flex-1 py-3 text-sm font-medium ${
                  activeTab === "requests"
                    ? "border-b-2 border-[#d71920] text-[#d71920]"
                    : "text-slate-500"
                }`}
              >
                Gelen Talepler
              </button>

              <button
                onClick={() => setActiveTab("myAppointments")}
                className={`flex-1 py-3 text-sm font-medium ${
                  activeTab === "myAppointments"
                    ? "border-b-2 border-[#d71920] text-[#d71920]"
                    : "text-slate-500"
                }`}
              >
                Randevularım
              </button>
            </div>

            {/* İçerik */}
            <div className="p-6">
              {error && (
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
                  {error}
                </div>
              )}

              {loading && (
                <div className="text-center py-8">
                  <p className="text-slate-500">Yükleniyor...</p>
                </div>
              )}

              {!loading && activeTab === "requests" && (
                <div className="space-y-4">
                  {pendingAppointments.length === 0 ? (
                    <p className="text-slate-500 text-sm">
                      Henüz gelen randevu talebi yok.
                    </p>
                  ) : (
                    pendingAppointments.map((apt) => (
                      <div
                        key={apt.id}
                        className="border border-slate-200 rounded-lg p-4 bg-slate-50"
                      >
                        <div className="flex justify-between items-start mb-2">
                          <div>
                            <p className="font-semibold text-slate-900">
                              {(apt as any).subject || apt.course || "Ders belirtilmemiş"}
                            </p>
                            <p className="text-sm text-slate-600 mt-1">
                              Öğrenci: {(apt as any).studentName || "Bilinmiyor"}
                            </p>
                            <p className="text-sm text-slate-600 mt-1">
                              Sebep: {(() => {
                                const reason = (apt as any).requestReason || apt.reason || "";
                                const reasonLower = reason.toLowerCase().trim();
                                
                                // Eğer reason "question", "exam", "other" gibi enum değerleriyse Türkçe'ye çevir
                                // Aksi halde öğrencinin yazdığı özel metni göster
                                if (reasonLower === "question") {
                                  return "Soru sorma";
                                } else if (reasonLower === "exam") {
                                  return "Sınav kağıdına bakma";
                                } else if (reasonLower === "other") {
                                  return "Diğer";
                                } else if (reason.trim()) {
                                  // Öğrencinin yazdığı özel metin (request_reason kolonundan gelen)
                                  return reason;
                                } else {
                                  return "Sebep belirtilmemiş";
                                }
                              })()}
                            </p>
                            <p className="text-sm text-slate-600">
                              {apt.date ? new Date(apt.date).toLocaleDateString('tr-TR') : 'Tarih belirtilmemiş'} - {apt.time ? (typeof apt.time === 'string' ? apt.time : (typeof apt.time === 'object' && apt.time !== null ? `${String((apt.time as any).hours || 0).padStart(2, '0')}:${String((apt.time as any).minutes || 0).padStart(2, '0')}` : 'Saat belirtilmemiş')) : 'Saat belirtilmemiş'}
                            </p>
                            {(apt as any).rejectionReason && (
                              <p className="text-sm text-red-500 mt-1 italic">
                                Red Nedeni: {(apt as any).rejectionReason}
                              </p>
                            )}
                          </div>
                          <div className="flex gap-2">
                            <button
                              onClick={() =>
                                handleStatusUpdate(apt.id, "approved")
                              }
                              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm font-medium"
                            >
                              Onayla
                            </button>
                            <button
                              onClick={() =>
                                handleStatusUpdate(apt.id, "rejected")
                              }
                              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 text-sm font-medium"
                            >
                              Reddet
                            </button>
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}

              {!loading && activeTab === "myAppointments" && (
                <div className="space-y-4">
                  {approvedAppointments.length === 0 ? (
                    <p className="text-slate-500 text-sm">
                      Onayladığınız randevular burada listelenecek.
                    </p>
                  ) : (
                    approvedAppointments.map((apt) => (
                      <div
                        key={apt.id}
                        className="border border-slate-200 rounded-lg p-4 bg-white"
                      >
                        <p className="font-semibold text-slate-900">
                          {apt.course || "Ders belirtilmemiş"}
                        </p>
                        <p className="text-sm text-slate-600 mt-1">
                          {apt.reason}
                        </p>
                        <p className="text-sm text-slate-600">
                          {apt.date} - {apt.time}
                        </p>
                        {apt.note && (
                          <p className="text-sm text-slate-500 mt-1 italic">
                            Not: {apt.note}
                          </p>
                        )}
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </section>

          {/* SAĞ TARAF – DERS PROGRAMI */}
          <section className="lg:col-span-2 bg-white rounded-xl border p-4 shadow">
            <div className="flex justify-between items-center mb-3">
              <h2 className="text-sm font-semibold">
                Haftalık Ders Programı
              </h2>
              <button
                onClick={handleSaveSchedule}
                disabled={savingSchedule}
                className="px-4 py-1.5 text-xs font-medium text-white bg-[#d71920] rounded hover:bg-[#b8151a] disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {savingSchedule ? "Kaydediliyor..." : "Kaydet"}
              </button>
            </div>

            <div className="grid grid-cols-6 text-xs gap-1">
              <div />
              {days.map((d) => (
                <div key={d} className="text-center font-medium">
                  {d}
                </div>
              ))}

              {times.map((t) => (
                <React.Fragment key={t}>
                  <div className="text-right pr-2 text-[11px] whitespace-nowrap min-w-[88px]">
                    {t}
                  </div>
                  {days.map((d) => {
                    const key = `${d}-${t}`;
                    const active = selectedSlots.includes(key);

                    return (
                      <button
                        key={key}
                        onClick={() => toggleSlot(key)}
                        className={`h-6 rounded border transition
                        ${
                          active
                            ? "bg-[#d71920] border-[#d71920]"
                            : "bg-slate-100 hover:bg-slate-200"
                        }`}
                      />
                    );
                  })}
                </React.Fragment>
              ))}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
};

export default InstructorAppointmentManagement;
