-- request_reason kolonunun var olup olmadığını kontrol et
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'appointments' 
AND column_name = 'request_reason';

-- Eğer sonuç boşsa, kolon yok demektir
-- Eğer sonuç varsa, kolon zaten mevcut demektir

