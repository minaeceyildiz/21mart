import React, { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { resetPasswordWithToken, ApiError } from "../services/authService";

const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const tokenFromUrl = searchParams.get("token")?.trim() || "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const missingTokenMessage =
    "Geçersiz bağlantı. E-postadaki şifre sıfırlama linkini kullanın veya yeni talep oluşturun.";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!tokenFromUrl) return;
    if (password.length < 6) {
      setError("Şifre en az 6 karakter olmalıdır.");
      return;
    }
    if (password !== confirm) {
      setError("Şifreler eşleşmiyor.");
      return;
    }
    setLoading(true);
    try {
      await resetPasswordWithToken(tokenFromUrl, password);
      setSuccess(true);
      window.setTimeout(() => {
        navigate("/", { replace: true });
      }, 2500);
    } catch (err) {
      const api = err as ApiError;
      setError(api.message || "Şifre güncellenemedi.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100 px-4">
      <div className="w-full max-w-sm bg-white p-6 rounded-xl shadow flex flex-col gap-4">
        <h1 className="text-xl font-semibold text-center text-slate-800">Yeni şifre belirle</h1>

        {success && (
          <div className="bg-green-50 border border-green-200 text-green-800 text-sm px-4 py-3 rounded-lg text-center">
            <p className="font-semibold">Şifreniz başarıyla güncellendi.</p>
            <p className="mt-2">Giriş sayfasına yönlendiriliyorsunuz…</p>
          </div>
        )}

        {!success && !tokenFromUrl && (
          <div className="bg-red-50 border border-red-200 text-red-800 text-sm px-4 py-3 rounded-lg">
            {missingTokenMessage}
          </div>
        )}

        {!success && tokenFromUrl && (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-800 text-sm px-4 py-3 rounded-lg">{error}</div>
            )}
            <input
              type="password"
              required
              minLength={6}
              autoComplete="new-password"
              placeholder="Yeni şifre (en az 6 karakter)"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="border rounded-lg px-4 py-2"
            />
            <input
              type="password"
              required
              minLength={6}
              autoComplete="new-password"
              placeholder="Yeni şifre (tekrar)"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              className="border rounded-lg px-4 py-2"
            />
            <button
              type="submit"
              disabled={loading}
              className="bg-[#d71920] text-white py-2 rounded-lg hover:opacity-90 disabled:opacity-50"
            >
              {loading ? "Kaydediliyor…" : "Şifreyi güncelle"}
            </button>
          </form>
        )}

        <p className="text-center text-sm text-slate-500">
          <Link to="/" className="text-[#d71920] underline">
            Giriş sayfası
          </Link>
          {" · "}
          <Link to="/sifremi-unuttum" className="text-[#d71920] underline">
            Yeni sıfırlama talebi
          </Link>
        </p>
      </div>
    </div>
  );
};

export default ResetPasswordPage;
