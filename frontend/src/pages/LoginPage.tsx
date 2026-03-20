import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { login, register, ApiError } from "../services/authService";

const LoginPage: React.FC = () => {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<"student" | "instructor" | "cashier">("student");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");
  const [showError, setShowError] = useState(false);
  const [success, setSuccess] = useState<string>("");
  const [showSuccess, setShowSuccess] = useState(false);

  const [isSignup, setIsSignup] = useState(false);

  // Hata mesajı gösterildiğinde animasyon için
  useEffect(() => {
    if (error) {
      setShowError(true);
      setShowSuccess(false);
      setSuccess("");
    }
  }, [error]);

  // Başarı mesajı gösterildiğinde animasyon için
  useEffect(() => {
    if (success) {
      setShowSuccess(true);
      setShowError(false);
      setError("");
    }
  }, [success]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setShowError(false);
    setShowSuccess(false);
    setLoading(true);

    try {
      // Kasiyer kısayolu: kullanıcı adı/şifre bu ise otomatik kasiyer rolüyle login dene
      const isCashierShortcut =
        username.trim().toLowerCase() === "kasiyer" && password === "123456";

      const roleToUse = isCashierShortcut ? "cashier" : role;

      const response = await login({ username, password, role: roleToUse });

      console.log('Login response:', response); // Debug için
      console.log('User role:', response.user.role); // Debug için

      // Başarılı giriş sonrası role göre yönlendir
      // Backend'den gelen role değerini kullan (normalizedRole: 'student' veya 'instructor')
      const userRole = response.user.role;
      console.log('Navigating with role:', userRole); // Debug için

      if (userRole === "student") {
        console.log('Navigating to /ogrenci'); // Debug için
        navigate("/ogrenci", { replace: true });
      } else if (userRole === "instructor") {
        console.log('Navigating to /ogretim-elemani'); // Debug için
        navigate("/ogretim-elemani", { replace: true });
      } else if (userRole === "cashier") {
        navigate("/kasiyer/siparisler", { replace: true });
      } else {
        // Eğer role belirlenemezse, seçilen role göre yönlendir
        console.log('Role not determined, using selected role:', role); // Debug için
        if (role === "student") {
          navigate("/ogrenci", { replace: true });
        } else if (role === "instructor") {
          navigate("/ogretim-elemani", { replace: true });
        } else {
          navigate("/kasiyer/siparisler", { replace: true });
        }
      }
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Giriş yapılırken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setShowError(false);
    setShowSuccess(false);
    setLoading(true);

    try {
      // Eğer kullanıcı adı bir e-posta değilse, örnek bir e-posta kullan
      const isEmail = username.includes("@");
      const email = isEmail ? username : `${username}@example.com`;
      const name = isEmail ? username.split("@")[0] : username;

      const response = await register({
        name,
        email,
        password,
        role: role === "cashier" ? "student" : role,
        studentNo: role === "student" ? undefined : undefined,
      });

      console.log('Register response:', response); // Debug için

      // KONTROL: Token yoksa (Email doğrulama gerekli) yönlendirme YAPMA
      if (!response.token) {
        setSuccess("Kayıt başarılı! 🎉\n\nE-posta adresinize bir doğrulama linki gönderdik. Lütfen e-postanızı kontrol edin ve doğrulama linkine tıklayın.\n\nE-postayı bulamıyorsanız spam klasörünü kontrol etmeyi unutmayın.");
        setIsSignup(false);
        setUsername("");
        setPassword("");
        return; // Fonksiyondan çık, yönlendirme yapma
      }

      console.log('User role:', response.user.role); // Debug için

      // Kayıt sonrası role göre yönlendir
      // Backend'den gelen role değerini kullan (normalizedRole: 'student' veya 'instructor')
      const userRole = response.user.role;
      console.log('Navigating with role (register):', userRole); // Debug için

      if (userRole === "student") {
        console.log('Navigating to /ogrenci (register)'); // Debug için
        navigate("/ogrenci", { replace: true });
      } else if (userRole === "instructor") {
        console.log('Navigating to /ogretim-elemani (register)'); // Debug için
        navigate("/ogretim-elemani", { replace: true });
      } else if (userRole === "cashier") {
        navigate("/kasiyer/siparisler", { replace: true });
      } else {
        // Eğer role belirlenemezse, seçilen role göre yönlendir
        console.log('Role not determined, using selected role (register):', role); // Debug için
        if (role === "student") {
          navigate("/ogrenci", { replace: true });
        } else if (role === "instructor") {
          navigate("/ogretim-elemani", { replace: true });
        } else {
          navigate("/kasiyer/siparisler", { replace: true });
        }
      }
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Kayıt yapılırken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100">
      <form
        onSubmit={isSignup ? handleRegister : handleSubmit}
        className="w-full max-w-sm bg-white p-6 rounded-xl shadow flex flex-col gap-4"
      >
        <h1 className="text-xl font-semibold text-center text-slate-800">
          {isSignup ? "Başkent Yaşam Kayıt" : "Başkent Yaşam Giriş"}
        </h1>

        {/* Başarı Mesajı */}
        {success && showSuccess && (
          <div 
            className="bg-green-50 border-l-4 border-green-500 text-green-800 px-5 py-4 rounded-lg shadow-lg relative transition-all duration-300 ease-out"
            style={{
              animation: 'slideDown 0.3s ease-out',
              minHeight: '80px'
            }}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-start gap-3 flex-1">
                <span className="text-2xl flex-shrink-0 mt-1">✅</span>
                <div className="flex-1">
                  <p className="font-bold text-base mb-2 text-green-900">Başarılı!</p>
                  <p className="text-sm leading-relaxed whitespace-pre-line text-green-700">{success}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowSuccess(false);
                  setTimeout(() => setSuccess(""), 300);
                }}
                className="text-green-400 hover:text-green-700 hover:bg-green-100 rounded-full w-6 h-6 flex items-center justify-center flex-shrink-0 transition-colors duration-200"
                aria-label="Kapat"
                title="Kapat"
              >
                <span className="text-xl font-bold leading-none">×</span>
              </button>
            </div>
          </div>
        )}

        {/* Hata Mesajı */}
        {error && showError && (
          <div 
            className="bg-red-50 border-l-4 border-red-500 text-red-800 px-5 py-4 rounded-lg shadow-lg relative transition-all duration-300 ease-out"
            style={{
              animation: 'slideDown 0.3s ease-out',
              minHeight: '80px'
            }}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-start gap-3 flex-1">
                <span className="text-2xl flex-shrink-0 mt-1">⚠️</span>
                <div className="flex-1">
                  <p className="font-bold text-base mb-2 text-red-900">Dikkat!</p>
                  <p className="text-sm leading-relaxed whitespace-pre-line text-red-700">{error}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowError(false);
                  setTimeout(() => setError(""), 300);
                }}
                className="text-red-400 hover:text-red-700 hover:bg-red-100 rounded-full w-6 h-6 flex items-center justify-center flex-shrink-0 transition-colors duration-200"
                aria-label="Kapat"
                title="Kapat"
              >
                <span className="text-xl font-bold leading-none">×</span>
              </button>
            </div>
          </div>
        )}

        <input
          type="text"
          placeholder={isSignup ? "Kullanıcı adı" : "Kullanıcı adı"}
          value={username}
          onChange={(e) => {
            const val = e.target.value;
            setUsername(val);
            // Akıllı rol seçimi (Sadece kayıt olurken)
            if (isSignup && val.length > 0) {
              // Rakamla başlıyorsa -> Öğrenci, yoksa -> Akademik
              if (/^\d/.test(val)) {
                setRole("student");
              } else {
                setRole("instructor");
              }
            }
          }}
          className="border rounded-lg px-4 py-2"
        />

        <input
          type="password"
          placeholder="Şifre"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="border rounded-lg px-4 py-2"
        />

        <select
          value={role}
          onChange={(e) => setRole(e.target.value as "student" | "instructor" | "cashier")}
          className="border rounded-lg px-4 py-2"
        >
          <option value="student">Öğrenci</option>
          <option value="instructor">Akademik Personel</option>
          {!isSignup && <option value="cashier">Kasiyer</option>}
        </select>
        {!isSignup && (
          <div className="text-right mt-2">
            <Link
              to="/sifremi-unuttum"
              className="text-sm text-blue-600 hover:underline"
            >
              Şifremi Unuttum
            </Link>
          </div>
        )}
        <button
          type="submit"
          disabled={loading}
          className="bg-[#d71920] text-white py-2 rounded-lg hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading
            ? isSignup
              ? "Kayıt yapılıyor..."
              : "Giriş yapılıyor..."
            : isSignup
              ? "Kayıt ol"
              : "Giriş yap"}
        </button>

        <p className="text-center text-sm text-slate-500">
          {isSignup ? (
            <>
              Hesabınız var mı?{" "}
              <button
                type="button"
                onClick={() => {
                  setIsSignup(false);
                  setError("");
                  setSuccess("");
                  setShowError(false);
                  setShowSuccess(false);
                }}
                className="underline text-[#d71920]"
              >
                Giriş yap
              </button>
            </>
          ) : (
            <>
              Hesabınız yok mu?{" "}
              <button
                type="button"
                onClick={() => {
                  setIsSignup(true);
                  setError("");
                  setSuccess("");
                  setShowError(false);
                  setShowSuccess(false);
                }}
                className="underline text-[#d71920]"
              >
                Kayıt ol
              </button>
            </>
          )}
        </p>
      </form>
    </div>
  );
};

export default LoginPage;
