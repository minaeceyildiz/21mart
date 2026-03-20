using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApiProject.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string name, string token, int userId);
    Task SendPasswordResetEmailAsync(string email, string name, string resetLink, int validMinutes);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string name, string token, int userId)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var smtpHost = smtpSettings["Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
            var smtpUsername = smtpSettings["Username"] ?? "";
            var smtpPassword = smtpSettings["Password"] ?? "";
            var smtpFromEmail = smtpSettings["FromEmail"] ?? smtpUsername;
            var smtpFromName = smtpSettings["FromName"] ?? "Başkent Üniversitesi";
            
            // BaseUrl'i environment variable'dan al (Docker için), yoksa config'den
            var baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") 
                       ?? smtpSettings["BaseUrl"] 
                       ?? "http://localhost:5283";

            // Doğrulama linki oluştur - Backend API endpoint'ine gider
            var verificationLink = $"{baseUrl}/api/auth/verify-email?token={token}&userId={userId}";

            // Email içeriği
            var subject = "E-posta Doğrulama - Başkent Üniversitesi Yaşam Platformu";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
                        <tr>
                            <td align='center'>
                                <table width='600' cellpadding='0' cellspacing='0' style='background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='background: linear-gradient(135deg, #d71920 0%, #a01417 100%); padding: 40px; text-align: center;'>
                                            <h1 style='color: white; margin: 0; font-size: 28px;'>Başkent Üniversitesi</h1>
                                            <p style='color: #ffd; margin: 10px 0 0 0; font-size: 14px;'>Yaşam Platformu</p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Content -->
                                    <tr>
                                        <td style='padding: 40px 30px;'>
                                            <h2 style='color: #333; margin: 0 0 20px 0; font-size: 24px;'>Hoş Geldiniz, {name}!</h2>
                                            <p style='color: #666; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;'>
                                                Başkent Yaşam platformuna kayıt olduğunuz için teşekkür ederiz. 
                                                Hesabınızı aktifleştirmek için aşağıdaki butona tıklamanız yeterli.
                                            </p>
                                            
                                            <!-- Button -->
                                            <table width='100%' cellpadding='0' cellspacing='0'>
                                                <tr>
                                                    <td align='center' style='padding: 20px 0;'>
                                                        <a href='{verificationLink}' 
                                                           style='background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); 
                                                                  color: white; 
                                                                  padding: 15px 40px; 
                                                                  text-decoration: none; 
                                                                  border-radius: 5px; 
                                                                  font-weight: bold; 
                                                                  font-size: 16px;
                                                                  display: inline-block;
                                                                  box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                                                            ✓ E-postayı Doğrula
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>
                                            
                                            <!-- Alternative Link -->
                                            <div style='background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                                <p style='color: #666; margin: 0 0 10px 0; font-size: 14px;'>
                                                    <strong>Buton çalışmıyorsa</strong> aşağıdaki linki tarayıcınıza kopyalayıp yapıştırın:
                                                </p>
                                                <p style='color: #4CAF50; word-break: break-all; margin: 0; font-size: 12px;'>
                                                    {verificationLink}
                                                </p>
                                            </div>
                                            
                                            <!-- Info Box -->
                                            <div style='border-left: 4px solid #ff9800; padding: 15px; margin: 20px 0; background-color: #fff8e1;'>
                                                <p style='color: #f57c00; margin: 0; font-size: 14px;'>
                                                    <strong>⚠️ Önemli:</strong> Bu doğrulama linki 24 saat geçerlidir.
                                                </p>
                                            </div>
                                            
                                            <p style='color: #999; font-size: 14px; line-height: 1.6; margin: 20px 0 0 0;'>
                                                Eğer bu kaydı siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.
                                            </p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Footer -->
                                    <tr>
                                        <td style='background-color: #f9f9f9; padding: 20px 30px; text-align: center; border-top: 1px solid #eee;'>
                                            <p style='color: #999; font-size: 12px; margin: 0;'>
                                                Bu otomatik bir e-postadır, lütfen yanıtlamayın.
                                            </p>
                                            <p style='color: #999; font-size: 12px; margin: 10px 0 0 0;'>
                                                © 2026 Başkent Üniversitesi - Tüm hakları saklıdır.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
            ";

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(smtpFromEmail, smtpFromName);
                    message.To.Add(new MailAddress(email, name));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;
                    message.BodyEncoding = System.Text.Encoding.UTF8;
                    message.SubjectEncoding = System.Text.Encoding.UTF8;

                    // Email gönderilmeden önce içeriği console'a yazdır (development için)
                    Console.WriteLine("\n==========================================");
                    Console.WriteLine("📧 EMAIL DOĞRULAMA MAİLİ GÖNDERİLİYOR");
                    Console.WriteLine("==========================================");
                    Console.WriteLine($"To: {email}");
                    Console.WriteLine($"Subject: {subject}");
                    Console.WriteLine($"\nDoğrulama Linki:");
                    Console.WriteLine($"{verificationLink}");
                    Console.WriteLine("\n==========================================\n");

                    await client.SendMailAsync(message);
                    _logger.LogInformation($"Doğrulama email'i gönderildi: {email}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Email gönderilirken hata oluştu: {email}");
            throw new InvalidOperationException("Email gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.", ex);
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string name, string resetLink, int validMinutes)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var smtpHost = smtpSettings["Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
            var smtpUsername = smtpSettings["Username"] ?? "";
            var smtpPassword = smtpSettings["Password"] ?? "";
            var smtpFromEmail = smtpSettings["FromEmail"] ?? smtpUsername;
            var smtpFromName = smtpSettings["FromName"] ?? "Başkent Üniversitesi";

            var subject = "Şifre Sıfırlama - Başkent Yaşam";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
                <body style='margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f4f4f4;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4;padding:20px;'>
                        <tr><td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' style='background-color:white;border-radius:10px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
                                <tr>
                                    <td style='background:linear-gradient(135deg,#d71920 0%,#a01417 100%);padding:32px;text-align:center;'>
                                        <h1 style='color:white;margin:0;font-size:24px;'>Başkent Yaşam</h1>
                                        <p style='color:#ffd;margin:8px 0 0;font-size:13px;'>Şifre sıfırlama talebi</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:32px 28px;'>
                                        <p style='color:#333;margin:0 0 16px;font-size:16px;'>Merhaba <strong>{WebUtility.HtmlEncode(name)}</strong>,</p>
                                        <p style='color:#666;line-height:1.6;margin:0 0 20px;font-size:15px;'>
                                            Hesabınız için şifre sıfırlama isteği alındı. Yeni şifre belirlemek için aşağıdaki butona tıklayın.
                                        </p>
                                        <table width='100%' cellpadding='0' cellspacing='0'><tr><td align='center' style='padding:16px 0;'>
                                            <a href='{WebUtility.HtmlEncode(resetLink)}' style='background:#d71920;color:white;padding:14px 36px;text-decoration:none;border-radius:6px;font-weight:bold;font-size:15px;display:inline-block;'>
                                                Şifremi sıfırla
                                            </a>
                                        </td></tr></table>
                                        <div style='background:#f9f9f9;padding:16px;border-radius:6px;margin:20px 0;'>
                                            <p style='color:#666;margin:0 0 8px;font-size:13px;'><strong>Link çalışmıyorsa</strong> adresi kopyalayın:</p>
                                            <p style='color:#d71920;word-break:break-all;margin:0;font-size:12px;'>{WebUtility.HtmlEncode(resetLink)}</p>
                                        </div>
                                        <div style='border-left:4px solid #ff9800;padding:12px 14px;background:#fff8e1;margin-top:16px;'>
                                            <p style='color:#e65100;margin:0;font-size:13px;'>
                                                <strong>Önemli:</strong> Bu bağlantı yaklaşık <strong>{validMinutes} dakika</strong> geçerlidir. Süre dolunca yeni talep oluşturmanız gerekir.
                                            </p>
                                        </div>
                                        <p style='color:#999;font-size:13px;margin-top:20px;'>
                                            Bu isteği siz yapmadıysanız bu e-postayı yok sayın; şifreniz değişmez.
                                        </p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='background:#f9f9f9;padding:16px 28px;text-align:center;border-top:1px solid #eee;'>
                                        <p style='color:#999;font-size:11px;margin:0;'>Bu otomatik bir e-postadır, lütfen yanıtlamayın.</p>
                                    </td>
                                </tr>
                            </table>
                        </td></tr>
                    </table>
                </body>
                </html>";

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            using var message = new MailMessage();
            message.From = new MailAddress(smtpFromEmail, smtpFromName);
            message.To.Add(new MailAddress(email, name));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.SubjectEncoding = System.Text.Encoding.UTF8;

            _logger.LogInformation("Şifre sıfırlama e-postası gönderiliyor: {Email}", email);
            await client.SendMailAsync(message);
            _logger.LogInformation("Şifre sıfırlama e-postası gönderildi: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi: {Email}", email);
            throw new InvalidOperationException("E-posta gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.", ex);
        }
    }
}
