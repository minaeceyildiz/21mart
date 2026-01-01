-- request_reason kolonunu eklemek için bu SQL'i PostgreSQL'de çalıştırın
-- Bu dosyayı pgAdmin veya psql ile çalıştırabilirsiniz

-- Kolonun var olup olmadığını kontrol et ve yoksa ekle
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

