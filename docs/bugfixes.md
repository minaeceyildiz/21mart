# Docker Düzeltmeleri - Özet

## Yapılan Değişiklikler

### 1. Backend (API) Düzeltmeleri

#### Program.cs
- **CORS Yapılandırması**: Docker container adlarını (`baskent_web`, `web`) CORS origin listesine eklendi
- **HTTPS Yönlendirmesi**: Docker ortamında HTTPS yönlendirmesini devre dışı bırakmak için `DisableHttpsRedirection` environment variable desteği eklendi

#### EmailService.cs
- **Email Doğrulama URL'leri**: Email doğrulama linklerinin backend API URL'ini `APP_BASE_URL` environment variable'dan alması sağlandı
- Böylece Docker ve local ortamda farklı URL'ler kullanılabilir

#### AuthController.cs
- **Frontend URL'leri**: Email doğrulama success/error sayfalarındaki frontend yönlendirme linklerinin `FRONTEND_URL` environment variable'dan alması sağlandı
- HTML response'larda hardcoded `localhost:3000` yerine dinamik URL kullanımı

#### appsettings.json
- Email BaseUrl'leri `localhost:3000` olarak güncellendi (email doğrulama sayfaları için)

### 2. Docker Yapılandırması

#### docker-compose.yml
- **Environment Variables**: API container'ına şu değişkenler eklendi:
  - `DisableHttpsRedirection: "true"` - HTTPS yönlendirmesini kapat
  - `APP_BASE_URL: "http://localhost:5283"` - Backend API URL'i (email linkleri için)
  - `FRONTEND_URL: "http://localhost:3000"` - Frontend URL'i (yönlendirmeler için)
- **Volumes**: API logları için `api_logs` volume'u eklendi

#### nginx.conf (Frontend)
- **WebSocket Desteği**: SignalR için WebSocket proxy ayarları eklendi
- **Gelişmiş Proxy Headers**: `X-Forwarded-For`, `X-Forwarded-Proto`, `Connection upgrade` header'ları eklendi
- **SignalR Hub Route**: `/notificationHub` endpoint'i için özel proxy yapılandırması

### 3. Yeni Dosyalar

#### .dockerignore Dosyaları
- **Backend**: `bin`, `obj`, `logs`, `.vs`, `.vscode` klasörlerini ignore ediyor
- **Frontend**: `node_modules`, `.git`, `.env.local` gibi gereksiz dosyaları ignore ediyor

#### DOCKER_README.md
- Docker kullanım rehberi
- Kurulum adımları
- Sorun giderme ipuçları
- Environment variable açıklamaları

## Çözülen Sorunlar

### 1. ✅ Kayıt Olma (Register)
- Email doğrulama linkleri artık doğru URL'leri kullanıyor
- Email gönderimi çalışıyor

### 2. ✅ Email Doğrulama
- Doğrulama linklerine tıklandığında backend API'ye gidiyor
- `login_type` ENUM değeri `school_email` olarak doğru şekilde güncelleniyor
- Doğrulama sonrası frontend'e yönlendirme çalışıyor

### 3. ✅ Giriş Yapma (Login)
- Email doğrulama kontrolü çalışıyor
- Doğrulanmamış kullanıcılar giriş yapamıyor (uyarı mesajı gösteriliyor)
- Doğrulanmış kullanıcılar başarıyla giriş yapabiliyor
- JWT token oluşturma ve doğrulama çalışıyor

### 4. ✅ CORS Sorunları
- Frontend (nginx container) -> Backend API iletişimi çalışıyor
- SignalR WebSocket bağlantıları çalışıyor

### 5. ✅ Database Migration
- Container başlatıldığında migration'lar otomatik uygulanıyor
- Veritabanı şeması doğru şekilde oluşturuluyor
- PostgreSQL connection pool ve retry mekanizması çalışıyor

## Test Adımları

### 1. Container'ları Başlatma
```bash
cd c:\Users\Sıla\OneDrive\Desktop\enson6.1\bitirmeYeni
docker-compose up -d
```

### 2. Servis Durumlarını Kontrol
```bash
docker ps
docker logs baskent_api --tail 50
docker logs baskent_web --tail 20
docker logs baskent_db --tail 20
```

### 3. Uygulamayı Test Etme

#### A. Frontend'i Açın
Tarayıcıda: http://localhost:3000

#### B. Kayıt Olma Testi
1. "Kayıt Ol" butonuna tıklayın
2. Formu doldurun:
   - Kullanıcı adı (öğrenci ise rakamla başlayan, akademisyen ise harfle başlayan)
   - Email (Gmail hesabınız)
   - Şifre
   - Rol seçin (Öğrenci/Akademik Personel)
3. "Kayıt Ol" butonuna tıklayın
4. Email gönderildi mesajını görmelisiniz
5. Email kutunuzu kontrol edin (yasambaskent@gmail.com'dan gelen email)
6. Email'deki "E-postayı Doğrula" butonuna tıklayın
7. Doğrulama başarılı sayfasını görmelisiniz

#### C. Giriş Yapma Testi
1. Frontend'de "Giriş Yap" sayfasına gidin
2. Kullanıcı adı/email ve şifrenizi girin
3. Rol seçin (kayıt olurken seçtiğiniz rol)
4. "Giriş Yap" butonuna tıklayın
5. Başarıyla giriş yapmalısınız

### 4. API Test (Swagger)
Tarayıcıda: http://localhost:5283/swagger

- `/api/Auth/register` endpoint'ini test edin
- `/api/Auth/login` endpoint'ini test edin

### 5. Database Bağlantısı Test
```bash
docker exec -it baskent_db psql -U postgres -d oys3 -c "SELECT id, name, email, login_type FROM users;"
```

## Önemli Notlar

### Email Ayarları
- Gmail SMTP kullanılıyor (`smtp.gmail.com:587`)
- App-specific password kullanılmalı (2FA aktifse)
- Email gönderimi için internet bağlantısı gerekli

### Veritabanı
- PostgreSQL 16 Alpine kullanılıyor
- Veritabanı adı: `oys3`
- Kullanıcı: `postgres`
- Şifre: `1234`
- Port: `5432`
- Veriler `pgdata` volume'unda kalıcı olarak saklanıyor

### Port Yapılandırması
- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5283`
- Swagger: `http://localhost:5283/swagger`
- Database: `localhost:5432`

### Environment Variables (docker-compose.yml)
```yaml
ASPNETCORE_ENVIRONMENT: Development
ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=oys3;Username=postgres;Password=1234
DisableHttpsRedirection: "true"
APP_BASE_URL: "http://localhost:5283"
FRONTEND_URL: "http://localhost:3000"
```

## Sorun Giderme

### Container Loglarını İzleme
```bash
# Tüm container'ların loglarını canlı izleme
docker-compose logs -f

# Sadece API loglarını izleme
docker logs -f baskent_api

# Sadece Frontend loglarını izleme
docker logs -f baskent_web
```

### Container'ları Yeniden Başlatma
```bash
# Sadece API'yi yeniden başlat
docker-compose restart api

# Tüm servisleri yeniden başlat
docker-compose restart

# Container'ları durdur ve yeniden başlat
docker-compose down
docker-compose up -d
```

### Veritabanını Sıfırlama
⚠️ **DİKKAT**: Tüm verileri siler!
```bash
docker-compose down -v
docker-compose up -d
```

### Build Cache'ini Temizleme
```bash
docker builder prune -f
docker-compose build --no-cache
docker-compose up -d
```

## Arkadaşlarınızla Paylaşım

Projenizi arkadaşlarınızla paylaşmak için:

1. **Git ile paylaşım**:
```bash
git add .
git commit -m "Docker yapılandırması tamamlandı - giriş sorunu düzeltildi"
git push
```

2. **Arkadaşlarınız projeyi klonladıktan sonra**:
```bash
cd proje-klasoru
docker-compose up -d
```

3. **İlk kullanımda**:
- Container'lar otomatik olarak build edilecek
- Database migration'ları otomatik uygulanacak
- Test verileri otomatik yüklenecek

## Üretim (Production) Ortamına Geçiş

Production'da kullanmak için değiştirilmesi gerekenler:

1. **Environment Variables**:
   - `APP_BASE_URL`: Gerçek domain adresiniz (örn: `https://api.baskent.edu.tr`)
   - `FRONTEND_URL`: Frontend domain'iniz (örn: `https://baskent.edu.tr`)
   - `ASPNETCORE_ENVIRONMENT`: `Production`

2. **Güvenlik**:
   - `appsettings.json` içindeki JWT SecretKey'i değiştirin
   - Database şifresini güçlü bir şifreye değiştirin
   - SMTP şifresini environment variable'a taşıyın

3. **HTTPS**:
   - SSL sertifikası ekleyin
   - `DisableHttpsRedirection: "false"` yapın veya kaldırın
   - Nginx'te HTTPS yapılandırması yapın

4. **Performance**:
   - Database connection pool ayarlarını optimize edin
   - Log seviyesini ayarlayın
   - Resource limit'leri belirleyin

## Özet

✅ **Tüm sorunlar çözüldü:**
- Email gönderimi çalışıyor
- Email doğrulama çalışıyor
- Giriş sistemi çalışıyor
- Docker container'ları düzgün çalışıyor
- Database migration'ları otomatik uygulanıyor
- CORS sorunları çözüldü
- SignalR WebSocket desteği eklendi

🎉 **Artık arkadaşlarınız da projeyi çalıştırabilir!**
