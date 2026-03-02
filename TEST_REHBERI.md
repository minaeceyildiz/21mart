# Test Rehberi - Docker ile Çalıştırma

## Hızlı Başlangıç

### 1. Container'ları Başlatma
```bash
cd c:\Users\Sıla\OneDrive\Desktop\enson6.1\bitirmeYeni
docker-compose up -d
```

Bekleyin (container'lar başlayıp database hazır olana kadar ~30 saniye)

### 2. Uygulamayı Açın
- Frontend: http://localhost:3000
- API Swagger: http://localhost:5283/swagger

## Test Senaryosu 1: Yeni Kullanıcı Kaydı (Tam Akış)

### Adım 1: Kayıt Ol
1. http://localhost:3000 adresini açın
2. "Kayıt Ol" butonuna tıklayın
3. Formu doldurun:
   ```
   Kullanıcı Adı: testuser123
   Email: youremail@gmail.com  (kendi gerçek email adresiniz)
   Şifre: Test1234!
   Şifre Tekrar: Test1234!
   Rol: Öğrenci  (veya Akademik Personel)
   ```
4. "Kayıt Ol" butonuna tıklayın
5. ✅ Beklenen Sonuç: "Kayıt başarılı! Lütfen email adresinize gelen doğrulama linkine tıklayarak hesabınızı aktifleştirin."

### Adım 2: Email Doğrulama
1. Email kutunuzu kontrol edin (Gmail)
2. "Başkent Yaşam Platformu" göndereninden gelen emaili açın
3. "✓ E-postayı Doğrula" butonuna tıklayın
4. ✅ Beklenen Sonuç: "E-posta Başarıyla Doğrulandı!" sayfası açılır
5. "Giriş Yap" butonuna tıklayın

### Adım 3: Giriş Yapma
1. Giriş formunda bilgilerinizi girin:
   ```
   Kullanıcı Adı/Email: testuser123
   Şifre: Test1234!
   Rol: Öğrenci  (kayıt olurken seçtiğiniz rol)
   ```
2. "Giriş Yap" butonuna tıklayın
3. ✅ Beklenen Sonuç: Dashboard sayfasına yönlendirilirsiniz

## Test Senaryosu 2: Email Doğrulamadan Giriş Yapmaya Çalışma

### Adım 1: Yeni Kullanıcı Kaydı
1. Yeni bir kullanıcı oluşturun (Senaryo 1, Adım 1)
2. Email gönderildi mesajını görün

### Adım 2: Email Doğrulamadan Giriş Deneyin
1. Email'deki doğrulama linkine **TIKLMAYIN**
2. Doğrudan "Giriş Yap" sayfasına gidin
3. Kullanıcı adı ve şifrenizi girin
4. "Giriş Yap" butonuna tıklayın
5. ✅ Beklenen Sonuç: "Lütfen e-posta adresinizi doğrulayın. Kayıt sırasında gönderilen e-postadaki linke tıklayınız." hatası

## Test Senaryosu 3: Mevcut Doğrulanmış Kullanıcı ile Giriş

### Önceden Hazırlanmış Test Kullanıcısı
Veritabanında ID=5 olan kullanıcı manuel olarak doğrulanmıştır:
```
Kullanıcı Adı: silakutlu26
Email: silakutlu26@gmail.com
Rol: Öğrenci (muhtemelen)
Login Type: school_email (doğrulanmış)
```

Bu kullanıcı ile giriş yapabilirsiniz (şifresini biliyorsanız).

## Test Senaryosu 4: API Endpoint Testleri (Swagger)

### Adım 1: Swagger'ı Açın
http://localhost:5283/swagger

### Adım 2: Register Endpoint Testi
1. `POST /api/Auth/register` endpoint'ini genişletin
2. "Try it out" butonuna tıklayın
3. Request body'yi doldurun:
```json
{
  "username": "apitest123",
  "email": "youremail@gmail.com",
  "password": "Test1234!",
  "role": 0,
  "studentNo": null
}
```
4. "Execute" butonuna tıklayın
5. ✅ Beklenen Sonuç: Status 200, token boş, mesaj içerir

### Adım 3: Login Endpoint Testi (Doğrulanmamış Kullanıcı)
1. `POST /api/Auth/login` endpoint'ini genişletin
2. "Try it out" butonuna tıklayın
3. Request body:
```json
{
  "usernameOrEmail": "apitest123",
  "password": "Test1234!",
  "role": 0
}
```
4. "Execute" butonuna tıklayın
5. ✅ Beklenen Sonuç: Status 401, "Lütfen e-posta adresinizi doğrulayın" mesajı

## Veritabanı Kontrolleri

### Kullanıcıları Görüntüleme
```bash
docker exec baskent_db psql -U postgres -d oys3 -c "SELECT id, full_name, email, login_type FROM users;"
```

### Belirli Kullanıcının Detayları
```bash
docker exec baskent_db psql -U postgres -d oys3 -c "SELECT * FROM users WHERE email = 'youremail@gmail.com';"
```

### Login Type Enum Değerlerini Görme
```bash
docker exec baskent_db psql -U postgres -d oys3 -c "SELECT unnest(enum_range(NULL::login_type));"
```

Beklenen Çıktı:
```
    unnest    
--------------
 school_email
 staff_id
```

### Manuel Email Doğrulama (Test için)
⚠️ Sadece test amaçlı! Production'da kullanmayın!
```bash
docker exec baskent_db psql -U postgres -d oys3 -c "UPDATE users SET login_type = 'school_email'::login_type WHERE email = 'youremail@gmail.com';"
```

## Container Yönetimi

### Tüm Container'ları Görüntüleme
```bash
docker ps
```

Beklenen Çıktı:
```
baskent_web   (port 3000)
baskent_api   (port 5283)
baskent_db    (port 5432)
```

### Logları İzleme
```bash
# Tüm servisler
docker-compose logs -f

# Sadece API
docker logs -f baskent_api

# Sadece Frontend
docker logs -f baskent_web

# Son 50 satır
docker logs baskent_api --tail 50
```

### Container'ları Yeniden Başlatma
```bash
# Tümü
docker-compose restart

# Sadece API
docker-compose restart api

# Sadece Frontend
docker-compose restart web
```

### Container'ları Durdurma
```bash
docker-compose down
```

### Container'ları Silme (Veritabanı dahil!)
⚠️ **DİKKAT**: Tüm verileri siler!
```bash
docker-compose down -v
```

### Yeniden Build Etme
```bash
# Tüm servisler
docker-compose up -d --build

# Sadece API
docker-compose up -d --build api

# Sadece Frontend
docker-compose up -d --build web
```

## Sorun Giderme

### Sorun: "Port already in use" hatası

**Çözüm 1**: Mevcut container'ları durdurun
```bash
docker-compose down
docker ps -a  # Çalışan tüm container'ları göster
docker stop <container-id>  # Gerekirse manuel durdur
```

**Çözüm 2**: docker-compose.yml'de port numaralarını değiştirin
```yaml
ports:
  - "3001:80"     # Frontend için (3000 yerine 3001)
  - "5284:8080"   # API için (5283 yerine 5284)
```

### Sorun: API başlamıyor veya hata veriyor

**Kontrol 1**: Veritabanı hazır mı?
```bash
docker logs baskent_db --tail 20
```
"database system is ready to accept connections" mesajını görmelisiniz.

**Kontrol 2**: API loglarını inceleyin
```bash
docker logs baskent_api --tail 50
```

**Kontrol 3**: Migration sorunları
```bash
# Container'a bağlan ve migration durumunu kontrol et
docker exec baskent_api dotnet ef migrations list
```

### Sorun: Email gelmiyor

**Kontrol 1**: SMTP ayarları
`appsettings.json` dosyasında:
- Host: smtp.gmail.com
- Port: 587
- Email: yasambaskent@gmail.com
- Password: (App-specific password)

**Kontrol 2**: API loglarında email gönderimi
```bash
docker logs baskent_api 2>&1 | findstr "EMAIL"
```

Beklenen mesaj:
```
📧 EMAIL DOĞRULAMA MAİLİ GÖNDERİLİYOR
Doğrulama email'i gönderildi: youremail@gmail.com
```

**Kontrol 3**: Gmail spam klasörünü kontrol edin

**Kontrol 4**: İnternet bağlantınızı kontrol edin

### Sorun: "login_type" enum hatası

Bu sorun artık otomatik düzeltilebilir. API başlatıldığında otomatik olarak:
1. `login_type` kolonu yoksa oluşturur
2. Enum değerleri yanlışsa (`Student`, `Teacher`) düzeltir
3. Doğru değerleri (`school_email`, `staff_id`) kullanır

Manuel kontrol:
```bash
docker logs baskent_api 2>&1 | findstr "login_type"
```

Beklenen mesaj:
```
login_type enum kontrol edildi/düzeltildi.
```

### Sorun: Veritabanı bağlantı hatası

**Kontrol 1**: PostgreSQL çalışıyor mu?
```bash
docker exec baskent_db pg_isready -U postgres
```

**Kontrol 2**: Veritabanı var mı?
```bash
docker exec baskent_db psql -U postgres -l
```

`oys3` veritabanını görmelisiniz.

**Kontrol 3**: Bağlantı bilgileri
docker-compose.yml:
```yaml
ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=oys3;Username=postgres;Password=1234
```

### Sorun: Frontend API'ye bağlanamıyor

**Kontrol 1**: API çalışıyor mu?
```bash
curl http://localhost:5283/swagger
```

veya tarayıcıda: http://localhost:5283/swagger

**Kontrol 2**: CORS ayarları
Program.cs dosyasında:
```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "http://baskent_web",
    "http://web"
)
```

**Kontrol 3**: Nginx proxy yapılandırması
Frontend nginx.conf:
```nginx
location /api/ {
    proxy_pass http://api:8080/api/;
}
```

## Başarı Göstergeleri

### ✅ Sistem Düzgün Çalışıyor
- [ ] 3 container çalışıyor (web, api, db)
- [ ] http://localhost:3000 açılıyor
- [ ] http://localhost:5283/swagger açılıyor
- [ ] Kayıt olunca email geliyor
- [ ] Email'deki link çalışıyor
- [ ] Doğrulanmış kullanıcı giriş yapabiliyor
- [ ] Doğrulanmamış kullanıcı giriş yapamıyor (uyarı alıyor)

### ❌ Sorun Var
Yukarıdaki adımlardan biri çalışmıyorsa "Sorun Giderme" bölümüne bakın veya:
1. Container loglarını kontrol edin
2. Veritabanı bağlantısını test edin
3. Port numaralarını kontrol edin
4. Container'ları yeniden build edin

## Performans İpuçları

### Build Süresini Azaltma
Docker build cache kullanır. Değişiklik yapmadığınız layerlar cache'den gelir:
- Sadece kod değiştiyse: ~20 saniye
- Package'ler değiştiyse: ~2-3 dakika
- İlk build: ~5-10 dakika

### Cache Temizleme
Eğer build sorunları yaşıyorsanız:
```bash
docker builder prune -f
docker-compose build --no-cache
```

### Volume Boyutu
Veritabanı boyutunu kontrol etmek için:
```bash
docker system df -v
```

## Güvenlik Notları

### ⚠️ Production'a Geçmeden Önce Değiştirin:

1. **JWT Secret Key** (appsettings.json):
```json
"SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong12345"
```
Güçlü, rastgele bir key kullanın!

2. **Database Password** (docker-compose.yml):
```yaml
POSTGRES_PASSWORD: 1234
```
Güçlü bir şifre kullanın!

3. **SMTP Password**:
Environment variable'a taşıyın, appsettings.json'da tutmayın.

4. **HTTPS**:
Production'da mutlaka HTTPS kullanın ve SSL sertifikası ekleyin.

## İletişim ve Destek

Sorun yaşarsanız:
1. Bu dokümandaki sorun giderme adımlarını deneyin
2. Container loglarını kontrol edin
3. GitHub issue açın (varsa)
4. Ekip liderine danışın

## Ek Kaynaklar

- Docker Documentation: https://docs.docker.com/
- PostgreSQL Documentation: https://www.postgresql.org/docs/
- ASP.NET Core Documentation: https://docs.microsoft.com/en-us/aspnet/core/
- React Documentation: https://react.dev/

---

**Son Güncelleme**: 2 Mart 2026
**Docker Compose Versiyonu**: 3.8
**Test Edilen Platform**: Windows 11, Docker Desktop
