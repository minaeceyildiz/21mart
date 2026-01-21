# 🚀 Hızlı Veritabanı Kurulumu

Arkadaşlarınız için **en hızlı** kurulum yöntemi:

## ⚡ 3 Adımda Kurulum

### 1️⃣ PostgreSQL'de Veritabanı Oluştur

```sql
CREATE DATABASE bitirme_db1;
```

### 2️⃣ Connection String'i Güncelle

`appsettings.json` dosyasını açın ve kendi bilgilerinizi yazın:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bitirme_db1;Username=postgres;Password=SIZIN_SIFRENIZ;Include Error Detail=true"
  }
}
```

### 3️⃣ SQL Script'i Çalıştır

**pgAdmin veya DBeaver kullanıyorsanız:**
1. `setup_database.sql` dosyasını açın
2. Tüm içeriği kopyalayın
3. Query tool'da yapıştırın ve çalıştırın (F5)

**Komut satırından:**
```bash
psql -U postgres -d bitirme_db1 -f setup_database.sql
```

### 4️⃣ Backend'i Çalıştır

```bash
cd bitirme-backend-main
dotnet run
```

✅ **Hazır!** Backend otomatik olarak örnek verileri ekleyecek.

---

## 📋 Oluşturulan Tablolar

- ✅ `users` - Kullanıcılar
- ✅ `Appointments` - Randevular (UserId/UserId1 YOK)
- ✅ `instructor_schedule` - Hoca ders programı
- ✅ `Notifications` - Bildirimler
- ✅ `MenuItems` - Menü öğeleri
- ✅ `Orders` - Siparişler
- ✅ `OrderItems` - Sipariş detayları
- ✅ `OccupancyLogs` - Doluluk logları

---

## 🔑 Test Kullanıcıları

Backend başladığında otomatik oluşturulur:

| Email | Şifre | Rol |
|-------|-------|-----|
| `ali.ogrenci@baskent.edu.tr` | `baskent123` | Öğrenci |
| `hoca@baskent.edu.tr` | `baskent123` | Akademik Personel |
| `admin@baskent.edu.tr` | `admin123` | Admin |

---

## ❓ Sorun mu var?

Detaylı rehber için: `DATABASE_SETUP.md` dosyasına bakın.













