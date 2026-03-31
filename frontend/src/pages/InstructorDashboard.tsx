import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { logout, getCurrentUser } from "../services/authService";
import NotificationBell from "../components/NotificationBell";

const InstructorDashboard: React.FC = () => {
  const navigate = useNavigate();
  const user = getCurrentUser();

  const [showLogoutModal, setShowLogoutModal] = useState(false);
  const openLogoutModal = () => setShowLogoutModal(true);
  const closeLogoutModal = () => setShowLogoutModal(false);

  const handleLogout = () => {
    logout();
    setShowLogoutModal(false);
    navigate("/");
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">
            Başkent Yaşam – Öğretim Elemanı
          </h1>
          <div className="flex items-center gap-4">
            <span className="text-sm">
              Hoş Geldiniz, {user?.name || "Öğretim Elemanı"}
            </span>
            <NotificationBell />
            <button
              onClick={openLogoutModal}
              className="hover:underline text-sm"
            >
              Çıkış yap
            </button>
          </div>
        </div>
      </header>

      {/* Kartlar */}
      <main className="flex-1 flex items-center justify-center px-6 py-10">
        <div className="w-full max-w-6xl grid grid-cols-1 md:grid-cols-2 gap-8">
          {/* Randevu Yönetimi */}
          <button
            onClick={() => navigate("/randevu-yonetimi")}
            className="text-left bg-white rounded-2xl border p-6 shadow hover:shadow-lg transition"
          >
            <h3 className="text-lg font-semibold mb-2">Randevu Yönetimi</h3>
            <p className="text-slate-600">
              Öğrencilerden gelen randevuları yönetin.
            </p>
          </button>

          {/* Diğerleri (aynı) */}
          <button
            onClick={() => navigate("/kutuphane")}
            className="text-left bg-white rounded-2xl border p-6 shadow hover:shadow-lg transition"
          >
            <h3 className="text-lg font-semibold mb-2">Kütüphane Doluluk</h3>
            <p className="text-slate-600">
              Kütüphanenin doluluk oranını görüntüleyin.
            </p>
          </button>

          <button
            onClick={() => navigate("/kafeterya")}
            className="text-left bg-white rounded-2xl border p-6 shadow hover:shadow-lg transition"
          >
            <h3 className="text-lg font-semibold mb-2">Kafeterya Sipariş</h3>
            <p className="text-slate-600">
              Menüden yemek seçip ileriki bir saat için sipariş verin.
            </p>
          </button>

          <div className="bg-white rounded-2xl border p-6 shadow">
            <h3 className="text-lg font-semibold mb-2">Otopark Durumu</h3>
            <p className="text-slate-600">
              Otoparkın doluluk oranını görüntüleyin.
            </p>
          </div>
        </div>
      </main>

      {showLogoutModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
          onClick={closeLogoutModal}
        >
          <div
            className="bg-white rounded-lg p-6 w-full max-w-md mx-4"
            role="dialog"
            aria-modal="true"
            aria-labelledby="logout-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="logout-title" className="text-lg font-semibold mb-4">
              Çıkış Yap
            </h2>
            <p className="text-slate-600 mb-6">
              Çıkış yapmak istediğinizden emin misiniz?
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={closeLogoutModal}
                className="px-4 py-2 rounded-md border"
              >
                İptal
              </button>
              <button
                onClick={() => {
                  handleLogout();
                }}
                className="px-4 py-2 rounded-md bg-[#d71920] text-white"
              >
                Çıkış Yap
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default InstructorDashboard;
