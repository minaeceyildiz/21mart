# Projeyi Çalıştırma Kılavuzu

## Yöntem 1: Terminal'den Çalıştırma

Proje klasöründe terminal açın ve şu komutu çalıştırın:

```bash
dotnet run
```

Proje başladıktan sonra şu adreslerden erişebilirsiniz:
- **HTTP**: http://localhost:5283/swagger
- **HTTPS**: https://localhost:7275/swagger

## Yöntem 2: Visual Studio Code'dan

1. Visual Studio Code'da projeyi açın
2. `F5` tuşuna basın veya
3. Sol üstten "Run and Debug" butonuna tıklayın
4. ".NET Core" profilini seçin

## Yöntem 3: Visual Studio'dan

1. Visual Studio'da projeyi açın
2. `F5` tuşuna basın veya
3. Üst menüden "Start" butonuna tıklayın

## API Endpoint'leri

Proje çalıştıktan sonra Swagger UI'dan tüm endpoint'leri test edebilirsiniz:

### Randevu Endpoint'leri:
- `GET /api/appointment` - Tüm randevular
- `GET /api/appointment/{id}` - ID'ye göre randevu
- `GET /api/appointment/email/{email}` - E-posta'ya göre randevular
- `POST /api/appointment` - Yeni randevu oluştur
- `PUT /api/appointment/{id}` - Randevu güncelle
- `DELETE /api/appointment/{id}` - Randevu sil

### Bildirim Endpoint'leri:
- `GET /api/notification/email/{email}` - E-posta'ya göre bildirimler
- `GET /api/notification/email/{email}/unread` - Okunmamış bildirimler
- `PUT /api/notification/{id}/read` - Bildirimi okundu işaretle

## Notlar

- Projeyi durdurmak için terminal'de `Ctrl + C` tuşlarına basın
- İlk çalıştırmada tarayıcı otomatik açılabilir
- HTTPS için SSL sertifikası gerekebilir (ilk çalıştırmada tarayıcı uyarı verebilir)

