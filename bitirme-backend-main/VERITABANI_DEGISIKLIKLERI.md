# Veritabanı Değişiklikleri - request_reason Kolonu

## Yapılan Değişiklik

`appointments` tablosuna `request_reason` adında yeni bir kolon eklendi. Bu kolon, öğrencinin "Diğer" seçeneğinde yazdığı özel metni saklamak için kullanılıyor.

## Kolon Bilgileri

- **Kolon Adı:** `request_reason`
- **Tip:** `TEXT` (nullable)
- **Amaç:** Öğrencinin yazdığı özel metni saklamak

## Otomatik Ekleme Yöntemleri

### 1. Migration (Önerilen - Otomatik)

Backend başlarken otomatik olarak migration uygulanır:
- Dosya: `Migrations/20251225000000_AddRequestReasonColumn.cs`
- `Program.cs`'de `context.Database.Migrate()` ile otomatik uygulanır

### 2. Program.cs Kontrolü (Yedek)

Migration başarısız olursa, `Program.cs`'deki kod kolonu kontrol eder ve yoksa ekler.

## Manuel Ekleme (Gerekirse)

Eğer otomatik ekleme çalışmazsa, aşağıdaki SQL'i çalıştırın:

```sql
-- request_reason kolonunu ekle (eğer yoksa)
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'appointments' 
        AND column_name = 'request_reason'
    ) THEN
        ALTER TABLE "appointments" 
        ADD COLUMN "request_reason" TEXT;
        RAISE NOTICE 'request_reason kolonu eklendi.';
    ELSE
        RAISE NOTICE 'request_reason kolonu zaten mevcut.';
    END IF;
END $$;
```

## Kontrol

Kolonun var olup olmadığını kontrol etmek için:

```sql
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'appointments' 
AND column_name = 'request_reason';
```

## Sorun Giderme

### Hata: "must be owner of table appointments"

**Çözüm:** Tablo sahipliğini değiştirin:
```sql
ALTER TABLE "appointments" OWNER TO [veritabanı_kullanıcı_adı];
```

### Migration Çalışmıyor

1. `dotnet ef migrations list` ile migration'ları kontrol edin
2. `dotnet ef database update` ile manuel uygulayın
3. Veya yukarıdaki SQL'i manuel çalıştırın

## Kod Değişiklikleri

1. **AppDbContext.cs:** `RequestReason` property'si `request_reason` kolonuna map edildi
2. **AppointmentService.cs:** INSERT ve SELECT işlemlerinde `request_reason` kolonu kullanılıyor
3. **Program.cs:** Backend başlarken kolon kontrolü yapılıyor

## Test

Kolonun çalışıp çalışmadığını test etmek için:
1. Öğrenci olarak giriş yapın
2. "Diğer" seçeneğini seçip özel metin yazın
3. Randevu oluşturun
4. Hoca olarak giriş yapın
5. Randevu talebinde öğrencinin yazdığı metni görün

