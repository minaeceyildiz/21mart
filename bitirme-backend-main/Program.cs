using ApiProject.Services;
using ApiProject.Data;
using ApiProject.Services.Background;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using ApiProject.Hubs;

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/error.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
    .CreateLogger();

try
{
    // PostgreSQL tarih formatı hatasını engellemek için:
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u kullan
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Bitirme Backend API",
            Version = "v1"
        });

        // JWT Token desteği için
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
    builder.Services.AddSignalR();

    // Entity Framework Core - PostgreSQL yapılandırması
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

    // JWT Authentication yapılandırması
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey bulunamadı.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // SignalR için JWT token desteği
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // SignalR hub istekleri için token'ı query string'den al
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Servisleri Dependency Injection'a ekle
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ICafeService, CafeService>();
    builder.Services.AddScoped<ScheduleService>();

    // Background Service
    builder.Services.AddHostedService<OrderCleanupService>();

    // CORS yapılandırması (Frontend bağlantısı için)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200", "http://localhost:8080")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // SignalR ve cookie desteği için
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<NotificationHub>("/notificationHub");

    // --- MIGRATION VE BAŞLANGIÇ VERİLERİNİ YÜKLEME KODU ---
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApiProject.Data.AppDbContext>();
            
            // Migration'ları otomatik uygula (arkadaşlarınız için)
            context.Database.Migrate();
            
            // request_reason kolonunu ekle (eğer yoksa) - async/await kullan
            try
            {
                var connection = context.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                if (!wasOpen)
                {
                    await connection.OpenAsync();
                }
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        DO $$ 
                        BEGIN
                            IF NOT EXISTS (
                                SELECT 1 
                                FROM information_schema.columns 
                                WHERE table_name = 'appointments' 
                                AND column_name = 'request_reason'
                            ) THEN
                                ALTER TABLE ""appointments"" 
                                ADD COLUMN ""request_reason"" TEXT;
                                RAISE NOTICE 'request_reason kolonu eklendi.';
                            END IF;
                        END $$;
                    ";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("request_reason kolonu kontrol edildi/eklendi.");
                }
                finally
                {
                    if (!wasOpen)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"request_reason kolonu eklenirken hata oluştu (zaten var olabilir): {ex.Message}");
            }
            
            // Veritabanını oluştur ve verileri bas
            ApiProject.Data.DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Veri yüklenirken hata oluştu: " + ex.Message);
        }
    }
    // -----------------------------------------

    Log.Information("Uygulama başlatılıyor...");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılırken kritik hata oluştu.");
}
finally
{
    Log.CloseAndFlush();
}
