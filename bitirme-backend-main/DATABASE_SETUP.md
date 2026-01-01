# 🗄️ Veritabanı Kurulum Rehberi

Bu rehber, projeyi ilk kez çalıştıracak arkadaşlarınız için hazırlanmıştır.

## 📋 Gereksinimler

- PostgreSQL 12+ yüklü olmalı
- .NET 6.0+ SDK yüklü olmalı
- PostgreSQL'de bir veritabanı oluşturulmuş olmalı

---

## 🚀 Yöntem 1: SQL Script ile Kurulum (ÖNERİLEN)

### Adım 1: PostgreSQL'e Bağlanın

```bash
# PostgreSQL komut satırı ile bağlanın
psql -U postgres -d bitirme_db1
```

Veya pgAdmin, DBeaver gibi bir GUI tool kullanabilirsiniz.

### Adım 2: Connection String'i Kontrol Edin

`appsettings.json` dosyasındaki connection string'i kendi bilgilerinize göre güncelleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bitirme_db1;Username=postgres;Password=1234;Include Error Detail=true"
  }
}
```

**Önemli:** 
- `Database=bitirme_db1` → Kendi veritabanı adınızı yazın
- `Username=postgres` → Kendi kullanıcı adınızı yazın
- `Password=1234` → Kendi şifrenizi yazın

### Adım 3: Veritabanını Oluşturun (Eğer yoksa)

```sql
CREATE DATABASE bitirme_db1;
```

### Adım 4: SQL Script'i Çalıştırın

**Seçenek A: psql ile**
```bash
psql -U postgres -d bitirme_db1 -f setup_database.sql
```

**Seçenek B: pgAdmin/DBeaver ile**
1. `setup_database.sql` dosyasını açın
2. Tüm içeriği kopyalayın
3. Query tool'da yapıştırın ve çalıştırın

### Adım 5: Backend'i Çalıştırın

```bash
cd bitirme-backend-main
dotnet run
```

Backend başladığında `DbInitializer` otomatik olarak:
- Eksik tabloları oluşturacak
- Örnek kullanıcıları ekleyecek (öğrenci, hoca, admin)
- Örnek menü öğelerini ekleyecek

---

## 🔄 Yöntem 2: Migration ile Kurulum

### Adım 1: Connection String'i Güncelleyin

`appsettings.json` dosyasını düzenleyin (Yöntem 1, Adım 2'ye bakın).

### Adım 2: Migration'ları Uygulayın

```bash
cd bitirme-backend-main

# Migration'ları veritabanına uygula
dotnet ef database update
```

### Adım 3: Appointments Tablosunu Düzeltin

Migration'larda eski `UserId` ve `UserId1` kolonları olabilir. Bunları kaldırmak için:

```sql
-- PostgreSQL'de çalıştırın
ALTER TABLE "Appointments" DROP COLUMN IF EXISTS "UserId";
ALTER TABLE "Appointments" DROP COLUMN IF EXISTS "UserId1";
DROP INDEX IF EXISTS "IX_Appointments_UserId";
DROP INDEX IF EXISTS "IX_Appointments_UserId1";
```

### Adım 4: Instructor_Schedule Tablosunu Oluşturun

```sql
-- setup_database.sql dosyasındaki "2. INSTRUCTOR_SCHEDULE TABLOSU" bölümünü çalıştırın
```

### Adım 5: Backend'i Çalıştırın

```bash
dotnet run
```

---

## ✅ Kurulum Kontrolü

### Tabloları Kontrol Edin

```sql
-- Tüm tabloları listeleyin
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Beklenen tablolar:
-- - users
-- - Appointments
-- - instructor_schedule
-- - Notifications
-- - MenuItems
-- - Orders
-- - OrderItems
-- - OccupancyLogs
```

### Appointments Tablosunu Kontrol Edin

```sql
-- UserId/UserId1 kolonları OLMAMALI
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Appointments';

-- Sadece şunlar olmalı:
-- Id, StudentId, TeacherId, Date, Time, Subject, RequestReason, Status, RejectionReason, CreatedAt, UpdatedAt
```

### Instructor_Schedule Tablosunu Kontrol Edin

```sql
-- Tablo yapısını kontrol edin
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'instructor_schedule';

-- Beklenen kolonlar:
-- id, instructor_id, day_of_week (smallint), start_time (time), course_name
```

---

## 🐛 Sorun Giderme

### Hata: "Connection string not found"

**Çözüm:** `appsettings.json` dosyasında `DefaultConnection` key'inin olduğundan emin olun.

### Hata: "Table already exists"

**Çözüm:** `setup_database.sql` dosyasında `DROP TABLE IF EXISTS` komutları var, bu normal. Eğer hala hata alıyorsanız, tabloyu manuel olarak silin:

```sql
DROP TABLE IF EXISTS "Appointments" CASCADE;
DROP TABLE IF EXISTS "instructor_schedule" CASCADE;
```

### Hata: "Foreign key constraint violation"

**Çözüm:** Önce `users` tablosunun var olduğundan emin olun. Eğer yoksa, backend'i çalıştırın, `DbInitializer` otomatik oluşturacaktır.

### Hata: "Column day_of_week is of type smallint but expression is of type character varying"

**Çözüm:** `instructor_schedule` tablosunu silip yeniden oluşturun:

```sql
DROP TABLE IF EXISTS "instructor_schedule" CASCADE;

-- setup_database.sql dosyasındaki "2. INSTRUCTOR_SCHEDULE TABLOSU" bölümünü çalıştırın
```

---

## 📝 Örnek Kullanıcılar

Backend başladığında otomatik olarak şu kullanıcılar oluşturulur:

| Email | Şifre | Rol |
|-------|-------|-----|
| ali.ogrenci@baskent.edu.tr | baskent123 | Öğrenci |
| hoca@baskent.edu.tr | baskent123 | Akademik Personel |
| admin@baskent.edu.tr | admin123 | Admin |

---

## 🎯 Hızlı Başlangıç (Özet)

1. PostgreSQL'de veritabanı oluştur: `CREATE DATABASE bitirme_db1;`
2. `appsettings.json`'daki connection string'i güncelle
3. `setup_database.sql` dosyasını çalıştır
4. Backend'i çalıştır: `dotnet run`
5. Frontend'i çalıştır: `npm start` (frontend klasöründe)

---

## 📞 Yardım

Eğer sorun yaşarsanız:
1. Backend log'larını kontrol edin
2. PostgreSQL log'larını kontrol edin
3. `setup_database.sql` dosyasını adım adım çalıştırın (her bölümü ayrı ayrı)

---

**Son Güncelleme:** 2025-01-XX
**Hazırlayan:** Proje Ekibi




