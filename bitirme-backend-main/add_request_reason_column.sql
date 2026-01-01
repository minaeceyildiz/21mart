-- ============================================
-- REQUEST_REASON KOLONUNU EKLEME
-- ============================================
-- Bu script request_reason kolonunu appointments tablosuna ekler
-- Öğrencinin "diğer" seçeneğinde yazdığı özel metin için kullanılır
-- ============================================

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
        
        RAISE NOTICE 'request_reason kolonu başarıyla eklendi.';
    ELSE
        RAISE NOTICE 'request_reason kolonu zaten mevcut.';
    END IF;
END $$;

-- Kontrol: Kolonun eklendiğini doğrula
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'appointments' 
AND column_name = 'request_reason';

