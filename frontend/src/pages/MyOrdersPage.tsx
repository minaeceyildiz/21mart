import React, { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getCurrentUser } from "../services/authService";
import { getMyOrders, getOrderStatusText, OrderResponseDto } from "../services/orderService";

const MyOrdersPage: React.FC = () => {
  const user = getCurrentUser();
  const isStudent = user?.role === "student";

  const [orders, setOrders] = useState<OrderResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");

  const homePath = useMemo(() => {
    if (user?.role === "cashier") return "/kasiyer/siparisler";
    return isStudent ? "/ogrenci" : "/ogretim-elemani";
  }, [isStudent, user?.role]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        setLoading(true);
        const data = await getMyOrders();
        if (mounted) setOrders(data);
      } catch (e: any) {
        if (mounted) setError(e?.message || "Siparişler getirilirken hata oluştu.");
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Siparişlerim</h1>
          <Link to={homePath} className="text-sm underline hover:opacity-90">
            Geri dön
          </Link>
        </div>
      </header>

      <main className="flex-1 px-4 py-8">
        <div className="max-w-6xl mx-auto">
          {loading && <p className="text-slate-600">Yükleniyor...</p>}
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {!loading && !error && orders.length === 0 && (
            <div className="bg-white rounded-xl border p-6">
              <p className="text-slate-600">Henüz bir siparişiniz yok.</p>
            </div>
          )}

          <div className="space-y-4">
            {orders.map((o) => (
              <div key={o.id} className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
                  <div>
                    <p className="text-sm text-slate-500">Sipariş No</p>
                    <p className="text-lg font-semibold text-slate-900">{o.orderNumber}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="px-3 py-1 rounded-full text-sm bg-slate-100 text-slate-800">
                      {getOrderStatusText(o.status)}
                    </span>
                    <span
                      className={
                        "px-3 py-1 rounded-full text-sm " +
                        (o.isPaid ? "bg-green-100 text-green-800" : "bg-amber-100 text-amber-800")
                      }
                    >
                      {o.isPaid ? "Ödendi" : "Ödenmedi"}
                    </span>
                  </div>
                </div>

                <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="md:col-span-2">
                    <p className="text-sm font-medium text-slate-700 mb-2">Ürünler</p>
                    <div className="space-y-2">
                      {o.orderItems.map((it) => (
                        <div key={it.id} className="flex items-center justify-between bg-slate-50 rounded-lg px-3 py-2">
                          <div className="text-sm text-slate-900">
                            {it.menuItemName} <span className="text-slate-500">x {it.quantity}</span>
                          </div>
                          <div className="text-sm font-medium text-slate-900">
                            {(it.price * it.quantity).toFixed(2)} ₺
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div>
                    <p className="text-sm font-medium text-slate-700 mb-2">Toplam</p>
                    <div className="bg-slate-50 rounded-lg p-4">
                      <p className="text-2xl font-bold text-[#d71920]">{o.totalAmount.toFixed(2)} ₺</p>
                      <p className="text-xs text-slate-500 mt-1">
                        Oluşturma: {new Date(o.createdAt).toLocaleString("tr-TR")}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
};

export default MyOrdersPage;

