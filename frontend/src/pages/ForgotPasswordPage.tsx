import React, { useState } from "react";
import { Link } from "react-router-dom";
import { requestForgotPassword, ApiError } from "../services/authService";

const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setMessage(null);
    setLoading(true);
    try {
      const msg = await requestForgotPassword(email.trim());
      setMessage(msg);
    } catch (err) {
      const api = err as ApiError;
      setError(api.message || "İstek gönderilemedi.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100 px-4">
      <div className="w-full max-w-sm bg-white p-6 rounded-xl shadow flex flex-col gap-4">
        <h1 className="text-xl font-semibold text-center text-slate-800">Şifremi unuttum</h1>
        <p className="text-sm text-slate-600 text-center">
          Kayıtlı e-posta adresinizi girin. Hesabınız varsa size kısa süreli bir sıfırlama bağlantısı göndeririz.
        </p>

        {message && (
          <div className="bg-green-50 border border-green-200 text-green-800 text-sm px-4 py-3 rounded-lg">
            {message}
          </div>
        )}
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-800 text-sm px-4 py-3 rounded-lg">{error}</div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <input
            type="email"
            required
            autoComplete="email"
            placeholder="E-posta adresiniz"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="border rounded-lg px-4 py-2"
          />
          <button
            type="submit"
            disabled={loading}
            className="bg-[#d71920] text-white py-2 rounded-lg hover:opacity-90 disabled:opacity-50"
          >
            {loading ? "Gönderiliyor…" : "Sıfırlama bağlantısı gönder"}
          </button>
        </form>

        <p className="text-center text-sm text-slate-500">
          <Link to="/" className="text-[#d71920] underline">
            Giriş sayfasına dön
          </Link>
        </p>
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
