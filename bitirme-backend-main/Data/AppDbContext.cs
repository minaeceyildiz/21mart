using ApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // --- İŞTE BUNLAR EKSİK OLDUĞU İÇİN TABLOLAR GELMEDİ ---
        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicit table name mappings to existing database tables (case-sensitive)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Appointment>().ToTable("appointments");
            modelBuilder.Entity<MenuItem>().ToTable("cafeteria_menu_items");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
            modelBuilder.Entity<Notification>().ToTable("Notifications");

            // User entity column mappings - Veritabanındaki gerçek kolon adlarına göre
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                // role_id kolonu integer - enum'ı integer'a çevir
                // roles tablosundaki ID'ler (1, 2, 3, 4) olabilir, enum değerlerimiz (0, 1, 2, 3)
                // Bu yüzden enum değerini +1 yaparak kaydediyoruz (Student=0 -> role_id=1, Teacher=1 -> role_id=2)
                entity.Property(e => e.Role)
                    .HasColumnName("role_id")
                    .HasConversion(
                        v => (int)v + 1, // Enum'dan DB'ye: Student (0) -> 1, Teacher (1) -> 2, vb.
                        v => (UserRole)((int)v - 1)); // DB'den enum'a: 1 -> Student (0), 2 -> Teacher (1), vb.
                entity.Property(e => e.Name).HasColumnName("full_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.StudentNo).HasColumnName("staff_id");
                // login_type kolonu veritabanında özel bir tip (login_type ENUM) olduğu için mapping'den çıkarıyoruz
                // Yeni kullanıcı kaydında bu kolonu kullanmıyoruz
                entity.Ignore(e => e.LoginType);
            });

            // Enum Ayarları
            modelBuilder.Entity<Appointment>().Property(a => a.Status).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<string>();
            modelBuilder.Entity<Notification>().Property(n => n.Type).HasConversion<string>();

            // İlişki Ayarları - Shadow property uyarısını önlemek için navigation property'leri belirt
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Student)
                .WithMany(u => u.StudentAppointments)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Teacher)
                .WithMany(u => u.TeacherAppointments)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
