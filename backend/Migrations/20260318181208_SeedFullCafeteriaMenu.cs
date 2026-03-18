using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProject.Migrations
{
    /// <inheritdoc />
    public partial class SeedFullCafeteriaMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Tavuklu Sandviç", 25.00m, "Tavuk, cheddar peyniri, yanında patates kızartması ve yeşillik ile servis edilir." });

            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Peynirli Sandviç", 20.00m, "Kaşar peyniri, domates ve marul ile hazırlanır. Yanında yeşillik ile servis edilir." });

            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Ton Balıklı Sandviç", 28.00m, "Ton balığı, mısır, marul ve özel sos ile hazırlanır. Hafif ve doyurucu bir seçenektir." });

            migrationBuilder.InsertData(
                table: "cafeteria_menu_items",
                columns: new[] { "Name", "Price", "Description", "ImageUrl", "IsAvailable" },
                values: new object[,]
                {
                    { "Penne Arabiata", 42.00m, "Domates sosu, sarımsak ve baharatlarla hazırlanmış penne makarna. Hafif acılı olarak servis edilir.", null, true },
                    { "Kremalı Makarna", 45.00m, "Kremalı sos, mantar ve baharatlarla hazırlanmış makarna. Üzerine parmesan serpilir.", null, true },
                    { "Kaşarlı Tost", 18.00m, "Bol kaşar peyniri ile hazırlanmış çıtır tost. Ketçap ve mayonez ile servis edilir.", null, true },
                    { "Karışık Tost", 24.00m, "Sucuk, kaşar peyniri ve domates ile hazırlanmış karışık tost. Sıcak servis edilir.", null, true },
                    { "Akdeniz Salata", 30.00m, "Marul, domates, salatalık, zeytin ve beyaz peynir ile hazırlanmış hafif Akdeniz salatası.", null, true },
                    { "Tavuklu Salata", 35.00m, "Izgara tavuk parçaları, mevsim yeşillikleri ve özel sos ile hazırlanır.", null, true },
                    { "Izgara Tavuk", 55.00m, "Izgara tavuk göğsü, pilav ve közlenmiş sebzeler ile servis edilir.", null, true },
                    { "Tavuk Şinitzel", 58.00m, "Pane harçlı tavuk şinitzel, patates kızartması ve salata ile servis edilir.", null, true },
                    { "Köfte", 65.00m, "Izgara köfte, pilav, domates ve biber eşliğinde servis edilir.", null, true },
                    { "Et Sote", 72.00m, "Dana eti, biber, soğan ve domates ile sotelenerek hazırlanır. Pilav ile servis edilir.", null, true },
                    { "Kola", 12.00m, "Soğuk servis edilen gazlı içecek.", null, true },
                    { "Ayran", 10.00m, "Geleneksel yoğurt içeceği, soğuk servis edilir.", null, true },
                    { "Su", 6.00m, "500 ml içme suyu.", null, true },
                    { "Çay", 8.00m, "Taze demlenmiş siyah çay.", null, true },
                    { "Kahve", 18.00m, "Sıcak servis edilen filtre kahve.", null, true },
                    { "Cheesecake", 28.00m, "Yumuşak kıvamlı cheesecake, meyve sosu ile servis edilir.", null, true },
                    { "Sufle", 32.00m, "Akışkan çikolatalı sufle. Sıcak servis edilir.", null, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Hamburger", 150.00m, "Klasik" });

            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Tost", 50.00m, "Kaşarlı" });

            migrationBuilder.UpdateData(
                table: "cafeteria_menu_items",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "Price", "Description" },
                values: new object[] { "Çay", 10.00m, "Taze" });

            migrationBuilder.Sql("DELETE FROM cafeteria_menu_items WHERE \"Id\" > 3;");
        }
    }
}
