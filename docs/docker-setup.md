# Başkent Yaşam Platformu - Docker Kurulum Rehberi

## Gereksinimler

- Docker Desktop
- Git

## Hızlı Başlangıç

```bash
# 1. Projeyi klonlayın
git clone <repo-url>
cd baskent-yasam

# 2. Docker ile başlatın
docker compose up --build

# 3. Tarayıcıda açın
# http://localhost:3000
```

## Kurulum Adımları

### 1. Projeyi Klonlayın

```bash
git clone <repo-url>
cd baskent-yasam
```

### 2. Docker Container'ları Başlatın

Tüm servisleri (PostgreSQL, Backend API, Frontend) aynı anda başlatmak için:

```bash
docker compose up --build
```

İlk çalıştırmada build işlemi birkaç dakika sürebilir.

### 3. Uygulamaya Erişim

| Servis | URL |
|--------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5283 |
| Swagger | http://localhost:5283/swagger |
| PostgreSQL | localhost:5433 |

### 4. Veritabanı Bilgileri

- **Host**: localhost
- **Port**: 5433
- **Database**: oys3
- **Username**: postgres
- **Password**: 1234

## Kullanım

### Container'ları Durdurma

```bash
docker compose down
```

### Container'ları Silme (Veritabanı dahil)

⚠️ **DİKKAT**: Bu komut veritabanındaki tüm verileri silecektir!

```bash
docker compose down -v
```

### Sadece Backend'i Yeniden Build Etme

```bash
docker compose up --build api
```

### Sadece Frontend'i Yeniden Build Etme

```bash
docker compose up --build web
```

### Logları Görüntüleme

```bash
# Tüm servisler
docker compose logs -f

# Sadece API
docker compose logs -f api

# Sadece Frontend
docker compose logs -f web

# Sadece Database
docker compose logs -f db
```

## Test Kullanıcıları

| Kullanıcı | Email | Şifre | Rol |
|-----------|-------|-------|-----|
| Ali Ogrenci | ali.ogrenci@baskent.edu.tr | baskent123 | Öğrenci |
| Mehmet Hoca | hoca@baskent.edu.tr | baskent123 | Akademik Personel |
| Admin | admin@baskent.edu.tr | admin123 | Admin |

## Sorun Giderme

### Port Zaten Kullanımda Hatası

Eğer 3000, 5283 veya 5433 portları başka uygulamalar tarafından kullanılıyorsa:

```bash
# Hangi uygulama portu kullanıyor?
lsof -i :3000

# Veya docker-compose.yml'de portları değiştirin
```

### Container Başlatılamıyor

```bash
# Tüm container'ları temizle
docker compose down

# Docker cache'ini temizle
docker system prune -a

# Yeniden başlat
docker compose up --build
```

### Veritabanı Bağlantı Hatası

```bash
# Container'ları yeniden başlat
docker compose restart

# Veritabanı durumunu kontrol et
docker compose exec db pg_isready -U postgres
```

### Email Doğrulama Çalışmıyor

SMTP ayarları `docker-compose.yml` dosyasında tanımlı. Sorun yaşarsanız logları kontrol edin:

```bash
docker compose logs api | grep -i email
```

## Environment Variables

`docker-compose.yml` dosyasında tanımlanan değişkenler:

| Değişken | Açıklama |
|----------|----------|
| `ASPNETCORE_ENVIRONMENT` | Development/Production |
| `ConnectionStrings__DefaultConnection` | PostgreSQL bağlantı dizesi |
| `DisableHttpsRedirection` | Docker'da HTTPS yönlendirmesini devre dışı bırakır |
| `APP_BASE_URL` | Email doğrulama linkleri için backend URL'i |
| `FRONTEND_URL` | Email doğrulama sonrası yönlendirme için frontend URL'i |

## Veritabanı Kalıcılığı

PostgreSQL verileri `pgdata` Docker volume'unda saklanır. Container'lar silinse bile veriler korunur.

Verileri tamamen silmek için:

```bash
docker compose down -v
```

## Destek

Sorun yaşarsanız:

1. Container loglarını kontrol edin: `docker compose logs -f`
2. Container durumunu kontrol edin: `docker compose ps`
3. Veritabanına bağlanın: `docker compose exec db psql -U postgres -d oys3`
