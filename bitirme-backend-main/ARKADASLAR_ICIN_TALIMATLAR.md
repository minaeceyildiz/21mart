# Arkadaşlar İçin Talimatlar - request_reason Kolonu

## Sorun

Projeyi çalıştırdığınızda `request_reason` kolonu eksik olabilir ve randevu oluştururken hata alabilirsiniz.

## Çözüm (3 Yöntem)

### Yöntem 1: Otomatik (Backend'i Yeniden Başlat)

1. Backend'i durdurun (Ctrl+C)
2. Backend'i yeniden başlatın
3. Backend başlarken otomatik olarak kolon eklenir
4. Console'da "request_reason kolonu kontrol edildi/eklendi." mesajını görürsünüz

### Yöntem 2: Manuel SQL (Önerilen)

Eğer otomatik ekleme çalışmazsa:

1. PostgreSQL'e bağlanın (pgAdmin veya psql)
2. `add_request_reason_column.sql` dosyasını açın
3. SQL'i çalıştırın
4. Backend'i yeniden başlatın

**Dosya:** `bitirme-backend-main/add_request_reason_column.sql`

### Yöntem 3: Migration Manuel Uygulama

Terminal'de backend klasöründe:

```bash
dotnet ef database update
```

## Kontrol

Kolonun eklendiğini kontrol etmek için:

```sql
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'appointments' 
AND column_name = 'request_reason';
```

Eğer sonuç boşsa, kolon yok demektir.

## Hata: "must be owner of table appointments"

Bu hata, veritabanı kullanıcınızın tablo sahibi olmadığını gösterir.

**Çözüm:**

```sql
-- Veritabanı yöneticisi olarak çalıştırın
ALTER TABLE "appointments" OWNER TO [veritabanı_kullanıcı_adı];
```

Örnek:
```sql
ALTER TABLE "appointments" OWNER TO postgres;
-- veya
ALTER TABLE "appointments" OWNER TO oys3;
```

## Yapılan Değişiklikler Özeti

1. **Yeni Kolon:** `appointments` tablosuna `request_reason` TEXT kolonu eklendi
2. **Migration:** `Migrations/20251225000000_AddRequestReasonColumn.cs` dosyası eklendi
3. **Program.cs:** Backend başlarken otomatik kolon kontrolü eklendi
4. **AppDbContext:** `RequestReason` property'si `request_reason` kolonuna map edildi

## Test

1. Öğrenci olarak giriş yapın
2. "Diğer" seçeneğini seçip özel metin yazın (örn: "raporluyum")
3. Randevu oluşturun
4. Hata almamalısınız
5. Hoca olarak giriş yapın
6. Randevu talebinde öğrencinin yazdığı metni görün

## Sorun Devam Ederse

1. Backend console log'larını kontrol edin
2. Veritabanı bağlantısını kontrol edin
3. Tablo sahipliğini kontrol edin
4. `add_request_reason_column.sql` dosyasını manuel çalıştırın

