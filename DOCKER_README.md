# Başkent Yaşam Platformu - Docker Kurulum Rehberi

## Gereksinimler

- Docker Desktop (Windows için)
- Docker Compose

## Kurulum Adımları

### 1. Projeyi Klonlayın veya İndirin

```bash
cd c:\Users\Sıla\OneDrive\Desktop\enson6.1\bitirmeYeni
```

### 2. Docker Container'ları Başlatın

Tüm servisleri (PostgreSQL, Backend API, Frontend) aynı anda başlatmak için:

```bash
docker-compose up --build
```

İlk çalıştırmada build işlemi biraz zaman alabilir (5-10 dakika).

### 3. Uygulamaya Erişim

- **Frontend (Web Arayüzü)**: http://localhost:3000
- **Backend API**: http://localhost:5283
- **Swagger (API Dokümantasyonu)**: http://localhost:5283/swagger
- **PostgreSQL Database**: localhost:5432

### 4. Veritabanı Bilgileri

- **Host**: localhost
- **Port**: 5432
- **Database**: oys3
- **Username**: postgres
- **Password**: 1234

## Kullanım

### Container'ları Durdurma

```bash
docker-compose down
```

### Container'ları Silme (Veritabanı dahil)

⚠️ **DİKKAT**: Bu komut veritabanındaki tüm verileri silecektir!

```bash
docker-compose down -v
```

### Sadece Backend'i Yeniden Build Etme

```bash
docker-compose up --build api
```

### Sadece Frontend'i Yeniden Build Etme

```bash
docker-compose up --build web
```

### Logları Görüntüleme

Tüm servislerin loglarını görmek için:

```bash
docker-compose logs -f
```

Sadece belirli bir servisin loglarını görmek için:

```bash
docker-compose logs -f api
docker-compose logs -f web
docker-compose logs -f db
```

## Sorun Giderme

### 1. Port Zaten Kullanımda Hatası

Eğer 3000, 5283 veya 5432 portları başka uygulamalar tarafından kullanılıyorsa, `docker-compose.yml` dosyasındaki port numaralarını değiştirebilirsiniz.

### 2. Container Başlatılamıyor

```bash
# Tüm container'ları temizle
docker-compose down

# Docker cache'ini temizle
docker system prune -a

# Yeniden başlat
docker-compose up --build
```

### 3. Veritabanı Bağlantı Hatası

Backend container'ı başlatıldığında veritabanının hazır olmasını bekler. Eğer sorun yaşıyorsanız:

```bash
# Container'ları yeniden başlat
docker-compose restart
```

### 4. Email Doğrulama Çalışmıyor

Email doğrulama linkleri `http://localhost:5283/api/auth/verify-email` adresine gider. Eğer farklı bir domain kullanıyorsanız, `docker-compose.yml` dosyasındaki `APP_BASE_URL` değişkenini güncelleyin.

## Geliştirme Notları

### Environment Variables

`docker-compose.yml` dosyasında tanımlanan environment variables:

- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ConnectionStrings__DefaultConnection`: PostgreSQL bağlantı dizesi
- `DisableHttpsRedirection`: Docker'da HTTPS yönlendirmesini devre dışı bırakır
- `APP_BASE_URL`: Email doğrulama linkleri için backend URL'i
- `FRONTEND_URL`: Email doğrulama sonrası yönlendirme için frontend URL'i

### Veritabanı Kalıcılığı

PostgreSQL verileri `pgdata` adlı Docker volume'unda saklanır. Container'lar silinse bile veriler korunur. Verileri tamamen silmek için:

```bash
docker-compose down -v
```

### Backend Logları

Backend logları Docker volume'unda saklanır ve container içinde `/app/logs` dizininde bulunur.

## Üretim (Production) Ortamına Geçiş

Üretim ortamında kullanmak için:

1. `docker-compose.yml` dosyasında `ASPNETCORE_ENVIRONMENT` değerini `Production` yapın
2. `APP_BASE_URL` ve `FRONTEND_URL` değerlerini gerçek domain adreslerinizle değiştirin
3. `appsettings.json` dosyasında güvenlik ayarlarını (JWT SecretKey, SMTP şifresi) güncelleyin
4. HTTPS sertifikası yapılandırın

## Destek

Sorun yaşarsanız:

1. Container loglarını kontrol edin: `docker-compose logs -f`
2. Container durumunu kontrol edin: `docker-compose ps`
3. Veritabanı bağlantısını test edin: `docker-compose exec db psql -U postgres -d oys3`
