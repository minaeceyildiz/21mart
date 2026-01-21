-- login_type kolonunu nullable (NULL kabul eden) yap
ALTER TABLE users 
ALTER COLUMN login_type DROP NOT NULL;

-- Kontrol et
SELECT column_name, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'users' AND column_name = 'login_type';

-- Mevcut kullanıcıları da NULL yap (test için)
UPDATE users SET login_type = NULL WHERE id > 0;

SELECT id, full_name, email, login_type FROM users ORDER BY id DESC LIMIT 10;

