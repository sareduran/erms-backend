# ERMS Backend

OYAK Dijital temalı Çalışan Talep Yönetim Sistemi'nin bağımsız ASP.NET Core Web API deposudur. Dokümandaki dört katmanlı yapıyı uygular:

- `ERMS.Domain`: Varlıklar ve enum'lar; dış bağımlılığı yoktur.
- `ERMS.Application`: DTO'lar, servisler, iş kuralları ve port arayüzleri.
- `ERMS.Infrastructure`: EF Core, SQL Server, JWT/BCrypt ve yerel dosya saklama adaptörleri.
- `ERMS.Api`: Controller'lar, authentication, Swagger ve global hata middleware'i.

## Gereksinimler

- .NET 8 SDK
- Varsayılan geliştirme modu için ek veritabanı kurulumu gerekmez (SQLite).
- Üretim/SQL Server modu için SQL Server 2022 veya Docker Desktop.

## Çalıştırma

```powershell
$env:Jwt__Key = "<en-az-32-karakter-rastgele-bir-deger>"
$env:ERMS_DEMO_INITIAL_PASSWORD = "<sizin-belirleyeceginiz-gecici-parola>"
dotnet restore --configfile NuGet.Config
dotnet run --project src/ERMS.Api
```

SQL Server kullanmak için önce `docker compose up -d` çalıştırın ve `Database__Provider=SqlServer` ortam değişkenini verin. Varsayılan ayar, kurulumsuz yerel demo için `src/ERMS.Api/erms.db` SQLite dosyasını oluşturur. Üretim hedefi SQL Server ve EF migration'dır.

Swagger: `http://localhost:5082/swagger`

İlk açılışta migration uygulanır ve çalışan, iki seviyeli yönetici ve admin demo kullanıcıları oluşturulur. Geçici parola kaynak kodda tutulmaz; `ERMS_DEMO_INITIAL_PASSWORD` ortam değişkeninden alınır ve veritabanına yalnızca BCrypt hash'i yazılır. Demo e-posta adresleri `DatabaseSeeder` içinden görülebilir.

## Güvenlik notları

- Access token ömrü varsayılan 30 dakikadır.
- Refresh token istemciye bir kez döner; veritabanında yalnızca SHA-256 özeti tutulur ve yenilemede rotate edilir.
- Parolalar BCrypt work factor 12 ile hash'lenir.
- `Jwt__Key`, `ERMS_DEMO_INITIAL_PASSWORD` ve SQL Server bağlantısı kaynak koda yazılmamalı; secret/env değişkenlerinden verilmelidir.
- Yüklemeler 10 MB ile ve `pdf/doc/docx/xls/xlsx/png/jpg/jpeg` uzantılarıyla sınırlıdır.

## Ana uç noktalar

- `POST /api/auth/login`, `POST /api/auth/refresh`
- `GET/POST/PUT /api/requests`, `submit`, `cancel`, `comments`, `attachments`
- `GET /api/approvals/pending`, `approve`, `reject`
- `api/admin/users`, `departments`, `request-types`, `history`

Silme işlemleri fiziksel değildir; `IsActive=false` ile soft-delete uygulanır. Admin tüm talepleri ve audit loglarını görür; yönetici yalnızca doğrudan bağlı çalışanlarının taleplerini sonuçlandırabilir.

## Ayrı GitHub deposu

Bu klasör kendi başına depodur:

```powershell
cd backend
git init
git add .
git commit -m "feat: ERMS layered backend"
```
