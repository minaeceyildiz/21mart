-- ============================================
-- VERİTABANI GÜNCELLİK KONTROL SCRIPT'İ
-- ============================================
-- Bu script mevcut veritabanınızın güncel olup olmadığını kontrol eder
-- ============================================

-- 1. APPOINTMENTS TABLOSU KONTROLÜ
-- ============================================
-- UserId ve UserId1 kolonları OLMAMALI
SELECT 
    'Appointments Tablosu Kontrolü' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'Appointments' 
            AND column_name IN ('UserId', 'UserId1')
        ) THEN '❌ HATA: UserId veya UserId1 kolonları VAR (kaldırılmalı)'
        ELSE '✅ OK: UserId/UserId1 kolonları YOK (güncel)'
    END AS durum;

-- StudentId ve TeacherId kolonları OLMALI
SELECT 
    'Appointments - StudentId/TeacherId Kontrolü' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'Appointments' 
            AND column_name IN ('StudentId', 'TeacherId')
        ) THEN '✅ OK: StudentId ve TeacherId kolonları VAR'
        ELSE '❌ HATA: StudentId veya TeacherId kolonları YOK'
    END AS durum;

-- Appointments tablosundaki tüm kolonları listele
SELECT 
    'Appointments Tablosu Kolonları' AS bilgi,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'Appointments'
ORDER BY ordinal_position;

-- ============================================
-- 2. INSTRUCTOR_SCHEDULE TABLOSU KONTROLÜ
-- ============================================
-- Tablo var mı?
SELECT 
    'instructor_schedule Tablosu Kontrolü' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.tables 
            WHERE table_name = 'instructor_schedule'
        ) THEN '✅ OK: instructor_schedule tablosu VAR'
        ELSE '❌ HATA: instructor_schedule tablosu YOK (oluşturulmalı)'
    END AS durum;

-- Tablo varsa kolonları kontrol et
SELECT 
    'instructor_schedule Tablosu Kolonları' AS bilgi,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'instructor_schedule'
ORDER BY ordinal_position;

-- day_of_week tipi kontrolü (smallint olmalı)
SELECT 
    'instructor_schedule - day_of_week Tipi' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'day_of_week'
            AND data_type = 'smallint'
        ) THEN '✅ OK: day_of_week smallint tipinde'
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'day_of_week'
        ) THEN '❌ HATA: day_of_week yanlış tipte (smallint olmalı)'
        ELSE '❌ HATA: day_of_week kolonu YOK'
    END AS durum;

-- start_time tipi kontrolü (time olmalı)
SELECT 
    'instructor_schedule - start_time Tipi' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'start_time'
            AND (data_type = 'time' OR udt_name = 'time')
        ) THEN '✅ OK: start_time time tipinde'
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'start_time'
        ) THEN '❌ HATA: start_time yanlış tipte (time olmalı)'
        ELSE '❌ HATA: start_time kolonu YOK'
    END AS durum;

-- course_name kontrolü (NOT NULL olmalı)
SELECT 
    'instructor_schedule - course_name Kontrolü' AS kontrol,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'course_name'
            AND is_nullable = 'NO'
        ) THEN '✅ OK: course_name NOT NULL'
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'course_name'
        ) THEN '❌ HATA: course_name NULL olabilir (NOT NULL olmalı)'
        ELSE '❌ HATA: course_name kolonu YOK'
    END AS durum;

-- ============================================
-- 3. ÖZET RAPOR
-- ============================================
SELECT 
    '=== ÖZET RAPOR ===' AS rapor,
    '' AS bos_satir;

-- Appointments tablosu güncel mi?
SELECT 
    'Appointments Tablosu' AS tablo,
    CASE 
        WHEN NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'Appointments' 
            AND column_name IN ('UserId', 'UserId1')
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'Appointments' 
            AND column_name IN ('StudentId', 'TeacherId')
        ) THEN '✅ GÜNCEL'
        ELSE '❌ GÜNCELLENMELİ'
    END AS durum;

-- instructor_schedule tablosu var mı ve güncel mi?
SELECT 
    'instructor_schedule Tablosu' AS tablo,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = 'instructor_schedule'
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'day_of_week'
            AND data_type = 'smallint'
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'instructor_schedule' 
            AND column_name = 'start_time'
            AND (data_type = 'time' OR udt_name = 'time')
        ) THEN '✅ GÜNCEL'
        ELSE '❌ GÜNCELLENMELİ'
    END AS durum;





