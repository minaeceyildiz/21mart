import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login, register, ApiError } from "../services/authService";

const LoginPage: React.FC = () => {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<"student" | "instructor">("student");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");

  const [isSignup, setIsSignup] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await login({ username, password, role });

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
      } else {
        // Eğer role belirlenemezse, seçilen role göre yönlendir
        console.log('Role not determined, using selected role:', role); // Debug için
        if (role === "student") {
          navigate("/ogrenci", { replace: true });
        } else {
          navigate("/ogretim-elemani", { replace: true });
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
        role,
        studentNo: role === "student" ? undefined : undefined,
      });

      console.log('Register response:', response); // Debug için
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
      } else {
        // Eğer role belirlenemezse, seçilen role göre yönlendir
        console.log('Role not determined, using selected role (register):', role); // Debug için
        if (role === "student") {
          navigate("/ogrenci", { replace: true });
        } else {
          navigate("/ogretim-elemani", { replace: true });
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

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <input
          type="text"
          placeholder={isSignup ? "Kullanıcı adı" : "Kullanıcı adı"}
          value={username}
          onChange={(e) => setUsername(e.target.value)}
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
          onChange={(e) => setRole(e.target.value as "student" | "instructor")}
          className="border rounded-lg px-4 py-2"
        >
          <option value="student">Öğrenci</option>
          <option value="instructor">Akademik Personel</option>
        </select>

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
                onClick={() => setIsSignup(false)}
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
                onClick={() => setIsSignup(true)}
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
