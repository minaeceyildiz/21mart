-- appointment_reason enum değerlerini kontrol et
SELECT 
    t.typname AS enum_name,
    e.enumlabel AS enum_value
FROM pg_type t 
JOIN pg_enum e ON t.oid = e.enumtypid  
WHERE t.typname = 'appointment_reason'
ORDER BY e.enumsortorder;

