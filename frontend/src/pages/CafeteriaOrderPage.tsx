import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getCurrentUser } from "../services/authService";
import {
  getMenuItems,
  createOrder,
  getMyOrders,
  MenuItemFromApi,
  OrderResponse,
} from "../services/cafeteriaService";

interface MenuItem {
  id: number;
  name: string;
  price: number;
  category: string;
  image: string;
  description: string;
}

type OrderStatus =
  | "Onaylanması Bekleniyor"
  | "Hazırlanıyor"
  | "Hazırlandı"
  | "Teslim Alındı"
  | "İptal Edildi";

interface OrderItem {
  menuItemId: number;
  name: string;
  quantity: number;
  price: number;
}

interface PastOrder {
  id: number;
  items: OrderItem[];
  totalPrice: number;
  selectedTime: string;
  note: string;
  status: OrderStatus;
  createdAt: string;
}

const categoryList = [
  { name: "Sandviçler", icon: "🥪" },
  { name: "Makarnalar", icon: "🍝" },
  { name: "Tostlar", icon: "🍞" },
  { name: "Salatalar", icon: "🥗" },
  { name: "Tavuk Yemekleri", icon: "🍗" },
  { name: "Et Yemekleri", icon: "🥩" },
  { name: "Soğuk İçecekler", icon: "🥤" },
  { name: "Sıcak İçecekler", icon: "☕" },
  { name: "Tatlılar", icon: "🍰" },
];

const categoryMapping: Record<string, string> = {
  "Tavuklu Sandviç": "Sandviçler",
  "Peynirli Sandviç": "Sandviçler",
  "Ton Balıklı Sandviç": "Sandviçler",
  "Penne Arabiata": "Makarnalar",
  "Kremalı Makarna": "Makarnalar",
  "Kaşarlı Tost": "Tostlar",
  "Karışık Tost": "Tostlar",
  "Akdeniz Salata": "Salatalar",
  "Tavuklu Salata": "Salatalar",
  "Izgara Tavuk": "Tavuk Yemekleri",
  "Tavuk Şinitzel": "Tavuk Yemekleri",
  Köfte: "Et Yemekleri",
  "Et Sote": "Et Yemekleri",
  Kola: "Soğuk İçecekler",
  Ayran: "Soğuk İçecekler",
  Su: "Soğuk İçecekler",
  Çay: "Sıcak İçecekler",
  Kahve: "Sıcak İçecekler",
  Cheesecake: "Tatlılar",
  Sufle: "Tatlılar",
  Hamburger: "Et Yemekleri",
  Tost: "Tostlar",
};

const categoryIcons: Record<string, string> = {
  Sandviçler: "🥪",
  Makarnalar: "🍝",
  Tostlar: "🍞",
  Salatalar: "🥗",
  "Tavuk Yemekleri": "🍗",
  "Et Yemekleri": "🥩",
  "Soğuk İçecekler": "🥤",
  "Sıcak İçecekler": "☕",
  Tatlılar: "🍰",
};

const statusStyles: Record<string, string> = {
  "Onaylanması Bekleniyor":
    "bg-yellow-100 text-yellow-800 border border-yellow-200",
  Hazırlanıyor: "bg-orange-100 text-orange-800 border border-orange-200",
  Hazırlandı: "bg-blue-100 text-blue-800 border border-blue-200",
  "Teslim Alındı": "bg-green-100 text-green-800 border border-green-200",
  "İptal Edildi": "bg-red-100 text-red-800 border border-red-200",
};

const CART_STORAGE_KEY = "cafeteria_cart";

interface CartItem {
  itemId: number;
  quantity: number;
}

function loadCartFromStorage(): CartItem[] {
  try {
    const data = localStorage.getItem(CART_STORAGE_KEY);
    if (data) return JSON.parse(data);
  } catch { /* ignore */ }
  return [];
}

function saveCartToStorage(items: { item: MenuItem; quantity: number }[]) {
  const data: CartItem[] = items.map((si) => ({
    itemId: si.item.id,
    quantity: si.quantity,
  }));
  localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(data));
}

function mapApiToMenuItem(apiItem: MenuItemFromApi): MenuItem {
  const category = categoryMapping[apiItem.name] || "Et Yemekleri";
  const image = categoryIcons[category] || "🍽️";
  return {
    id: apiItem.id,
    name: apiItem.name,
    price: apiItem.price,
    category,
    image,
    description: apiItem.description || "",
  };
}

function mapOrderResponse(order: OrderResponse): PastOrder {
  return {
    id: order.id,
    items: order.items.map((item) => ({
      menuItemId: item.menuItemId,
      name: item.name,
      quantity: item.quantity,
      price: item.price,
    })),
    totalPrice: order.totalPrice,
    selectedTime: order.pickupTime,
    note: order.note || "",
    status: order.status as OrderStatus,
    createdAt: order.createdAt,
  };
}

const CafeteriaOrderPage: React.FC = () => {
  const user = getCurrentUser();
  const isStudent = user?.role === "student";

  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [menuLoading, setMenuLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] =
    useState<string>("Sandviçler");
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedItems, setSelectedItems] = useState<
    { item: MenuItem; quantity: number }[]
  >([]);
  const [selectedTime, setSelectedTime] = useState("");
  const [note, setNote] = useState("");
  const [orders, setOrders] = useState<PastOrder[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [cartLoaded, setCartLoaded] = useState(false);

  useEffect(() => {
    const fetchMenu = async () => {
      try {
        const data = await getMenuItems();
        setMenuItems(data.map(mapApiToMenuItem));
      } catch (err) {
        console.error("Menü yüklenemedi:", err);
      } finally {
        setMenuLoading(false);
      }
    };
    fetchMenu();
  }, []);

  useEffect(() => {
    if (menuItems.length === 0) return;
    const stored = loadCartFromStorage();
    if (stored.length > 0) {
      const restored = stored
        .map((ci) => {
          const item = menuItems.find((m) => m.id === ci.itemId);
          return item ? { item, quantity: ci.quantity } : null;
        })
        .filter(Boolean) as { item: MenuItem; quantity: number }[];
      if (restored.length > 0) setSelectedItems(restored);
    }
    setCartLoaded(true);
  }, [menuItems]);

  const fetchOrders = useCallback(async () => {
    try {
      const data = await getMyOrders();
      setOrders(data.map(mapOrderResponse));
    } catch (err) {
      console.error("Siparişler yüklenemedi:", err);
    } finally {
      setOrdersLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchOrders();
    const interval = setInterval(fetchOrders, 15000);
    return () => clearInterval(interval);
  }, [fetchOrders]);

  useEffect(() => {
    if (!cartLoaded) return;
    saveCartToStorage(selectedItems);
  }, [selectedItems, cartLoaded]);

  const filteredItems = useMemo(() => {
    const trimmedSearch = searchTerm.trim().toLowerCase();

    if (trimmedSearch) {
      return menuItems.filter(
        (item) =>
          item.name.toLowerCase().includes(trimmedSearch) ||
          item.description.toLowerCase().includes(trimmedSearch) ||
          item.category.toLowerCase().includes(trimmedSearch),
      );
    }

    return menuItems.filter((item) => item.category === selectedCategory);
  }, [menuItems, selectedCategory, searchTerm]);

  const activeOrders = orders.filter(
    (order) =>
      order.status !== "Teslim Alındı" && order.status !== "İptal Edildi",
  );
  const pastOrders = orders.filter(
    (order) =>
      order.status === "Teslim Alındı" || order.status === "İptal Edildi",
  );

  const handleAddToCart = (item: MenuItem) => {
    const existingItem = selectedItems.find((si) => si.item.id === item.id);

    if (existingItem) {
      setSelectedItems(
        selectedItems.map((si) =>
          si.item.id === item.id ? { ...si, quantity: si.quantity + 1 } : si,
        ),
      );
    } else {
      setSelectedItems([...selectedItems, { item, quantity: 1 }]);
    }
  };

  const handleRemoveFromCart = (itemId: number) => {
    const existingItem = selectedItems.find((si) => si.item.id === itemId);

    if (existingItem && existingItem.quantity > 1) {
      setSelectedItems(
        selectedItems.map((si) =>
          si.item.id === itemId ? { ...si, quantity: si.quantity - 1 } : si,
        ),
      );
    } else {
      setSelectedItems(selectedItems.filter((si) => si.item.id !== itemId));
    }
  };

  const totalPrice = selectedItems.reduce(
    (sum, si) => sum + si.item.price * si.quantity,
    0,
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (selectedItems.length === 0) {
      alert("Lütfen en az bir ürün seçiniz.");
      return;
    }

    if (!selectedTime) {
      alert("Lütfen sipariş saati seçiniz.");
      return;
    }

    setSubmitting(true);
    try {
      await createOrder({
        orderItems: selectedItems.map((si) => ({
          menuItemId: si.item.id,
          quantity: si.quantity,
        })),
        pickupTime: selectedTime,
        note: note || undefined,
      });

      alert("Siparişiniz alındı!");
      setSelectedItems([]);
      setSelectedTime("");
      setNote("");
      localStorage.removeItem(CART_STORAGE_KEY);
      await fetchOrders();
    } catch (err: any) {
      const msg =
        err.response?.data?.message || "Sipariş oluşturulurken bir hata oluştu.";
      alert(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const getTimeOptions = () => {
    const now = new Date();
    const options = [];
    for (let i = 1; i <= 5; i++) {
      const time = new Date(now.getTime() + i * 60 * 60 * 1000);
      const hours = time.getHours().toString().padStart(2, "0");
      const minutes = time.getMinutes().toString().padStart(2, "0");
      options.push(`${hours}:${minutes}`);
    }
    return options;
  };

  return (
    <div className="min-h-screen bg-[#f8f8f8] flex flex-col">
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-7xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Kafeterya Sipariş</h1>
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
        <div className="max-w-7xl mx-auto grid grid-cols-1 xl:grid-cols-12 gap-6">
          <aside className="xl:col-span-3">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-5">
              <h2 className="text-lg font-semibold text-slate-900 mb-4">
                Menü Kategorileri
              </h2>

              <div className="mb-4">
                <input
                  type="text"
                  placeholder="Ara..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full rounded-xl border border-slate-300 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                />
              </div>

              <div className="space-y-3">
                {categoryList.map((category) => (
                  <button
                    key={category.name}
                    onClick={() => setSelectedCategory(category.name)}
                    className={`w-full flex items-center gap-3 rounded-xl border px-4 py-3 text-left transition ${
                      selectedCategory === category.name && !searchTerm.trim()
                        ? "border-[#d71920] bg-red-50 text-[#d71920]"
                        : "border-slate-200 bg-white text-slate-800 hover:bg-slate-50"
                    }`}
                  >
                    <span className="text-xl">{category.icon}</span>
                    <span className="font-medium leading-tight">
                      {category.name}
                    </span>
                  </button>
                ))}
              </div>
            </div>
          </aside>

          <section className="xl:col-span-5">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-5">
              <div className="flex items-center justify-between mb-5">
                <h2 className="text-xl font-semibold text-slate-900">
                  {searchTerm.trim() ? "Arama Sonuçları" : selectedCategory}
                </h2>
                <span className="text-sm text-slate-500">
                  {filteredItems.length} ürün
                </span>
              </div>

              {menuLoading ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-8 text-center text-slate-500">
                  Menü yükleniyor...
                </div>
              ) : filteredItems.length === 0 ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-8 text-center text-slate-500">
                  Ürün bulunamadı.
                </div>
              ) : (
                <div className="space-y-4">
                  {filteredItems.map((item) => (
                    <div
                      key={item.id}
                      className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm"
                    >
                      <div className="flex h-24 w-24 items-center justify-center rounded-xl bg-red-50 text-5xl">
                        {item.image}
                      </div>

                      <div className="flex-1">
                        <h3 className="text-2xl font-semibold text-slate-900">
                          {item.name}
                        </h3>

                        <p className="mt-2 text-sm leading-6 text-slate-600">
                          {item.description}
                        </p>

                        <p className="mt-3 text-2xl font-bold text-[#d71920]">
                          {item.price.toFixed(2)} TL
                        </p>

                        <div className="mt-4">
                          <button
                            onClick={() => handleAddToCart(item)}
                            className="inline-flex items-center gap-2 rounded-xl bg-[#d71920] px-5 py-2.5 text-sm font-semibold text-white hover:opacity-90"
                          >
                            Sepete Ekle
                            <span className="flex h-6 w-6 items-center justify-center rounded-md bg-white text-[#d71920] font-bold">
                              +
                            </span>
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>

          <aside className="xl:col-span-4 space-y-6">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-5">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">
                Sepetim
              </h2>

              {selectedItems.length === 0 ? (
                <p className="text-slate-500 text-sm">Sepetiniz boş</p>
              ) : (
                <>
                  <div className="space-y-3 mb-5">
                    {selectedItems.map((si) => (
                      <div
                        key={si.item.id}
                        className="rounded-xl bg-slate-50 border border-slate-200 p-3"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="font-medium text-slate-900">
                              {si.item.name}
                            </p>
                            <p className="text-sm text-slate-600">
                              {si.item.price} TL x {si.quantity}
                            </p>
                          </div>

                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => handleRemoveFromCart(si.item.id)}
                              className="h-8 w-8 rounded-lg bg-red-100 text-[#d71920] font-bold hover:bg-red-200"
                            >
                              -
                            </button>
                            <span className="min-w-[20px] text-center font-medium">
                              {si.quantity}
                            </span>
                            <button
                              onClick={() => handleAddToCart(si.item)}
                              className="h-8 w-8 rounded-lg bg-red-100 text-[#d71920] font-bold hover:bg-red-200"
                            >
                              +
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>

                  <form
                    onSubmit={handleSubmit}
                    className="space-y-4 border-t pt-4"
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-slate-900">
                        Toplam
                      </span>
                      <span className="text-2xl font-bold text-[#d71920]">
                        {totalPrice.toFixed(2)} TL
                      </span>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">
                        Sipariş Saati
                      </label>
                      <select
                        value={selectedTime}
                        onChange={(e) => setSelectedTime(e.target.value)}
                        className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                        required
                      >
                        <option value="">Saat seçiniz</option>
                        {getTimeOptions().map((time) => (
                          <option key={time} value={time}>
                            {time}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">
                        Not
                      </label>
                      <textarea
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                        rows={3}
                        className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                        placeholder="Özel istekleriniz..."
                      />
                    </div>

                    <button
                      type="submit"
                      disabled={submitting}
                      className="w-full rounded-xl bg-[#d71920] py-3 text-white font-semibold hover:opacity-90 disabled:opacity-50"
                    >
                      {submitting ? "Sipariş gönderiliyor..." : "Siparişi Tamamla"}
                    </button>
                  </form>
                </>
              )}
            </div>

            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-5">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">
                Aktif Siparişlerim
              </h2>

              {ordersLoading ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-slate-500">
                  Yükleniyor...
                </div>
              ) : activeOrders.length === 0 ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-slate-500">
                  Aktif sipariş bulunmuyor.
                </div>
              ) : (
                <div className="space-y-4">
                  {activeOrders.map((order) => (
                    <div
                      key={order.id}
                      className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
                    >
                      <div className="flex items-start justify-between gap-3 mb-3">
                        <div>
                          <p className="text-sm text-slate-500">
                            Sipariş #{order.id}
                          </p>
                          <p className="text-sm text-slate-500">
                            {order.createdAt} • Teslim: {order.selectedTime}
                          </p>
                        </div>

                        <span
                          className={`rounded-full px-3 py-1 text-xs font-semibold ${
                            statusStyles[order.status] || "bg-gray-100 text-gray-800"
                          }`}
                        >
                          {order.status}
                        </span>
                      </div>

                      <div className="space-y-1 mb-3">
                        {order.items.map((item, index) => (
                          <p
                            key={`${order.id}-${item.menuItemId}-${index}`}
                            className="text-sm text-slate-700"
                          >
                            <span className="font-medium">{item.name}</span> x{" "}
                            {item.quantity}
                          </p>
                        ))}
                      </div>

                      {order.note && (
                        <p className="mb-3 text-sm text-slate-600">
                          <span className="font-medium">Not:</span> {order.note}
                        </p>
                      )}

                      <span className="font-semibold text-slate-900">
                        {order.totalPrice.toFixed(2)} TL
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-5">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">
                Geçmiş Siparişlerim
              </h2>

              {ordersLoading ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-slate-500">
                  Yükleniyor...
                </div>
              ) : pastOrders.length === 0 ? (
                <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-slate-500">
                  Geçmiş sipariş bulunmuyor.
                </div>
              ) : (
                <div className="space-y-4">
                  {pastOrders.map((order) => (
                    <div
                      key={order.id}
                      className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
                    >
                      <div className="flex items-start justify-between gap-3 mb-3">
                        <div>
                          <p className="text-sm text-slate-500">
                            Sipariş #{order.id}
                          </p>
                          <p className="text-sm text-slate-500">
                            {order.createdAt} • Teslim: {order.selectedTime}
                          </p>
                        </div>

                        <span
                          className={`rounded-full px-3 py-1 text-xs font-semibold ${
                            statusStyles[order.status] || "bg-gray-100 text-gray-800"
                          }`}
                        >
                          {order.status}
                        </span>
                      </div>

                      <div className="space-y-1 mb-3">
                        {order.items.map((item, index) => (
                          <p
                            key={`${order.id}-${item.menuItemId}-${index}`}
                            className="text-sm text-slate-700"
                          >
                            <span className="font-medium">{item.name}</span> x{" "}
                            {item.quantity}
                          </p>
                        ))}
                      </div>

                      {order.note && (
                        <p className="mb-3 text-sm text-slate-600">
                          <span className="font-medium">Not:</span> {order.note}
                        </p>
                      )}

                      <span className="font-semibold text-slate-900">
                        {order.totalPrice.toFixed(2)} TL
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </aside>
        </div>
      </main>
    </div>
  );
};

export default CafeteriaOrderPage;
