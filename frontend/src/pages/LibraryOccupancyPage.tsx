import React, { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getCurrentUser } from "../services/authService";

const LibraryOccupancyPage: React.FC = () => {
  const user = getCurrentUser();
  const isStudent = user?.role === "student";

  const [totalCapacity] = useState(1800);
  const [currentCount] = useState(600);
  const [lastUpdated] = useState("Bugün, 14:32:10");

  const occupancyRate = useMemo(() => {
    return Math.round((currentCount / totalCapacity) * 100);
  }, [currentCount, totalCapacity]);

  const getBarColor = (rate: number) => {
    if (rate <= 40) return "bg-green-500";
    if (rate <= 70) return "bg-orange-400";
    return "bg-red-500";
  };

  const getTextColor = (rate: number) => {
    if (rate <= 40) return "text-green-600";
    if (rate <= 70) return "text-orange-500";
    return "text-red-600";
  };

  const getStatusText = (rate: number) => {
    if (rate <= 40) return "Kütüphane şu anda uygun yoğunlukta.";
    if (rate <= 70) return "Kütüphane orta yoğunlukta.";
    return "Kütüphane şu anda oldukça yoğun.";
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-7xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Kütüphane Doluluk Oranı</h1>
          <Link
            to={isStudent ? "/ogrenci" : "/ogretim-elemani"}
            className="text-sm underline hover:opacity-90"
          >
            {isStudent
              ? "Öğrenci anasayfasına dön"
              : "Öğretim elemanı anasayfasına dön"}
          </Link>
        </div>
      </header>

      <main className="flex-1 px-4 py-8">
        <div className="max-w-5xl mx-auto">
          <div className="bg-white rounded-3xl shadow-md border border-slate-200 p-8 md:p-10">
            <div className="mb-8">
              <h2 className="text-3xl font-bold text-slate-900 mb-2">
                Güncel Kütüphane Yoğunluğu
              </h2>
              <p className="text-slate-600 text-base">
                Kütüphanenin anlık doluluk durumu aşağıda gösterilmektedir.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
                <p className="text-sm text-slate-500 mb-2">
                  Kütüphane Kapasitesi
                </p>
                <p className="text-3xl font-bold text-slate-900">
                  {totalCapacity}
                </p>
                <p className="text-sm text-slate-600 mt-1">kişi</p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
                <p className="text-sm text-slate-500 mb-2">
                  İçerideki Kişi Sayısı
                </p>
                <p className="text-3xl font-bold text-slate-900">
                  {currentCount}
                </p>
                <p className="text-sm text-slate-600 mt-1">kişi</p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
                <p className="text-sm text-slate-500 mb-2">Doluluk Oranı</p>
                <p
                  className={`text-3xl font-bold ${getTextColor(occupancyRate)}`}
                >
                  %{occupancyRate}
                </p>
                <p className="text-sm text-slate-600 mt-1">
                  {getStatusText(occupancyRate)}
                </p>
              </div>
            </div>

            <div className="mb-6">
              <div className="flex items-center justify-between mb-3">
                <span className="text-sm font-medium text-slate-700">
                  Anlık doluluk seviyesi
                </span>
                <span
                  className={`text-sm font-semibold ${getTextColor(occupancyRate)}`}
                >
                  %{occupancyRate}
                </span>
              </div>

              <div className="w-full h-6 rounded-full bg-slate-200 overflow-hidden">
                <div
                  className={`h-full transition-all duration-500 ${getBarColor(occupancyRate)}`}
                  style={{ width: `${occupancyRate}%` }}
                />
              </div>
            </div>

            <div className="rounded-2xl bg-red-50 border border-red-100 p-5">
              <p className="text-base text-slate-800 leading-7">
                Kütüphane kapasitesi{" "}
                <span className="font-semibold">{totalCapacity} kişi</span>, şu
                anda içeride{" "}
                <span className="font-semibold">{currentCount} kişi</span>{" "}
                bulunuyor. Buna göre kütüphane şu anda{" "}
                <span className={`font-bold ${getTextColor(occupancyRate)}`}>
                  %{occupancyRate}
                </span>{" "}
                oranında dolu.
              </p>
            </div>

            <div className="mt-6 flex flex-col md:flex-row md:items-center md:justify-between gap-3 text-sm text-slate-600">
              <p>
                <span className="font-medium text-slate-800">
                  Son güncelleme:
                </span>{" "}
                {lastUpdated}
              </p>
              <p>
                Veriler 10 saniyede bir güncellenecek şekilde planlanmıştır.
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default LibraryOccupancyPage;
