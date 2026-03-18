using ApiProject.Models;
using BCrypt.Net;

namespace ApiProject.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. KULLANICILARI EKLE
            if (!context.Users.Any())
            {
                var student = new User
                {
                    Name = "Ali Ogrenci",
                    Email = "ali.ogrenci@baskent.edu.tr",
                    Role = UserRole.Student,
                    StudentNo = "20231001",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("baskent123")
                };

                var teacher = new User
                {
                    Name = "Mehmet Hoca",
                    Email = "hoca@baskent.edu.tr",
                    Role = UserRole.Teacher,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("baskent123")
                };

                var admin = new User
                {
                    Name = "Admin",
                    Email = "admin@baskent.edu.tr",
                    Role = UserRole.Admin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
                };

                context.Users.AddRange(student, teacher, admin);
                context.SaveChanges();
            }

            // 2. MENÜYÜ EKLE
            if (!context.MenuItems.Any())
            {
                var menuItems = new MenuItem[]
                {
                    new MenuItem { Name = "Hamburger", Price = 150, Description = "Klasik", IsAvailable = true },
                    new MenuItem { Name = "Tost", Price = 50, Description = "Kaşarlı", IsAvailable = true },
                    new MenuItem { Name = "Çay", Price = 10, Description = "Taze", IsAvailable = true }
                };
                context.MenuItems.AddRange(menuItems);
                context.SaveChanges();
            }

            // 3. ÖRNEK SİPARİŞ EKLE (YENİ KISIM)
            if (!context.Orders.Any())
            {
                // Kullanıcıyı ve Yemeği bul
                var ali = context.Users.FirstOrDefault(u => u.Email == "ali.ogrenci@baskent.edu.tr");
                var burger = context.MenuItems.FirstOrDefault(m => m.Name == "Hamburger");

                if (ali != null && burger != null)
                {
                    var order = new Order
                    {
                        StudentId = ali.Id,
                        OrderDate = DateTime.Now,
                        Status = OrderStatus.Preparing, // Mutfakta hazırlanıyor görünsün
                        TotalAmount = burger.Price
                    };
                    
                    // Siparişi kaydet ki ID oluşsun
                    context.Orders.Add(order);
                    context.SaveChanges();

                    // Sipariş Detayını ekle
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = burger.Id,
                        Quantity = 1,
                        Price = burger.Price
                    };
                    context.OrderItems.Add(orderItem);
                    context.SaveChanges();
                }
            }
        }
    }
}
