-- ============================================
-- GÜNCEL VERİTABANI KURULUM SCRIPT'İ
-- ============================================
-- Bu script tüm güncel tabloları oluşturur
-- PostgreSQL için hazırlanmıştır
-- ============================================

-- 1. APPOINTMENTS TABLOSU (Güncel - UserId/UserId1 YOK)
-- ============================================
DROP TABLE IF EXISTS "Appointments" CASCADE;

CREATE TABLE "Appointments" (
    "Id" SERIAL PRIMARY KEY,
    "StudentId" INTEGER NOT NULL,
    "TeacherId" INTEGER NOT NULL,
    "Date" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Time" INTERVAL NOT NULL,
    "Subject" VARCHAR(200) NOT NULL,
    "RequestReason" VARCHAR(500),
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "RejectionReason" VARCHAR(500),
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITHOUT TIME ZONE,
    
    -- Foreign Key Constraints
    CONSTRAINT "FK_Appointments_Users_StudentId" 
        FOREIGN KEY ("StudentId") 
        REFERENCES "users"("id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_Appointments_Users_TeacherId" 
        FOREIGN KEY ("TeacherId") 
        REFERENCES "users"("id") 
        ON DELETE RESTRICT
);

-- Index'ler
CREATE INDEX "IX_Appointments_StudentId" ON "Appointments"("StudentId");
CREATE INDEX "IX_Appointments_TeacherId" ON "Appointments"("TeacherId");
CREATE INDEX "IX_Appointments_Date" ON "Appointments"("Date");

-- ============================================
-- 2. INSTRUCTOR_SCHEDULE TABLOSU (YENİ)
-- ============================================
DROP TABLE IF EXISTS "instructor_schedule" CASCADE;

CREATE TABLE "instructor_schedule" (
    "id" SERIAL PRIMARY KEY,
    "instructor_id" INTEGER NOT NULL,
    "day_of_week" SMALLINT NOT NULL CHECK ("day_of_week" >= 1 AND "day_of_week" <= 5),
    "start_time" TIME WITHOUT TIME ZONE NOT NULL,
    "course_name" VARCHAR(200) NOT NULL DEFAULT '',
    
    -- Foreign Key Constraint
    CONSTRAINT "FK_instructor_schedule_users_instructor_id" 
        FOREIGN KEY ("instructor_id") 
        REFERENCES "users"("id") 
        ON DELETE CASCADE
);

-- Index
CREATE INDEX "IX_instructor_schedule_instructor_id" ON "instructor_schedule"("instructor_id");

-- ============================================
-- 3. NOTIFICATIONS TABLOSU (Eğer yoksa)
-- ============================================
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Message" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL,
    "RecipientEmail" VARCHAR(255),
    "RecipientUserId" INTEGER,
    "RelatedEntityId" INTEGER,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Notifications_Users_RecipientUserId" 
        FOREIGN KEY ("RecipientUserId") 
        REFERENCES "users"("id") 
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_RecipientUserId" ON "Notifications"("RecipientUserId");
CREATE INDEX IF NOT EXISTS "IX_Notifications_IsRead" ON "Notifications"("IsRead");

-- ============================================
-- 4. USERS TABLOSU (Eğer yoksa - temel yapı)
-- ============================================
-- NOT: Eğer users tablosu zaten varsa, bu kısmı atlayın
-- CREATE TABLE IF NOT EXISTS "users" (
--     "id" SERIAL PRIMARY KEY,
--     "role_id" INTEGER NOT NULL,
--     "full_name" VARCHAR(100) NOT NULL,
--     "email" VARCHAR(120) NOT NULL UNIQUE,
--     "password_hash" TEXT NOT NULL,
--     "staff_id" VARCHAR(40)
-- );

-- ============================================
-- 5. DİĞER TABLOLAR (Eğer yoksa)
-- ============================================
-- MenuItems, Orders, OrderItems, OccupancyLogs tabloları
-- Migration'lar veya DbInitializer tarafından oluşturulacak

-- ============================================
-- KURULUM TAMAMLANDI
-- ============================================




