import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  cashierApprove,
  cashierCancel,
  cashierNotPaid,
  cashierPaid,
  cashierPreparing,
  cashierReady,
  getCashierOrders,
  getOrderStatusText,
  OrderResponseDto,
  OrderStatus,
} from "../services/orderService";
import { getCurrentUser } from "../services/authService";

const CashierOrdersPage: React.FC = () => {
  const user = getCurrentUser();

  const [orders, setOrders] = useState<OrderResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");

  const [statusFilter, setStatusFilter] = useState<OrderStatus | "ALL">("ALL");
  const [paidFilter, setPaidFilter] = useState<"ALL" | "PAID" | "UNPAID">("UNPAID");
  const [searchDraft, setSearchDraft] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const isHistoryMode = debouncedSearch.trim().length >= 2;

  const queryParams = useMemo(() => {
    const params: Record<string, unknown> = {};
    if (statusFilter !== "ALL") params.status = statusFilter;
    if (paidFilter === "PAID") params.isPaid = true;
    if (paidFilter === "UNPAID") params.isPaid = false;
    return params;
  }, [paidFilter, statusFilter]);

  useEffect(() => {
    const t = window.setTimeout(() => setDebouncedSearch(searchDraft.trim()), 400);
    return () => window.clearTimeout(t);
  }, [searchDraft]);

  const refresh = useCallback(async () => {
    try {
      setLoading(true);
      const term = debouncedSearch.trim();
      const params: { status?: OrderStatus; isPaid?: boolean; userSearch?: string } = {};
      if (term.length >= 2) {
        params.userSearch = term;
      } else {
        Object.assign(params, queryParams);
      }
      const data = await getCashierOrders(params);
      setOrders(data);
    } catch (e: any) {
      setError(e?.message || "Siparişler getirilirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, queryParams]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const act = async (fn: (id: number) => Promise<any>, id: number) => {
    try {
      setError("");
      await fn(id);
      await refresh();
    } catch (e: any) {
      setError(e?.response?.data?.message || e?.message || "İşlem başarısız.");
    }
  };

  const canApprove = (s: OrderStatus) => s === "Received";
  const canPreparing = (s: OrderStatus) => s === "Received" || s === "Approved";
  const canReady = (s: OrderStatus) => s === "Preparing";
  const canPaid = (s: OrderStatus) => s === "Ready";
  const canNotPaid = (s: OrderStatus) => s === "Ready";
  const canCancel = (s: OrderStatus) => s !== "Cancelled" && s !== "Paid" && s !== "NotPaid";

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <div>
            <h1 className="text-2xl font-semibold">Kasiyer – Sipariş Yönetimi</h1>
            <p className="text-sm opacity-90">Hoş geldiniz, {user?.name || "Kasiyer"}</p>
          </div>
          <Link to="/" className="text-sm underline hover:opacity-90">
            Çıkış / Giriş sayfası
          </Link>
        </div>
      </header>

      <main className="flex-1 px-4 py-8">
        <div className="max-w-6xl mx-auto">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-4 mb-4 space-y-4">
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">
                Kullanıcı ara (ad, e-posta veya öğrenci no)
              </label>
              <div className="flex gap-2">
                <input
                  type="search"
                  className="flex-1 border border-slate-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-[#d71920]/30 focus:border-[#d71920] outline-none"
                  placeholder="Örn: ahmet veya @edu.tr veya öğrenci numarası…"
                  value={searchDraft}
                  onChange={(e) => setSearchDraft(e.target.value)}
                  autoComplete="off"
                />
                {searchDraft.length > 0 && (
                  <button
                    type="button"
                    className="px-3 py-2 text-sm rounded-lg border border-slate-300 text-slate-700 hover:bg-slate-50"
                    onClick={() => {
                      setSearchDraft("");
                      setDebouncedSearch("");
                    }}
                  >
                    Temizle
                  </button>
                )}
              </div>
              <p className="text-xs text-slate-500 mt-1">
                {searchDraft.trim().length > 0 && searchDraft.trim().length < 2
                  ? "Geçmiş siparişler için en az 2 karakter yazın."
                  : isHistoryMode
                    ? "Geçmiş modu: aşağıda eşleşen kullanıcı(lar)a ait tüm siparişler (iptal ve ödendi dahil) listelenir. Filtreler bu modda devre dışı."
                    : "Arama boşken: bekleyen kasiyer kuyruğu ve seçili durum/ödeme filtreleri uygulanır."}
              </p>
            </div>

            {isHistoryMode && (
              <div className="rounded-xl bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-blue-900">
                <span className="font-semibold">Kullanıcı geçmişi:</span> “{debouncedSearch.trim()}” —{" "}
                {loading ? (
                  <span className="text-blue-700">aranıyor…</span>
                ) : (
                  <>
                    <span className="font-mono">{orders.length}</span> sipariş listeleniyor (iptal / ödendi / ödenmedi
                    dahil).
                  </>
                )}
              </div>
            )}

            <div className="flex flex-col md:flex-row gap-3 md:items-center md:justify-between">
            <div className="flex gap-3 flex-col sm:flex-row">
              <div>
                <label className="block text-xs font-medium text-slate-600 mb-1">Status</label>
                <select
                  className="border rounded-lg px-3 py-2 text-sm disabled:bg-slate-100 disabled:text-slate-500"
                  value={statusFilter}
                  disabled={isHistoryMode}
                  onChange={(e) => setStatusFilter(e.target.value as any)}
                >
                  <option value="ALL">Hepsi</option>
                  <option value="Received">RECEIVED</option>
                  <option value="Approved">APPROVED</option>
                  <option value="Preparing">PREPARING</option>
                  <option value="Ready">READY</option>
                  <option value="Paid">PAID</option>
                  <option value="NotPaid">NOT PAID</option>
                  <option value="Cancelled">CANCELLED</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-600 mb-1">Ödeme</label>
                <select
                  className="border rounded-lg px-3 py-2 text-sm disabled:bg-slate-100 disabled:text-slate-500"
                  value={paidFilter}
                  disabled={isHistoryMode}
                  onChange={(e) => setPaidFilter(e.target.value as any)}
                >
                  <option value="ALL">Hepsi</option>
                  <option value="UNPAID">Ödenmedi</option>
                  <option value="PAID">Ödendi</option>
                </select>
              </div>
            </div>

            <button
              onClick={() => void refresh()}
              className="px-4 py-2 rounded-lg bg-slate-900 text-white text-sm hover:opacity-90"
              disabled={loading}
            >
              Yenile
            </button>
            </div>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {loading && <p className="text-slate-600">Yükleniyor...</p>}

          {!loading && orders.length === 0 && (
            <div className="bg-white rounded-xl border p-6">
              <p className="text-slate-600">
                {isHistoryMode
                  ? "Bu aramayla eşleşen kullanıcı bulunamadı veya bu kullanıcıya ait sipariş yok."
                  : "Filtreye uygun sipariş yok."}
              </p>
            </div>
          )}

          <div className="space-y-4">
            {orders.map((o) => (
              <div key={o.id} className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4">
                  <div className="flex-1">
                    {(o.customerName || o.customerEmail || o.studentNo) && (
                      <div
                        className={
                          "mb-3 rounded-lg border px-3 py-2 text-sm " +
                          (isHistoryMode
                            ? "bg-slate-100 border-slate-300"
                            : "bg-slate-50 border-slate-200")
                        }
                      >
                        <p className="text-xs font-medium text-slate-500 mb-1">Müşteri</p>
                        <p className="font-semibold text-slate-900">{o.customerName || "—"}</p>
                        {o.customerEmail && (
                          <p className="text-slate-600 text-xs mt-0.5">{o.customerEmail}</p>
                        )}
                        {o.studentNo && (
                          <p className="text-slate-600 text-xs">Öğrenci no: {o.studentNo}</p>
                        )}
                      </div>
                    )}

                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm text-slate-500">Sipariş No</p>
                        <p className="text-lg font-semibold text-slate-900">{o.orderNumber}</p>
                      </div>
                      <div className="flex items-center gap-2 flex-wrap justify-end">
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

                    {(o.pickupTime || o.note) && (
                      <div className="mt-3 flex flex-wrap gap-3">
                        {o.pickupTime && (
                          <div className="flex items-center gap-1.5 bg-blue-50 border border-blue-200 rounded-lg px-3 py-2">
                            <span className="text-blue-600 text-sm font-medium">Teslim Saati:</span>
                            <span className="text-sm text-blue-800 font-semibold">{o.pickupTime}</span>
                          </div>
                        )}
                        {o.note && (
                          <div className="flex items-center gap-1.5 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
                            <span className="text-amber-600 text-sm font-medium">Not:</span>
                            <span className="text-sm text-amber-800">{o.note}</span>
                          </div>
                        )}
                      </div>
                    )}

                    <div className="mt-3">
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
                  </div>

                  <div className="w-full lg:w-[320px]">
                    <div className="bg-slate-50 rounded-xl p-4 border">
                      <p className="text-sm font-medium text-slate-700">Toplam</p>
                      <p className="text-2xl font-bold text-[#d71920]">{o.totalAmount.toFixed(2)} ₺</p>
                      <p className="text-xs text-slate-500 mt-1">
                        Oluşturma: {new Date(o.createdAt).toLocaleString("tr-TR")}
                      </p>
                      <div className="mt-4 grid grid-cols-2 gap-2">
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-blue-600 text-white disabled:opacity-50"
                          onClick={() => act(cashierApprove, o.id)}
                          disabled={!canApprove(o.status)}
                        >
                          Onayla
                        </button>
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-indigo-600 text-white disabled:opacity-50"
                          onClick={() => act(cashierPreparing, o.id)}
                          disabled={!canPreparing(o.status)}
                        >
                          Hazırlanıyor
                        </button>
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-amber-600 text-white disabled:opacity-50"
                          onClick={() => act(cashierReady, o.id)}
                          disabled={!canReady(o.status)}
                        >
                          Hazır
                        </button>
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-green-700 text-white disabled:opacity-50"
                          onClick={() => act(cashierPaid, o.id)}
                          disabled={!canPaid(o.status)}
                        >
                          Ödendi
                        </button>
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-orange-600 text-white disabled:opacity-50"
                          onClick={() => act(cashierNotPaid, o.id)}
                          disabled={!canNotPaid(o.status)}
                        >
                          Ödenmedi
                        </button>
                        <button
                          className="px-3 py-2 rounded-lg text-sm bg-red-700 text-white disabled:opacity-50"
                          onClick={() => {
                            if (window.confirm("Siparişi iptal etmek istediğinize emin misiniz?"))
                              act(cashierCancel, o.id);
                          }}
                          disabled={!canCancel(o.status)}
                        >
                          İptal Et
                        </button>
                      </div>
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

export default CashierOrdersPage;

