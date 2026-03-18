import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getCurrentUser } from "../services/authService";
import { createOrder } from "../services/orderService";

interface MenuItem {
  id: number;
  name: string;
  price: number;
  category: string;
}

const CafeteriaOrderPage: React.FC = () => {
  useNavigate();
  const user = getCurrentUser();
  const isStudent = user?.role === "student";

  // Örnek menü verileri (backend'den gelecek)
  const [menuItems] = useState<MenuItem[]>([
    { id: 1, name: "Tavuk Döner", price: 45, category: "Ana Yemek" },
    { id: 2, name: "Et Döner", price: 50, category: "Ana Yemek" },
    { id: 3, name: "Lahmacun", price: 30, category: "Ana Yemek" },
    { id: 4, name: "Pizza", price: 55, category: "Ana Yemek" },
    { id: 5, name: "Hamburger", price: 40, category: "Ana Yemek" },
    { id: 6, name: "Çorba", price: 20, category: "Çorba" },
    { id: 7, name: "Salata", price: 25, category: "Salata" },
    { id: 8, name: "Kola", price: 10, category: "İçecek" },
    { id: 9, name: "Ayran", price: 8, category: "İçecek" },
    { id: 10, name: "Su", price: 5, category: "İçecek" },
  ]);

  const [selectedItems, setSelectedItems] = useState<{ item: MenuItem; quantity: number }[]>([]);
  const [selectedTime, setSelectedTime] = useState("");
  const [note, setNote] = useState("");

  const categories = Array.from(new Set(menuItems.map((item) => item.category)));

  const handleAddToCart = (item: MenuItem) => {
    const existingItem = selectedItems.find((si) => si.item.id === item.id);
    if (existingItem) {
      setSelectedItems(
        selectedItems.map((si) =>
          si.item.id === item.id ? { ...si, quantity: si.quantity + 1 } : si
        )
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
          si.item.id === itemId ? { ...si, quantity: si.quantity - 1 } : si
        )
      );
    } else {
      setSelectedItems(selectedItems.filter((si) => si.item.id !== itemId));
    }
  };

  const totalPrice = selectedItems.reduce(
    (sum, si) => sum + si.item.price * si.quantity,
    0
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (selectedItems.length === 0) {
      alert("Lütfen en az bir ürün seçiniz.");
      return;
    }

    if (!selectedTime) {
      alert("Lütfen sipariş saati seçiniz.");
      return;
    }

    (async () => {
      try {
        const payload = {
          orderItems: selectedItems.map((si) => ({
            menuItemId: si.item.id,
            quantity: si.quantity,
          })),
        };

        const created = await createOrder(payload);
        const orderNumber = created?.orderNumber || created?.OrderNumber;
        alert(orderNumber ? `Siparişiniz alındı! No: ${orderNumber}` : "Siparişiniz alındı!");

        // Sepeti temizle
        setSelectedItems([]);
        setSelectedTime("");
        setNote("");
      } catch (err: any) {
        const msg =
          err?.response?.data?.message ||
          err?.message ||
          "Sipariş oluşturulurken hata oluştu.";
        alert(msg);
      }
    })();
  };

  // Saat seçenekleri (ileriki saatler için)
  const getTimeOptions = () => {
    const now = new Date();
    const options = [];
    for (let i = 1; i <= 5; i++) {
      const time = new Date(now.getTime() + i * 60 * 60 * 1000); // i saat sonra
      const hours = time.getHours().toString().padStart(2, "0");
      const minutes = time.getMinutes().toString().padStart(2, "0");
      options.push(`${hours}:${minutes}`);
    }
    return options;
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Kafeterya Sipariş</h1>
          <Link
            to={isStudent ? "/ogrenci" : "/ogretim-elemani"}
            className="text-sm underline hover:opacity-90"
          >
            {isStudent ? "Öğrenci anasayfasına dön" : "Öğretim elemanı anasayfasına dön"}
          </Link>
        </div>
      </header>

      {/* İçerik */}
      <main className="flex-1 px-4 py-8">
        <div className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Menü */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-6">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">Menü</h2>

              {categories.map((category) => (
                <div key={category} className="mb-6">
                  <h3 className="text-lg font-semibold text-slate-800 mb-3 border-b pb-2">
                    {category}
                  </h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {menuItems
                      .filter((item) => item.category === category)
                      .map((item) => (
                        <div
                          key={item.id}
                          className="flex items-center justify-between p-3 border border-slate-200 rounded-lg hover:bg-slate-50"
                        >
                          <div className="flex-1">
                            <p className="font-medium text-slate-900">{item.name}</p>
                            <p className="text-sm text-slate-600">{item.price} ₺</p>
                          </div>
                          <button
                            onClick={() => handleAddToCart(item)}
                            className="ml-3 px-4 py-2 bg-[#d71920] text-white rounded-lg hover:opacity-90 text-sm font-medium"
                          >
                            Ekle
                          </button>
                        </div>
                      ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Sipariş Özeti */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-6 sticky top-4">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">Sipariş Özeti</h2>

              {selectedItems.length === 0 ? (
                <p className="text-slate-500 text-sm">Sepetiniz boş</p>
              ) : (
                <>
                  <div className="space-y-3 mb-4">
                    {selectedItems.map((si) => (
                      <div
                        key={si.item.id}
                        className="flex items-center justify-between p-2 bg-slate-50 rounded"
                      >
                        <div className="flex-1">
                          <p className="text-sm font-medium text-slate-900">
                            {si.item.name}
                          </p>
                          <p className="text-xs text-slate-600">
                            {si.item.price} ₺ x {si.quantity}
                          </p>
                        </div>
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => handleRemoveFromCart(si.item.id)}
                            className="px-2 py-1 bg-red-100 text-red-700 rounded text-xs hover:bg-red-200"
                          >
                            -
                          </button>
                          <span className="text-sm font-medium text-slate-900">
                            {si.quantity}
                          </span>
                          <button
                            onClick={() => handleAddToCart(si.item)}
                            className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs hover:bg-green-200"
                          >
                            +
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="border-t pt-4 mb-4">
                    <div className="flex justify-between items-center mb-4">
                      <span className="font-semibold text-slate-900">Toplam:</span>
                      <span className="font-bold text-lg text-[#d71920]">
                        {totalPrice} ₺
                      </span>
                    </div>
                  </div>

                  <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-1">
                        Sipariş Saati
                      </label>
                      <select
                        value={selectedTime}
                        onChange={(e) => setSelectedTime(e.target.value)}
                        className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
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
                        Not (Opsiyonel)
                      </label>
                      <textarea
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                        rows={3}
                        className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="Özel istekleriniz..."
                      />
                    </div>

                    <button
                      type="submit"
                      className="w-full bg-[#d71920] text-white py-3 rounded-lg hover:opacity-90 font-medium"
                    >
                      Sipariş Ver
                    </button>
                  </form>
                </>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default CafeteriaOrderPage;

