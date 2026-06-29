# ExamAI — Full Task Breakdown
## גודל: XS = 1–2 שעות | S = 3–5 שעות | M = 6–8 שעות
## Layer: BE = Backend | FE = Frontend | INFRA = Infrastructure | DB = Database

---

# ══════════════════════════════════════════
# EPIC 0 — INFRASTRUCTURE & PROJECT SETUP
# ══════════════════════════════════════════

---

## T-001 | XS | INFRA
### כותרת: Create GitHub Monorepo Structure
**תלויות:** אין
**Layer:** INFRA

**מה לעשות בדיוק:**
- צור repository חדש בשם `examai`
- צור את מבנה התיקיות הבא:
  ```
  examai/
  ├── src/
  │   ├── backend/
  │   │   ├── ExamAI.sln
  │   │   ├── ExamAI.Identity.API/
  │   │   ├── ExamAI.Exam.API/
  │   │   ├── ExamAI.Billing.API/
  │   │   ├── ExamAI.Grading.API/
  │   │   ├── ExamAI.Analytics.API/
  │   │   ├── ExamAI.Admin.API/
  │   │   ├── ExamAI.OCR.Worker/
  │   │   ├── ExamAI.Grading.Worker/
  │   │   ├── ExamAI.Export.Worker/
  │   │   ├── ExamAI.Notification.Worker/
  │   │   └── ExamAI.Shared/
  │   └── frontend/
  │       └── examai-web/
  ├── infra/
  │   ├── docker/
  │   ├── k8s/
  │   └── scripts/
  ├── docs/
  ├── .github/workflows/
  ├── .gitignore
  └── README.md
  ```
- הוסף `.gitignore` עם node_modules, bin, obj, .env, *.user
- הוסף `README.md` בסיסי עם תיאור הפרויקט

**קריטריוני קבלה:**
- [ ] Repo קיים ב-GitHub
- [ ] מבנה תיקיות מלא
- [ ] `.gitignore` תקין
- [ ] Branch ראשי: `main`, branch לפיתוח: `develop`

---

## T-002 | S | INFRA
### כותרת: Docker Compose for Local Development Environment
**תלויות:** T-001
**Layer:** INFRA

**מה לעשות בדיוק:**
- צור קובץ `infra/docker/docker-compose.yml` עם כל ה-services הבאים:
  - **PostgreSQL 16**: port 5432, volumes: `pg_data:/var/lib/postgresql/data`, env: POSTGRES_USER=examai, POSTGRES_PASSWORD=examai_local, POSTGRES_DB=examai
  - **Redis 7.2**: port 6379, volumes: `redis_data:/data`, enable AOF persistence
  - **RabbitMQ 3.13 management**: ports 5672 + 15672 (management UI), volumes: `rabbit_data:/var/lib/rabbitmq`, env: RABBITMQ_DEFAULT_USER=examai, RABBITMQ_DEFAULT_PASS=examai_local
  - **MinIO** (S3 compatible): ports 9000 (API) + 9001 (console), volumes: `minio_data:/data`, env: MINIO_ROOT_USER=examai, MINIO_ROOT_PASSWORD=examai_local
  - **Elasticsearch 8**: port 9200, env: xpack.security.enabled=false, discovery.type=single-node, volumes: `es_data:/usr/share/elasticsearch/data`
  - **Seq** (log viewer): port 5341, volumes: `seq_data:/data`
  - **ClamAV**: daemon mode, TCP socket 3310
- צור קובץ `infra/docker/.env.local` עם כל המשתנים
- צור קובץ `infra/scripts/init-local.sh`:
  - מריץ `docker-compose up -d`
  - מחכה ל-PostgreSQL להיות ready
  - מריץ migrations ראשוניות
  - יוצר MinIO bucket בשם `examai-files`
  - מדפיס URLs של כל השירותים

**קריטריוני קבלה:**
- [ ] `docker-compose up -d` עולה ללא שגיאות
- [ ] כל ה-ports מגיבים
- [ ] MinIO bucket נוצר אוטומטית
- [ ] RabbitMQ Management UI נגיש ב-localhost:15672
- [ ] Seq נגיש ב-localhost:5341

---

## T-003 | S | DB
### כותרת: Create .NET 9 Solution and Shared Project Structure
**תלויות:** T-001
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.sln` עם כל ה-projects
- צור project **ExamAI.Shared** (class library):
  - `Domain/` — base classes: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`
  - `Application/` — `IRepository<T>`, `IUnitOfWork`, `ICurrentUser`, `Result<T>` (success/failure pattern)
  - `Infrastructure/` — `BaseDbContext`, `AuditInterceptor`, `RabbitMqPublisher`
  - `Middleware/` — `ExceptionHandlingMiddleware`, `RequestLoggingMiddleware`
  - `Extensions/` — `ServiceCollectionExtensions` (register shared services)
- הוסף NuGet packages לכל project:
  - Microsoft.EntityFrameworkCore (9.x)
  - Npgsql.EntityFrameworkCore.PostgreSQL
  - Serilog.AspNetCore
  - MediatR
  - FluentValidation
  - RabbitMQ.Client
  - StackExchange.Redis
  - AWSSDK.S3
  - Microsoft.Extensions.Caching.StackExchangeRedis
- צור `Directory.Build.props` עם shared package versions
- צור `Directory.Packages.props` עם central package management

**קריטריוני קבלה:**
- [ ] `dotnet build ExamAI.sln` עובר ללא שגיאות
- [ ] כל projects ב-sln
- [ ] Shared base classes קיימים
- [ ] NuGet packages מוגדרים ב-central management

---

## T-004 | S | DB
### כותרת: PostgreSQL — Run All Schema Migrations (EF Core)
**תלויות:** T-002, T-003
**Layer:** DB

**מה לעשות בדיוק:**
- בכל API project, צור `Migrations/` folder עם EF Core
- צור `DbContext` per bounded context:
  - `IdentityDbContext` — schema `identity`
  - `ExamDbContext` — schema `exam`
  - `OcrDbContext` — schema `ocr`
  - `GradingDbContext` — schema `grading`
  - `BillingDbContext` — schema `billing`
  - `ExportDbContext` — schema `export`
  - `AnalyticsDbContext` — schema `analytics`
  - `AuditDbContext` — schema `audit`
- לכל DbContext הגדר `HasDefaultSchema(schemaName)`
- הוסף `AuditSaveChangesInterceptor` שמוסיף ל-`audit.audit_logs` בכל שינוי
- צור `initial_migration` לכל context עם כל הטבלאות לפי הסכמה מ-Part 1 של ה-design
- הוסף לכל טבלה indexes כמוגדר בסכמה
- צור `DatabaseSeeder` שמריץ את seed data: roles, plans

**קריטריוני קבלה:**
- [ ] `dotnet ef database update` מריץ ללא שגיאות
- [ ] כל הטבלאות נוצרות
- [ ] Seed data קיים: roles (teacher, admin, super_admin), plans (free, pay_as_you_go)
- [ ] Indexes נוצרים
- [ ] Schema separation תקין

---

## T-005 | S | INFRA
### כותרת: RabbitMQ — Define Exchanges, Queues and Routing Keys
**תלויות:** T-002
**Layer:** INFRA / BE

**מה לעשות בדיוק:**
- צור קובץ `infra/rabbitmq/topology.json` שמגדיר את כל ה-topology
- צור `RabbitMqTopologyInitializer` ב-ExamAI.Shared שמריץ בעת startup:
  - **Exchange:** `examai.events` (type: topic, durable: true)
  - **Exchange:** `examai.jobs` (type: direct, durable: true)
  - **Exchange:** `examai.dlx` (type: direct, durable: true) — Dead Letter Exchange
  - **Queues ורוטינג:**
    - Queue: `ocr.jobs` → routing key: `job.ocr`, x-dead-letter-exchange: `examai.dlx`
    - Queue: `grading.jobs` → routing key: `job.grading`
    - Queue: `export.jobs` → routing key: `job.export`
    - Queue: `notification.jobs` → routing key: `job.notification`
    - Queue: `ocr.jobs.dlq` → dead letter queue
    - Queue: `grading.jobs.dlq` → dead letter queue
  - כל queue: durable: true, x-message-ttl: 3600000 (1 שעה)
- צור `IMessagePublisher` interface ו-`RabbitMqMessagePublisher` implementation ב-Shared
- צור `IMessageConsumer<T>` base class לכל workers

**קריטריוני קבלה:**
- [ ] כל exchanges וqueues נוצרים ב-startup
- [ ] Messages נשלחים ומתקבלים בין producers ו-consumers
- [ ] Dead letter queue פועל כשצריך retry
- [ ] RabbitMQ Management UI מראה את ה-topology

---

## T-006 | XS | INFRA
### כותרת: Configure Serilog Structured Logging with Seq
**תלויות:** T-003
**Layer:** BE

**מה לעשות בדיוק:**
- בכל API/Worker project, הגדר Serilog ב-`Program.cs`:
  ```csharp
  builder.Host.UseSerilog((ctx, lc) => lc
      .ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithEnvironmentName()
      .Enrich.WithProperty("Application", "ExamAI.{ServiceName}")
      .WriteTo.Console(new JsonFormatter())
      .WriteTo.Seq(ctx.Configuration["Seq:Url"])
      .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));
  ```
- הוסף `RequestLoggingMiddleware` שלוג כל request: method, path, statusCode, duration, userId (מה-JWT)
- הוסף correlation ID middleware: generate GUID per request, attach ל-header `X-Correlation-Id` ול-log context
- הגדר log levels ב-`appsettings.json`: default=Information, Microsoft=Warning, EF=Warning
- הוסף sensitive data scrubber: scrub passwords, tokens, card numbers מה-logs

**קריטריוני קבלה:**
- [ ] כל request מולג ב-Seq
- [ ] Correlation ID קיים בכל log entry
- [ ] Passwords/tokens לא מופיעים ב-logs
- [ ] Log levels נכונים

---

## T-007 | S | INFRA
### כותרת: Configure OpenTelemetry Tracing and Metrics
**תלויות:** T-003
**Layer:** BE

**מה לעשות בדיוק:**
- הוסף packages: OpenTelemetry.Extensions.Hosting, OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.EntityFrameworkCore, OpenTelemetry.Instrumentation.StackExchangeRedis, OpenTelemetry.Exporter.Otlp
- הגדר ב-Shared `OpenTelemetryExtensions`:
  ```csharp
  services.AddOpenTelemetry()
      .WithTracing(b => b
          .AddAspNetCoreInstrumentation()
          .AddEntityFrameworkCoreInstrumentation()
          .AddRedisInstrumentation()
          .AddSource("ExamAI.*")
          .AddOtlpExporter())
      .WithMetrics(b => b
          .AddAspNetCoreInstrumentation()
          .AddRuntimeInstrumentation()
          .AddOtlpExporter());
  ```
- הוסף custom `ActivitySource` ב-Shared: `ExamAI.OCR`, `ExamAI.Grading`, `ExamAI.Export`
- צור `MetricsCollector` שמודד:
  - `examai_ocr_pages_processed_total` (counter)
  - `examai_grading_duration_seconds` (histogram)
  - `examai_active_grading_jobs` (gauge)
- הגדר Grafana ב-docker-compose עם Prometheus + OTLP collector

**קריטריוני קבלה:**
- [ ] Traces נראים ב-Jaeger/Grafana
- [ ] Custom metrics נאספים
- [ ] EF Core queries מוצגים ב-traces

---

## T-008 | S | INFRA
### כותרת: Angular 18 Project Setup with Core Modules
**תלויות:** T-001
**Layer:** FE

**מה לעשות בדיוק:**
- בתוך `src/frontend/`, הרץ: `ng new examai-web --routing --style=scss --ssr=false`
- התקן packages:
  - `@angular/material` + `@angular/cdk` (Material Design 3)
  - `@ngrx/store`, `@ngrx/effects`, `@ngrx/router-store` (state management)
  - `@stripe/stripe-js` (billing)
  - `chart.js` + `ng2-charts` (analytics)
  - `ngx-translate` (i18n — עברית + אנגלית)
  - `pdf-lib` (PDF preview)
  - `ngx-file-drop` (drag & drop upload)
- הגדר module structure:
  ```
  src/app/
  ├── core/           ← singleton services, guards, interceptors
  ├── shared/         ← shared components, pipes, directives
  ├── features/
  │   ├── auth/
  │   ├── exams/
  │   ├── grading/
  │   ├── billing/
  │   ├── analytics/
  │   └── admin/
  └── layout/         ← navbar, sidebar, shell
  ```
- הגדר `environment.ts` ו-`environment.prod.ts` עם apiUrl, stripeKey
- הגדר `app.routes.ts` עם lazy loading לכל feature module
- הגדר RTL support ב-`angular.json` (Hebrew = RTL): הוסף `"index": "src/index.html"` עם `dir="rtl"` ו-`lang="he"`
- הגדר Angular Material theme עם צבעים של ExamAI (primary: #3B82F6, accent: #10B981)

**קריטריוני קבלה:**
- [ ] `ng serve` עולה ללא שגיאות
- [ ] RTL עובד
- [ ] Lazy loading מוגדר
- [ ] NgRx store מוגדר (empty state)
- [ ] Material theme מוגדר

---

## T-009 | S | INFRA
### כותרת: API Gateway — YARP Configuration with Rate Limiting
**תלויות:** T-003
**Layer:** BE

**מה לעשות בדיוק:**
- צור project חדש `ExamAI.Gateway` (.NET 9 Web API)
- התקן `Yarp.ReverseProxy`
- הגדר routes ב-`appsettings.json`:
  ```json
  {
    "ReverseProxy": {
      "Routes": {
        "identity": { "ClusterId": "identity", "Match": { "Path": "/api/v1/auth/{**catch-all}" } },
        "exams": { "ClusterId": "exams", "Match": { "Path": "/api/v1/exams/{**catch-all}" } },
        "billing": { "ClusterId": "billing", "Match": { "Path": "/api/v1/billing/{**catch-all}" } },
        "grading": { "ClusterId": "grading", "Match": { "Path": "/api/v1/grades/{**catch-all}" } },
        "analytics": { "ClusterId": "analytics", "Match": { "Path": "/api/v1/analytics/{**catch-all}" } },
        "admin": { "ClusterId": "admin", "Match": { "Path": "/api/v1/admin/{**catch-all}" } }
      }
    }
  }
  ```
- הגדר **Rate Limiting** (ASP.NET Core 8+ built-in):
  - Global: 100 req/min per IP
  - Auth endpoints: 10 req/min per IP (stricter)
  - Upload endpoints: 20 req/min per user
  - Store rate limit state ב-Redis
- הגדר CORS: allow Angular origin (localhost:4200 ב-dev, production domain ב-prod)
- הגדר Security Headers middleware:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Strict-Transport-Security: max-age=31536000`
  - `Content-Security-Policy` מתאים
- הגדר JWT validation ב-Gateway (validate token לפני forward)

**קריטריוני קבלה:**
- [ ] כל routes מנותבים נכון
- [ ] Rate limiting פועל (401/429 מוחזר)
- [ ] Security headers מוחזרים בכל response
- [ ] CORS מוגדר נכון

---

## T-010 | XS | INFRA
### כותרת: GitHub Actions CI Pipeline — Build and Test
**תלויות:** T-003, T-008
**Layer:** INFRA

**מה לעשות בדיוק:**
- צור `.github/workflows/ci.yml`:
  - Trigger: push ל-`develop` ו-PR ל-`main`
  - Jobs:
    1. **backend-build**: `dotnet restore`, `dotnet build`, `dotnet test`
    2. **frontend-build**: `npm ci`, `ng build --configuration=production`, `ng test --watch=false`
    3. **docker-build**: build Docker images לכל service (בלי push ב-CI)
  - Cache: NuGet packages, npm cache
  - Secrets: AZURE_AI_KEY (לצורך integration tests)
- צור `.github/workflows/cd.yml`:
  - Trigger: push ל-`main`
  - Jobs: build → docker push → kubectl apply
- הוסף `CODEOWNERS` file

**קריטריוני קבלה:**
- [ ] CI pipeline עובר על כל PR
- [ ] Tests רצים אוטומטית
- [ ] Failed tests חוסמים merge

---

# ══════════════════════════════════════════
# EPIC 1 — IDENTITY & AUTHENTICATION — BACKEND
# ══════════════════════════════════════════

---

## T-011 | S | BE
### כותרת: Identity API — Project Bootstrap and JWT Configuration
**תלויות:** T-003, T-004, T-006
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Identity.API` project
- הגדר `Program.cs` עם:
  - `builder.Services.AddDbContext<IdentityDbContext>()`
  - `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  - JWT config: Issuer, Audience, SigningKey (256-bit minimum) מ-`appsettings.json` / Vault
  - Access token expiry: 15 דקות
  - Refresh token expiry: 7 ימים
  - Token settings stored ב-`JwtSettings` POCO
- צור `TokenService` עם methods:
  - `GenerateAccessToken(userId, email, roles) → string`
  - `GenerateRefreshToken() → string` (32 bytes random, Base64Url)
  - `ValidateAccessToken(token) → ClaimsPrincipal?`
  - `GetUserIdFromToken(token) → Guid?`
- כל claims: `sub`, `email`, `roles` (array), `jti` (unique token ID), `iat`, `exp`
- הגדר `ICurrentUserService` שחולץ userId מה-HttpContext

**קריטריוני קבלה:**
- [ ] JWT generation ו-validation עובדים
- [ ] Access token פג תוקף אחרי 15 דקות
- [ ] Claims נכונים ב-token
- [ ] `ICurrentUserService` מחזיר userId נכון

---

## T-012 | S | BE
### כותרת: Identity API — User Registration Endpoint
**תלויות:** T-011, T-004
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/auth/register`
- Request DTO:
  ```json
  { "email": "string", "password": "string", "firstName": "string", "lastName": "string" }
  ```
- Validations (FluentValidation):
  - email: valid format, max 320 chars
  - password: min 8 chars, must contain uppercase, lowercase, digit, special char
  - firstName/lastName: not empty, max 100 chars, no special chars
- Logic:
  1. Check email לא קיים כבר ב-DB (case-insensitive)
  2. Hash password עם BCrypt cost=12
  3. Insert ל-`identity.users`
  4. Insert ל-`identity.user_roles` עם role `teacher`
  5. Insert ל-`billing.subscriptions` עם plan `free`
  6. Generate verification token (32 bytes random, store SHA-256 hash ב-`identity.email_verifications`, expires 24h)
  7. Publish `UserRegisteredEvent` ל-RabbitMQ (notification worker ישלח email)
  8. Insert audit log
- Response 201:
  ```json
  { "userId": "uuid", "email": "string", "message": "Please verify your email" }
  ```
- Response 400 אם email כבר קיים
- Response 422 אם validation fails

**קריטריוני קבלה:**
- [ ] User נוצר ב-DB
- [ ] Password מאוחסן כ-hash בלבד
- [ ] Verification token נשמר כ-hash
- [ ] Event נשלח ל-RabbitMQ
- [ ] Duplicate email מחזיר 400

---

## T-013 | S | BE
### כותרת: Identity API — Email Verification Endpoint
**תלויות:** T-012
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/auth/verify-email`
- Request: `{ "token": "string" }` (הtoken שנשלח ב-email)
- Logic:
  1. Hash הtoken שהתקבל עם SHA-256
  2. חפש ב-`identity.email_verifications` לפי token_hash
  3. בדוק: exists, not used, not expired
  4. Mark `used_at = NOW()`
  5. Update `identity.users` → `email_verified = true`
  6. Publish `EmailVerifiedEvent`
  7. Insert audit log
- Response 200: `{ "message": "Email verified successfully" }`
- Response 400: token לא קיים / כבר שומש / פג תוקף
- צור `POST /api/v1/auth/resend-verification`:
  - מקבל `{ "email": "string" }`
  - יוצר token חדש, מבטל ישן, שולח מחדש
  - Rate limit: 3 פעמים ב-10 דקות

**קריטריוני קבלה:**
- [ ] Token תקין מאמת email
- [ ] Token שפג תוקף מחזיר שגיאה ברורה
- [ ] Token שכבר שומש מחזיר שגיאה
- [ ] Resend עובד עם rate limit

---

## T-014 | S | BE
### כותרת: Identity API — Login Endpoint (Email + Password)
**תלויות:** T-011, T-004
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/auth/login`
- Request: `{ "email": "string", "password": "string" }`
- Logic:
  1. מצא user לפי email (case-insensitive)
  2. בדוק user קיים + לא deleted + לא suspended
  3. בדוק email_verified = true (אחרת 403 עם message מתאים)
  4. BCrypt.Verify(password, hash)
  5. אם fail: הוסף לcounter של failed attempts ב-Redis (key: `login_attempts:{email}`)
  6. אחרי 5 failures → lock 15 דקות → return 429
  7. בcase של הצלחה: נקה counter
  8. Generate access token + refresh token
  9. Save refresh token hash ב-`identity.sessions` עם ip, user_agent
  10. Publish audit log
- Response 200:
  ```json
  {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900,
    "user": { "id": "uuid", "email": "string", "firstName": "string", "roles": ["teacher"] }
  }
  ```
- Response 401: credentials שגויים (generic message — לא לחשוף אם email קיים)
- Response 403: email לא מאומת
- Response 429: too many attempts

**קריטריוני קבלה:**
- [ ] Login תקין מחזיר tokens
- [ ] 5 failures מובילים ל-lock
- [ ] Failed attempts נאספים ב-Redis
- [ ] Generic error message (אין account enumeration)

---

## T-015 | S | BE
### כותרת: Identity API — Google OAuth Login
**תלויות:** T-011, T-004
**Layer:** BE

**מה לעשות בדיוק:**
- התקן `Google.Apis.Auth`
- צור `POST /api/v1/auth/google`
- Request: `{ "idToken": "string" }` (הtoken שהAngular מקבל מ-Google)
- Logic:
  1. Validate the Google ID token עם `GoogleJsonWebSignature.ValidateAsync(idToken)`
  2. Extract: email, sub (googleId), name, picture
  3. חפש user לפי google_id, אחרי לפי email
  4. אם לא קיים → Create user: email_verified=true (Google כבר אימת), google_id=sub, password_hash=null
  5. אם קיים עם email אחר → link google_id לuser הקיים
  6. Generate access token + refresh token
  7. Save session
  8. Return same response כמו login
- הגדר allowed clientIds ב-config (Google Cloud Console client ID)

**קריטריוני קבלה:**
- [ ] Valid Google token יוצר session
- [ ] User נוצר אוטומטית אם לא קיים
- [ ] Invalid token מחזיר 401
- [ ] Email verified מסומן אוטומטית

---

## T-016 | S | BE
### כותרת: Identity API — Refresh Token + Logout
**תלויות:** T-014
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/auth/refresh`:
  - Request: `{ "refreshToken": "string" }`
  - Logic:
    1. Hash הtoken שהתקבל
    2. חפש ב-`identity.sessions` לפי hash
    3. בדוק: exists, not revoked, not expired
    4. Check userId → user active
    5. Generate new access token + new refresh token (rotation)
    6. Update session: revoke old refresh token, save new one
    7. Return new tokens
  - Response 401 אם token לא תקין
  - Implement: refresh token rotation (כל refresh מייצר token חדש ומבטל ישן)

- צור `POST /api/v1/auth/logout`:
  - Requires valid access token (Authorization header)
  - Logic:
    1. Extract jti מה-access token
    2. Add jti ל-Redis blacklist עם TTL = remaining token lifetime
    3. Revoke ה-refresh token (מצא לפי userId + mark revoked_at)
  - Response 204

- ב-JWT middleware: הוסף check שה-jti לא ב-Redis blacklist

**קריטריוני קבלה:**
- [ ] Refresh מחזיר tokens חדשים
- [ ] ישן refresh token נפסל אחרי שימוש
- [ ] Logout מבטל access token ב-Redis
- [ ] Revoked refresh token מחזיר 401

---

## T-017 | S | BE
### כותרת: Identity API — Forgot Password + Reset Password
**תלויות:** T-011, T-004
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/auth/forgot-password`:
  - Request: `{ "email": "string" }`
  - **תמיד** return 200 (לא לחשוף אם email קיים)
  - Logic: אם user קיים → Generate reset token (32 bytes) → save hash ב-`identity.password_resets` (expires 1 שעה) → publish event לשליחת email
  - Rate limit: 3 requests per email per hour ב-Redis

- צור `POST /api/v1/auth/reset-password`:
  - Request: `{ "token": "string", "newPassword": "string", "confirmPassword": "string" }`
  - Validate: passwords match, password strength
  - Logic:
    1. Hash token
    2. מצא ב-`identity.password_resets`
    3. בדוק: valid, not used, not expired
    4. Hash new password (BCrypt cost=12)
    5. Update user.password_hash
    6. Mark reset token as used
    7. Revoke ALL active sessions לuser זה (כדי לאלץ re-login)
    8. Publish audit log
  - Response 200 אם הצליח

**קריטריוני קבלה:**
- [ ] Forgot password תמיד 200 (no enumeration)
- [ ] Reset token תקף שעה
- [ ] אחרי reset, כל sessions מבוטלים
- [ ] Token שפג תוקף → 400 עם הודעה ברורה

---

## T-018 | XS | BE
### כותרת: Identity API — Get Current User Endpoint
**תלויות:** T-011
**Layer:** BE

**מה לעשות בדיוק:**
- צור `GET /api/v1/auth/me` (requires auth)
- Logic: חלץ userId מ-JWT → load user מ-DB → return profile
- Response:
  ```json
  {
    "id": "uuid",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "avatarUrl": "string",
    "emailVerified": true,
    "roles": ["teacher"],
    "subscription": {
      "planName": "free",
      "pagesUsed": 12,
      "pagesLimit": 25
    }
  }
  ```
- Cache ב-Redis למשך 5 דקות (key: `user:{userId}:profile`)
- Invalidate cache בכל עדכון user

**קריטריוני קבלה:**
- [ ] Authenticated request מחזיר profile
- [ ] Unauthenticated מחזיר 401
- [ ] Response כולל subscription info

---

## T-019 | S | BE
### כותרת: Identity API — RBAC Middleware and Authorization Policies
**תלויות:** T-011, T-004
**Layer:** BE

**מה לעשות בדיוק:**
- צור `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>`
- צור Policy definitions:
  - `"CanManageExams"` → role: teacher
  - `"CanGradeExams"` → role: teacher
  - `"CanViewAnalytics"` → role: teacher
  - `"CanAccessAdmin"` → role: admin / super_admin
  - `"CanManageUsers"` → role: super_admin
- Register ב-`Program.cs`:
  ```csharp
  services.AddAuthorization(opts => {
      opts.AddPolicy("CanManageExams", p => p.Requirements.Add(new PermissionRequirement("exams.write")));
      // ...
  });
  ```
- הוסף `[Authorize(Policy = "...")]` ל-controllers (הכנה לbaזה)
- צור extension method: `services.AddExamAIAuthorization()`
- Seed permissions ב-DB לכל resource + action combination
- הוסף `SuspendedUserMiddleware`: בכל request check אם user suspended → return 403

**קריטריוני קבלה:**
- [ ] Teacher לא יכול לגשת ל-admin routes
- [ ] Admin לא יכול לעשות super_admin actions
- [ ] Suspended user מקבל 403 בכל request

---

# ══════════════════════════════════════════
# EPIC 1 — IDENTITY & AUTHENTICATION — FRONTEND
# ══════════════════════════════════════════

---


## T-020 | S | FE
### כותרת: Frontend — Auth Store (NgRx) + HTTP Interceptors
**תלויות:** T-008, T-014
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/auth/store/`:
  - `auth.state.ts`: `{ user: User | null, accessToken: string | null, loading: boolean, error: string | null }`
  - `auth.actions.ts`: login, loginSuccess, loginFailure, logout, refreshToken, setUser
  - `auth.reducer.ts`
  - `auth.effects.ts`: handle API calls, navigate on success
  - `auth.selectors.ts`: selectUser, selectIsLoggedIn, selectIsAdmin
- צור `AuthInterceptor` (HTTP_INTERCEPTORS):
  - מוסיף `Authorization: Bearer {token}` לכל request
  - On 401 → ניסיון refresh token → retry request
  - אם refresh נכשל → dispatch logout → navigate to /login
- צור `TokenStorageService`:
  - שמור access token ב-memory (לא localStorage!)
  - שמור refresh token ב-`httpOnly` cookie equivalent (sessionStorage במקרה זה)
  - `setTokens(access, refresh)`, `clearTokens()`, `getAccessToken(): string | null`
- צור `AuthGuard` ו-`NonAuthGuard` (redirect אם כבר logged in)

**קריטריוני קבלה:**
- [ ] Access token לא ב-localStorage
- [ ] 401 מטריגר refresh אוטומטי
- [ ] Failed refresh מוביל ל-logout
- [ ] Guards מגינים על routes

---

## T-021 | S | FE
### כותרת: Frontend — Registration Page
**תלויות:** T-020, T-012
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/auth/pages/register/register.component.ts`
- Form (Reactive Forms):
  - שדות: firstName, lastName, email, password, confirmPassword
  - Validators: required, email, minLength(8), passwordStrength (custom validator), passwordsMatch
  - Real-time validation messages בעברית
  - Password strength indicator (progress bar: weak/medium/strong)
- UX:
  - Show/hide password toggle
  - Loading state על submit button
  - Error message מה-server (email כבר קיים)
  - Success → navigate ל-`/auth/verify-email-sent` עם הודעה "שלחנו לך email לאימות"
- Routing: `/auth/register`
- Link ל-login
- Google OAuth button (מנתב ל-T-022)

**קריטריוני קבלה:**
- [ ] Form validation עם הודעות בעברית
- [ ] Server errors מוצגים
- [ ] Success מוביל לדף אימות email
- [ ] Loading state על submit

---

## T-022 | S | FE
### כותרת: Frontend — Login Page + Google OAuth
**תלויות:** T-020, T-014, T-015
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/auth/pages/login/login.component.ts`
- Form: email + password + "Remember me" (מאריך refresh token ל-30 יום)
- Google OAuth:
  - התקן `@abacritt/angularx-social-login`
  - הגדר `GoogleLoginProvider` עם Client ID מ-environment
  - On Google sign-in → קרא `POST /api/v1/auth/google` עם idToken
  - Handle success: dispatch loginSuccess
- Login flow:
  - Submit → POST /api/v1/auth/login
  - Success → save tokens → navigate ל-`/dashboard`
  - 403 (unverified) → navigate ל-`/auth/verify-email-sent`
  - 429 → הצג "החשבון ננעל ל-15 דקות"
  - 401 → "אימייל או סיסמה שגויים"
- הצג link "שכחת סיסמה?"

**קריטריוני קבלה:**
- [ ] Login עם email/password עובד
- [ ] Google OAuth עובד
- [ ] שגיאות מוצגות בעברית
- [ ] Locked account מקבל הודעה ברורה

---

## T-023 | S | FE
### כותרת: Frontend — Email Verification + Forgot/Reset Password Pages
**תלויות:** T-020, T-013, T-017
**Layer:** FE

**מה לעשות בדיוק:**
- **Verify Email Page** (`/auth/verify-email`):
  - מקבל token מה-URL query param (`?token=...`)
  - On load → POST /api/v1/auth/verify-email
  - Success → הצג "✓ האימייל אומת בהצלחה!" + כפתור "כניסה"
  - Error → הצג שגיאה + כפתור "שלח שוב"

- **Verify Email Sent Page** (`/auth/verify-email-sent`):
  - הצג הודעה: "שלחנו לך email לאימות"
  - כפתור "שלח שוב" → POST /api/v1/auth/resend-verification
  - Countdown: מנע resend 60 שניות בין ניסיונות

- **Forgot Password Page** (`/auth/forgot-password`):
  - Form: email
  - POST /api/v1/auth/forgot-password
  - תמיד הצג: "אם האימייל קיים, שלחנו לך הוראות"

- **Reset Password Page** (`/auth/reset-password`):
  - מקבל token מה-URL
  - Form: newPassword + confirmPassword + password strength indicator
  - POST /api/v1/auth/reset-password
  - Success → navigate ל-login עם הודעה

**קריטריוני קבלה:**
- [ ] כל pages עובדים עם API
- [ ] הודעות בעברית
- [ ] Resend עם countdown

---

## T-024 | S | FE
### כותרת: Frontend — App Shell: Navbar, Sidebar, Layout
**תלויות:** T-020
**Layer:** FE

**מה לעשות בדיוק:**
- צור `layout/` module עם:
  - **AppShellComponent**: wraps כל authenticated pages
  - **NavbarComponent**: לוגו, שם משתמש, avatar, notifications bell, logout
  - **SidebarComponent**: links לפי role
    - Teacher: דשבורד, מבחנים שלי, ניתוח כיתה, חיוב
    - Admin: ניהול משתמשים, מנויים, הגדרות מערכת, לוגים
  - **BreadcrumbComponent**: breadcrumbs דינמיים לפי route
  - **NotificationComponent**: dropdown עם התראות אחרונות
- Responsive: sidebar collapsible ב-mobile
- Hebrew RTL: וודא שכל layout נראה נכון ב-RTL
- Unread count badge על notifications
- Bottom bar: "דף זה משתמש ב-12 מתוך 25 עמודים חינמיים" (לplan חינמי)

**קריטריוני קבלה:**
- [ ] Layout נראה נכון ב-desktop + mobile
- [ ] RTL תקין
- [ ] Sidebar מציג links נכונים לפי role
- [ ] Free plan usage bar מוצג

---

# ══════════════════════════════════════════
# EPIC 2 — BILLING — BACKEND
# ══════════════════════════════════════════

---

## T-025 | S | BE
### כותרת: Billing API — Project Bootstrap + Stripe SDK Setup
**תלויות:** T-003, T-004, T-019
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Billing.API` project
- התקן `Stripe.net` NuGet package
- צור `StripeSettings` POCO: SecretKey, PublishableKey, WebhookSecret
- צור `StripeClientService`:
  - `CreateCustomerAsync(userId, email, name) → string customerId`
  - `AttachPaymentMethodAsync(customerId, paymentMethodId)`
  - `SetDefaultPaymentMethodAsync(customerId, paymentMethodId)`
  - `ChargeCustomerAsync(customerId, amountILS, pages, invoiceDescription) → PaymentIntentResult`
  - `RetrievePaymentMethodAsync(paymentMethodId) → PaymentMethodDetails`
- הגדר ILS כמטבע (currency: "ils")
- הגדר `UsageService`:
  - `GetUsageAsync(userId, month?) → UsageSummary`
  - `AddUsageAsync(userId, examId, studentExamId, pages)`
  - `CanScanMorePages(userId) → bool` (בדוק free limit / paid)

**קריטריוני קבלה:**
- [ ] Stripe SDK מוגדר
- [ ] Customer creation עובד
- [ ] Usage tracking עובד
- [ ] `CanScanMorePages` מחזיר נכון לפי plan

---

## T-026 | S | BE
### כותרת: Billing API — Usage Tracking Endpoints
**תלויות:** T-025
**Layer:** BE

**מה לעשות בדיוק:**
- צור `GET /api/v1/billing/usage`:
  - Response:
    ```json
    {
      "planName": "free",
      "pagesUsedTotal": 12,
      "pagesLimitFree": 25,
      "pagesRemaining": 13,
      "isUnlimited": false,
      "currentMonthUsage": 12,
      "billingHistory": [...]
    }
    ```
  - Cache ב-Redis 1 דקה

- צור `GET /api/v1/billing/plan`:
  - מחזיר plan details + usage summary

- צור internal endpoint `POST /api/v1/internal/billing/record-usage` (internal only, no public access):
  - Request: `{ "userId": "uuid", "examId": "uuid", "studentExamId": "uuid", "pages": int }`
  - OCR Worker קורא לזה לאחר סיום OCR
  - Logic: בדוק plan → אם free ו-usage >= 25 → reject ב-OCR level

**קריטריוני קבלה:**
- [ ] Usage נספר נכון
- [ ] Free plan נחסם ב-25 עמודים
- [ ] Cache פועל

---

## T-027 | S | BE
### כותרת: Billing API — Payment Method Management
**תלויות:** T-025
**Layer:** BE

**מה לעשות בדיוק:**
- צור `GET /api/v1/billing/payment-methods`:
  - מחזיר רשימת payment methods שמורים לuser
  - Response: `[{ id, brand, last4, expMonth, expYear, isDefault }]`

- צור `POST /api/v1/billing/payment-methods`:
  - Request: `{ "paymentMethodId": "string" }` (Stripe PaymentMethod ID מה-frontend)
  - Logic:
    1. אם Stripe customer לא קיים → צור
    2. Attach payment method ל-customer ב-Stripe
    3. שמור ב-`billing.payment_methods`
    4. אם ראשון → set as default
  - Response: 201 עם payment method details

- צור `DELETE /api/v1/billing/payment-methods/{id}`:
  - Detach מ-Stripe
  - Mark deleted_at

- צור `PUT /api/v1/billing/payment-methods/{id}/default`:
  - Set כ-default, unset שאר

**קריטריוני קבלה:**
- [ ] Payment methods שמורים בצורה מאובטחת (Stripe token בלבד)
- [ ] Delete עובד ב-Stripe וב-DB
- [ ] Default method מסומן נכון

---

## T-028 | S | BE
### כותרת: Billing API — Stripe Webhook Handler
**תלויות:** T-025
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/webhooks/stripe` (NO authentication — Stripe calls this):
  - Validate Stripe webhook signature עם `Stripe-Signature` header + WebhookSecret
  - Handle events:
    - `payment_intent.succeeded`:
      - מצא invoice לפי Stripe ID
      - Update status ל-`paid`
      - Set `paid_at`
      - Publish `InvoicePaidEvent` → notification worker שולח receipt email
    - `payment_intent.payment_failed`:
      - Update invoice status ל-`open`
      - Publish `PaymentFailedEvent` → email לuser
    - `customer.updated`:
      - Sync customer data
  - תמיד return 200 (Stripe retry logic)
  - Log כל webhook ל-audit

**קריטריוני קבלה:**
- [ ] Webhook signature validated
- [ ] Invoice status מתעדכן
- [ ] Receipt email נשלח אחרי תשלום מוצלח
- [ ] Invalid signature → 400

---

## T-029 | S | BE
### כותרת: Billing API — Invoice Generation + History
**תלויות:** T-025, T-028
**Layer:** BE

**מה לעשות בדיוק:**
- צור `InvoiceService`:
  - `CreateInvoiceAsync(userId, pages, amount)`:
    - Insert ל-`billing.invoices`
    - יצור Stripe PaymentIntent
    - Confirm עם default payment method
  - יקרא מ-OCR Worker לאחר סיום עבודה (עבור paid users)

- צור `GET /api/v1/billing/invoices`:
  - פגינציה: page + pageSize
  - Response:
    ```json
    {
      "invoices": [
        { "id": "uuid", "amount": 4.20, "pagesCount": 21, "status": "paid", "pdfUrl": "...", "createdAt": "..." }
      ],
      "total": 15
    }
    ```

- צור `GET /api/v1/billing/invoices/{id}/download`:
  - Generate PDF receipt (QuestPDF או Stripe hosted invoice)
  - Return presigned S3 URL (expire 15 דקות)

**קריטריוני קבלה:**
- [ ] Invoice נוצרת אחרי OCR עבור paid user
- [ ] History מוצגת בפגינציה
- [ ] PDF download עובד

---

# ══════════════════════════════════════════
# EPIC 2 — BILLING — FRONTEND
# ══════════════════════════════════════════

---

## T-030 | S | FE
### כותרת: Frontend — Billing Page: Usage + Payment Methods
**תלויות:** T-020, T-026, T-027
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/billing/pages/billing.component.ts` בRoute `/billing`
- Sections:
  1. **Usage Summary Card**:
     - Progress bar: "12 מתוך 25 עמודים חינמיים שומשו"
     - לpaid users: "סה"כ שומשו החודש: 47 עמודים"
  2. **Payment Methods** (רק לregistered users):
     - הצג saved cards עם brand icon, last4, expiry
     - כפתור "הסר", כפתור "קבע כברירת מחדל"
     - כפתור "הוסף כרטיס" → Stripe Elements form
  3. **Stripe Elements Form** (modal):
     - השתמש ב-`@stripe/stripe-js` + `Elements`
     - CardElement מ-Stripe (PCI compliant)
     - On submit: `stripe.createPaymentMethod()` → שלח paymentMethodId לbackend
- הצג "הוסף כרטיס" כפתור עם lock icon ו-"מאובטח ע"י Stripe"

**קריטריוני קבלה:**
- [ ] Card number לא עובר דרך backend שלנו
- [ ] Payment methods מוצגים
- [ ] Add/remove עובד
- [ ] Stripe Elements מוצג בצורה מאובטחת

---

## T-031 | S | FE
### כותרת: Frontend — Billing History Page
**תלויות:** T-020, T-029
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/billing/pages/billing-history.component.ts` בRoute `/billing/history`
- Table עם columns: תאריך, עמודים שנסרקו, סכום, סטטוס, הורדה
- Status badges: שולם (ירוק), ממתין (כתום), בוטל (אדום)
- כפתור "הורד קבלה" לכל invoice → מוריד PDF
- Pagination: 10 per page
- Export all ל-CSV (client-side, מnormalized data)

**קריטריוני קבלה:**
- [ ] טבלה עם כל invoices
- [ ] PDF download עובד
- [ ] Pagination עובד

---

# ══════════════════════════════════════════
# EPIC 3 — EXAM MANAGEMENT — BACKEND
# ══════════════════════════════════════════

---


## T-032 | S | BE
### כותרת: Exam API — Project Bootstrap + File Upload Service
**תלויות:** T-003, T-004, T-019
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Exam.API` project
- צור `S3FileService`:
  - `UploadFileAsync(stream, fileName, contentType, folder) → S3Key`
  - Key pattern: `{userId}/{examId}/{type}/{timestamp}_{fileName}`
  - `GetPresignedDownloadUrl(key, expiresIn) → string`
  - `DeleteFileAsync(key)`
  - Max file size: 50MB per file
  - Allowed types: application/pdf, image/jpeg, image/png, image/tiff
- צור `VirusScanService`:
  - שלח file ל-ClamAV TCP socket (127.0.0.1:3310)
  - Return: `{ IsSafe: bool, ThreatName: string? }`
  - Timeout: 30 שניות
  - אם ClamAV לא זמין → log warning + allow (degraded mode)
- צור `FileValidationService`:
  - בדוק magic bytes (לא רק extension)
  - PDF: `%PDF` header
  - JPEG: `FF D8 FF`
  - PNG: `89 50 4E 47`
  - Return 415 אם invalid

**קריטריוני קבלה:**
- [ ] Files נשמרים ב-S3 עם structured path
- [ ] Virus scan רץ על כל upload
- [ ] Magic bytes validated
- [ ] Infected file → 422 + נמחק מ-S3

---

## T-033 | S | BE
### כותרת: Exam API — Exam CRUD Endpoints
**תלויות:** T-032
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/exams`:
  - Request: `{ "title": "string", "subject": "string", "gradeLevel": "string", "maxScore": 100, "strictness": 50 }`
  - Insert ל-`exam.exams` עם status=draft
  - Return 201 עם exam object
  
- צור `GET /api/v1/exams` (teacher sees only their exams):
  - Query params: page, pageSize, status filter, search (by title)
  - Response: paginated list
  
- צור `GET /api/v1/exams/{id}`:
  - בדוק ownership (teacherId = currentUserId) → 403 אם לא
  - Include: questions count, student exams count, status

- צור `PUT /api/v1/exams/{id}`:
  - אפשר עדכון של title, subject, gradeLevel, maxScore, strictness
  - רק ב-draft status (לא ניתן לעדכון אחרי grading)

- צור `DELETE /api/v1/exams/{id}`:
  - Soft delete (deleted_at)
  - רק ב-draft status
  - אם יש student exams → 409 Conflict

- צור `PUT /api/v1/exams/{id}/strictness`:
  - Request: `{ "strictness": 75 }`
  - Validate 0-100
  - Update + audit log

**קריטריוני קבלה:**
- [ ] Teacher רואה רק exams שלו
- [ ] Exam במצב grading לא ניתן לעדכון
- [ ] Soft delete עובד

---

## T-034 | M | BE
### כותרת: Exam API — Template Upload + Question Parser
**תלויות:** T-032, T-033
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/exams/{id}/template` (multipart/form-data):
  1. Validate file (type, size)
  2. Virus scan
  3. Upload ל-S3 בpath: `{userId}/{examId}/template/{filename}`
  4. Insert ל-`exam.exam_templates`
  5. Publish `TemplateUploadedEvent` → trigger question parsing
  6. Return 201

- צור `QuestionParserService` שרץ כ-background job (Hangfire או background task):
  1. Download file מ-S3
  2. אם PDF → convert pages ל-images (ImageMagick/PdfiumViewer)
  3. שלח ל-Azure Document Intelligence (Layout model) לחלץ text
  4. שלח הtext ל-GPT-4o עם prompt:
     ```
     Extract all questions from this Hebrew exam.
     Return JSON: [{ "number": int, "text": string, "maxPoints": float }]
     Order by question number.
     ```
  5. Parse response → insert ל-`exam.questions`
  6. Update exam.status = 'active' אם הצליח
  7. Notify teacher (event → email)

- צור `GET /api/v1/exams/{id}/questions`:
  - Return כל השאלות עם rubric items

**קריטריוני קבלה:**
- [ ] Template upload עובד עם PDF ו-image
- [ ] Questions מחולצות אוטומטית
- [ ] Hebrew text מחולץ נכון
- [ ] אם parser נכשל → status='draft', notify teacher

---

## T-035 | M | BE
### כותרת: Exam API — Answer Key Upload + Auto-Build Rubric
**תלויות:** T-034
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/exams/{id}/answer-key` (multipart/form-data):
  - Same upload flow כמו template
  - Trigger rubric building

- צור `RubricBuilderService`:
  1. Download answer key מ-S3
  2. OCR → extract text
  3. שלח ל-GPT-4o עם prompt:
     ```
     Given these exam questions and this answer key (in Hebrew),
     build a grading rubric.
     Return JSON: [{
       "questionNumber": int,
       "correctAnswer": "string",
       "alternativeAnswers": ["string"],
       "gradingNotes": "string",
       "points": float
     }]
     ```
  4. Match rubric items לquestions לפי questionNumber
  5. Insert ל-`exam.rubric_items`
  6. Publish `RubricBuiltEvent`

- צור `GET /api/v1/exams/{id}/rubric`:
  - Return questions עם rubric_items

- צור `PUT /api/v1/exams/{id}/rubric`:
  - Request: `{ "items": [{ "questionId": "uuid", "correctAnswer": "...", "alternativeAnswers": [...], "gradingNotes": "...", "points": float }] }`
  - Update rubric_items
  - Audit log

- צור `PUT /api/v1/exams/{id}/questions/{questionId}`:
  - Update שאלה ספציפית (text, maxPoints)

**קריטריוני קבלה:**
- [ ] Rubric נבנה אוטומטית מmatch לשאלות
- [ ] Manual editing שומר שינויים
- [ ] Alternative answers נשמרים כarray

---

# ══════════════════════════════════════════
# EPIC 3 — EXAM MANAGEMENT — FRONTEND
# ══════════════════════════════════════════

---

## T-036 | S | FE
### כותרת: Frontend — Exam List Page
**תלויות:** T-024, T-033
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/exams/pages/exam-list/exam-list.component.ts` ב-Route `/exams`
- Layout: Grid cards (3 per row desktop, 1 mobile)
- כל card מציג: שם מבחן, מקצוע, כיתה, תאריך יצירה, מספר תלמידים, סטטוס badge
- Status badges צבעוניים: טיוטה (אפור), פעיל (כחול), בבדיקה (כתום), הושלם (ירוק)
- Toolbar: חיפוש בשם, filter לפי סטטוס, כפתור "מבחן חדש"
- Pagination או infinite scroll
- Empty state: "עדיין אין מבחנים. צור מבחן ראשון!" + כפתור
- Skeleton loading state
- כל card: כפתורי פעולה (עריכה, מחיקה, צפייה בניתוח) על hover

**קריטריוני קבלה:**
- [ ] Exams מוצגים ב-grid
- [ ] חיפוש ו-filter עובדים
- [ ] Empty state מוצג
- [ ] Loading skeleton מוצג

---

## T-037 | S | FE
### כותרת: Frontend — Create/Edit Exam Form + Strictness Slider
**תלויות:** T-036, T-033
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/exams/pages/exam-form/exam-form.component.ts`
- Form fields:
  - שם המבחן (required)
  - מקצוע (dropdown: מתמטיקה, עברית, היסטוריה, מדעים, אנגלית, אחר)
  - כיתה (free text)
  - ציון מקסימלי (number, default 100)
  - **Strictness Slider**:
    - Range: 0–100
    - Labels: 0="סמנטי בלבד", 50="מאוזן", 100="מילולי מדויק"
    - Tooltip: הסבר מה כל ערך אומר
    - Preview text: "בחומרה זו, תשובה כ'שלוש' תתקבל עבור '3'"
- Submit → POST /api/v1/exams
- Navigate ל-`/exams/{id}/setup` לאחר יצירה

**קריטריוני קבלה:**
- [ ] Form validation בעברית
- [ ] Strictness slider עם הסבר
- [ ] Navigate לsetup אחרי יצירה

---

## T-038 | M | FE
### כותרת: Frontend — Exam Setup Wizard (Template + Answer Key + Rubric)
**תלויות:** T-037, T-034, T-035
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/exams/pages/exam-setup/exam-setup.component.ts` ב-Route `/exams/{id}/setup`
- **Step 1: Upload Template**:
  - Drag & drop zone (PDF/image)
  - Preview: thumbnail של הדף הראשון
  - Upload progress bar
  - "מעבד שאלות..." spinner אחרי upload
  - הצגת שאלות שנמצאו כשhעיבוד מסתיים (polling כל 2 שניות על status)
  - Allow manual add/edit questions אם parser נכשל

- **Step 2: Upload Answer Key**:
  - Same upload UX
  - "בונה רובריקה..." spinner
  - הצגת רובריקה שנבנתה

- **Step 3: Edit Rubric**:
  - Editable table: שאלה | תשובה נכונה | תשובות חלופיות | הערות | ניקוד
  - Inline edit: click to edit
  - Add/remove alternative answers (chip input)
  - Save button

- Stepper component בראש הדף (1-2-3 progress)
- ניתן לחזור לstep קודם

**קריטריוני קבלה:**
- [ ] Stepper מדריך את המורה
- [ ] Upload עם drag & drop
- [ ] Rubric editable ב-table
- [ ] Real-time status updates

---

# ══════════════════════════════════════════
# EPIC 4 — OCR PROCESSING — BACKEND
# ══════════════════════════════════════════

---

## T-039 | S | BE
### כותרת: Exam API — Student Exam Upload Endpoint
**תלויות:** T-032, T-026
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/exams/{examId}/student-exams` (multipart/form-data):
  - Fields: `studentName` (string), `studentExternalId` (optional), `files[]` (multiple files)
  - Validations:
    - examId belongs to teacher
    - Exam status = 'active' (צריך template + rubric)
    - max 20 files per student
    - max 50MB per file
  - Usage check: קרא billing service → אם free ו-usage >= 25 → 402 Payment Required
  - לכל file:
    1. Validate magic bytes
    2. Virus scan
    3. Upload ל-S3: `{userId}/{examId}/students/{studentExamId}/{filename}`
  - Count total pages (PDF → count pages, image → 1 page)
  - Record usage: `AddUsageAsync(userId, examId, studentExamId, totalPages)`
  - Insert ל-`ocr.student_exams`
  - Publish `OcrJobQueuedEvent` ל-RabbitMQ → queue: `ocr.jobs`
  - Return 202 Accepted עם `{ studentExamId, status: "processing" }`

- צור `GET /api/v1/exams/{examId}/student-exams`:
  - Return list עם status כל תלמיד

- צור `GET /api/v1/student-exams/{id}/status`:
  - Return current status + progress percentage

**קריטריוני קבלה:**
- [ ] Multi-file upload עובד
- [ ] Billing check מונע upload מעבר לlimit
- [ ] OCR job נשלח לqueue
- [ ] Status polling endpoint עובד

---

## T-040 | M | BE
### כותרת: OCR Worker — Setup + Image Preprocessing Pipeline
**תלויות:** T-005, T-039
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.OCR.Worker` (.NET 9 Worker Service)
- צור `OcrJobConsumer` : `IMessageConsumer<OcrJobMessage>`:
  - Consume מ-queue `ocr.jobs`
  - Concurrency: 3 concurrent jobs
  - Ack message רק לאחר הצלחה מלאה

- צור `ImagePreprocessingService`:
  - **PDF Support**: השתמש ב-`PdfiumViewer` להמיר PDF pages ל-images (300 DPI)
  - **Deskew**: חשב skew angle עם Hough transform (OpenCvSharp), תקן אם angle > 0.5°
  - **Orientation Correction**: בדוק אם דף הפוך (ML model או Tesseract OSD)
  - **Noise Removal**: bilateral filter לhandwriting, median filter לscan artifacts
  - **Contrast Enhancement**: histogram equalization
  - **Output**: PNG 300 DPI per page
  - שמור preprocessed images ב-S3: `{examId}/ocr_processed/{studentExamId}/page_{n}.png`

- אחרי preprocessing → update `ocr.ocr_pages` עם flags: deskew_applied, orientation_correction

**קריטריוני קבלה:**
- [ ] PDF converted ל-images
- [ ] Deskew פועל
- [ ] Noise removal מיושם
- [ ] Preprocessed images נשמרים ב-S3
- [ ] Concurrency limit מוגדר

---

## T-041 | M | BE
### כותרת: OCR Worker — Azure Document Intelligence Hebrew Integration
**תלויות:** T-040
**Layer:** BE

**מה לעשות בדיוק:**
- צור `AzureDocumentIntelligenceService`:
  - Client: `DocumentAnalysisClient` עם endpoint + api key מ-config
  - Model: `prebuilt-layout` (supports Hebrew RTL)
  - `AnalyzePageAsync(imageBytes) → DocumentPage`:
    - קרא `AnalyzeDocumentAsync("prebuilt-layout", stream)`
    - Extract: lines, words, בounding boxes
    - Detect language (Hebrew vs Arabic vs mixed)
    - confidence per word
    - Return structured result

- צור `HandwritingRecognitionService`:
  - Custom model (fine-tuned ל-Hebrew handwriting): deploy ב-Azure Custom Vision
  - Fallback: אם custom model confidence < 0.6 → try Azure general handwriting model
  - `RecognizeHandwritingAsync(imageRegion, boundingBox) → string`

- צור `AnswerExtractionService`:
  - Input: OCR results + exam question layout (bounding boxes מה-template)
  - Match כל answer region לשאלה הרלוונטית לפי proximity/position
  - Output: `List<ExtractedAnswer>` עם question_id, raw_text, confidence, bounding_box
  - אם confidence < 0.65 → set needs_review = true

**קריטריוני קבלה:**
- [ ] Azure DI מחלץ text בעברית
- [ ] Bounding boxes מדויקים
- [ ] Handwriting מזוהה
- [ ] Low confidence מסומן לreview

---

## T-042 | S | BE
### כותרת: OCR Worker — Results Persistence + Grading Job Trigger
**תלויות:** T-041
**Layer:** BE

**מה לעשות בדיוק:**
- לאחר השלמת OCR:
  1. Insert כל pages ל-`ocr.ocr_pages`
  2. Insert כל answers ל-`ocr.extracted_answers`
  3. Update `ocr.student_exams.status` → `ocr_complete` (או `low_confidence` אם average < 0.7)
  4. Update `ocr.ocr_jobs.status` → `completed`
  5. Publish `GradingJobMessage` ל-queue: `grading.jobs`:
     ```json
     {
       "studentExamId": "uuid",
       "examId": "uuid",
       "teacherId": "uuid"
     }
     ```
  6. אם error → update status → `ocr_failed`, publish `OcrFailedEvent` → notify teacher

- Retry logic:
  - Max retries: 3
  - Backoff: 1 min, 5 min, 15 min
  - לאחר 3 failures → move to DLQ → notify admin + teacher

**קריטריוני קבלה:**
- [ ] Results שמורים ב-DB
- [ ] Grading job triggered
- [ ] Retry logic עובד
- [ ] Failure notification נשלחת

---

# ══════════════════════════════════════════
# EPIC 4 — OCR — FRONTEND
# ══════════════════════════════════════════

---

## T-043 | M | FE
### כותרת: Frontend — Student Exam Upload Page
**תלויות:** T-024, T-039
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/exams/pages/student-upload/student-upload.component.ts` ב-Route `/exams/{id}/students/upload`
- Layout:
  - הצג exam details בראש
  - Free plan alert: "נשארו לך X עמודים חינמיים"
- Upload Form:
  - שם התלמיד (required)
  - מזהה תלמיד (optional, לצרכי ייצוא)
  - Drag & drop zone (מקבל multiple files)
  - Preview thumbnails של הקבצים שנוספו
  - הצג סה"כ עמודים שיחויבו
  - Confirm עם עמודים בפני עצמן (אם paid)
- Upload button → POST + progress per file (parallel uploads)
- לאחר הצלחה → navigate ל-student exams list עם הודעת success

- **Student Exams List** (`/exams/{id}/students`):
  - Table: שם, זמן upload, סטטוס, פעולות
  - Status col: Processing / OCR Done / Grading / Graded / Error
  - Auto-refresh כל 5 שניות לסטטוסים pending
  - "פתח לבדיקה" כפתור כשסטטוס = Graded
  - Bulk upload: אפשר להעלות קובץ ZIP עם מבחנים מרובים

**קריטריוני קבלה:**
- [ ] Multi-file drag & drop
- [ ] Page count הוצג לפני upload
- [ ] Auto-refresh לסטטוס
- [ ] Free plan warning

---

# ══════════════════════════════════════════
# EPIC 5 — AI GRADING — BACKEND
# ══════════════════════════════════════════

---

## T-044 | M | BE
### כותרת: Grading Worker — Setup + OpenAI Grading Service
**תלויות:** T-005, T-042
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Grading.Worker` (.NET 9 Worker Service)
- צור `GradingJobConsumer` : consume מ-`grading.jobs`
- צור `OpenAiGradingService`:
  - Client: `OpenAIClient` עם API key מ-config
  - Model: `gpt-4o` (JSON mode)
  - `GradeQuestionAsync(request) → GradingResult`
  - Timeout: 30 שניות per question
  - Retry: 3 times עם exponential backoff
  - Rate limiting: max 10 concurrent requests לOpenAI
  - Fallback: אם GPT-4o fails 3 times → try `gpt-4-turbo`

- **Prompt Template** (stored in DB, admin-editable):
  ```
  You are grading a Hebrew school exam.
  
  Question: {questionText}
  Correct Answer: {correctAnswer}
  Alternative Accepted Answers: {alternativeAnswers}
  Grading Notes: {gradingNotes}
  Max Points: {maxPoints}
  Strictness Level: {strictness}/100 (0=semantic match OK, 100=exact wording required)
  
  Student's Answer: {studentAnswer}
  
  Respond in JSON only:
  {
    "isCorrect": boolean,
    "score": number,
    "deduction": number,
    "explanation": "string in Hebrew",
    "confidence": number (0-1)
  }
  ```

**קריטריוני קבלה:**
- [ ] Grading worker consumes jobs
- [ ] GPT-4o מחזיר structured JSON
- [ ] Fallback לGPT-4-turbo עובד
- [ ] Rate limiting לOpenAI מיושם

---

## T-045 | M | BE
### כותרת: Grading Worker — Strictness Engine + Score Calculator
**תלויות:** T-044
**Layer:** BE

**מה לעשות בדיוק:**
- צור `StrictnessEngine`:
  - Strictness 0–100 משפיע על ה-prompt ועל ה-post-processing
  - Strictness 0–30 (Semantic): אם LLM ענה partial credit → allow, semantic similarity check
  - Strictness 31–70 (Balanced): LLM decision = final
  - Strictness 71–100 (Exact): override LLM → require exact string match לstring comparison
  - עבור Strictness 71–100: בנוסף ל-LLM, בצע `StringSimilarityService.ComputeSimilarity(studentAnswer, correctAnswer)` ← Levenshtein distance
  - אם LLM ועstring similarity לא מסכימים ב-strictness > 70 → flag `needs_review = true`

- צור `ScoreCalculationService`:
  - `CalculateQuestionScore(gradingResult, maxPoints) → decimal`
  - `CalculateTotalScore(questionGrades) → TotalScore`
  - `CalculatePercentage(total, max) → decimal`

- צור `GradingAnnotationBuilder`:
  - Input: question grade + extracted answer bounding box
  - Output: `GradingAnnotation { symbol: "✓" | "-{X}", label: "מלא" | "{X} נקודות", color: "green" | "red", boundingBox }`

**קריטריוני קבלה:**
- [ ] Strictness 0 מקבל תשובות דומות
- [ ] Strictness 100 דורש התאמה מדויקת
- [ ] Score נחשב נכון
- [ ] Annotations נוצרות עם בounding boxes

---

## T-046 | S | BE
### כותרת: Grading Worker — Persist Results + Notify Teacher
**תלויות:** T-045
**Layer:** BE

**מה לעשות בדיוק:**
- לאחר grading כל השאלות:
  1. Insert `grading.student_grades` עם totalScore, percentage
  2. Insert כל `grading.question_grades` עם annotations
  3. Update `ocr.student_exams.status` → `graded`
  4. Publish `GradingCompletedEvent`:
     - Notification worker שולח email לteacher: "בדיקת '{שם תלמיד}' הושלמה"
  5. אם needs_review = true על >30% מהשאלות → send special "review required" email

- צור `GET /api/v1/grades/{studentExamId}` ב-Grading API:
  - Return `StudentGrade` מלא עם כל `QuestionGrade`
  - Include: original exam image URLs (presigned), annotations, teacher overrides

- צור `GET /api/v1/exams/{examId}/grades`:
  - Return כל grades לexam
  - Filter: status (ai_graded, under_review, approved)

**קריטריוני קבלה:**
- [ ] Grades נשמרים
- [ ] Email נשלח לteacher
- [ ] API מחזיר grades עם annotations
- [ ] needs_review שאלות מסומנות

---

# ══════════════════════════════════════════
# EPIC 6 — TEACHER REVIEW — BACKEND
# ══════════════════════════════════════════

---

## T-047 | S | BE
### כותרת: Grading API — Override Grade + Add Comment Endpoints
**תלויות:** T-046
**Layer:** BE

**מה לעשות בדיוק:**
- צור `PUT /api/v1/grades/questions/{questionGradeId}/override`:
  - Request: `{ "newScore": float, "newIsCorrect": bool, "comment": "string" }`
  - Validations: newScore >= 0, newScore <= maxScore
  - Logic:
    1. בדוק ownership: teacher owns this exam
    2. Insert ל-`grading.teacher_overrides`
    3. Update `question_grades.score` ל-newScore (לא מוחק AI grade)
    4. Recalculate total_score ב-`student_grades`
    5. Insert audit log עם old + new values
    6. Mark `student_grades.status` → `under_review`
  - Response 200 עם updated grade

- צור `POST /api/v1/grades/{studentGradeId}/comments`:
  - Request: `{ "comment": "string" }`
  - Insert ל-`grading.teacher_comments`
  - Audit log

- צור `GET /api/v1/grades/{studentGradeId}/history`:
  - Return: all overrides + comments + original AI grades
  - Sorted by created_at

**קריטריוני קבלה:**
- [ ] Override שומר original AI grade
- [ ] Total score מחושב מחדש
- [ ] Audit trail מלא
- [ ] Teacher רק מסוגל לoverride exams שלו

---

## T-048 | S | BE
### כותרת: Grading API — Approve Grade + Audit Service
**תלויות:** T-047
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/grades/{studentGradeId}/approve`:
  - Validate: teacher owns exam
  - Validate: status != already approved
  - Update `student_grades.status` → `approved`
  - Update `student_grades.reviewed_by` = currentUserId
  - Update `student_grades.approved_at` = NOW()
  - Update `ocr.student_exams.status` → `approved`
  - Publish `GradeApprovedEvent`
  - Audit log
  - Response 200

- צור `AuditService` (Shared):
  - `LogAsync(userId, action, resourceType, resourceId, oldValue, newValue)`
  - Insert ל-`audit.audit_logs`
  - Also publish to Elasticsearch (for admin search)
  - בnon-blocking async (don't fail main operation if audit fails)

- Actions to audit: LOGIN, LOGOUT, EXAM_CREATED, TEMPLATE_UPLOADED, GRADE_OVERRIDDEN, GRADE_APPROVED, BILLING_PAYMENT, USER_SUSPENDED

**קריטריוני קבלה:**
- [ ] Approve לוקח status ל-approved
- [ ] Audit log נכתב לכל action
- [ ] Elasticsearch indexed
- [ ] לא ניתן לapprove twice

---

# ══════════════════════════════════════════
# EPIC 6 — TEACHER REVIEW — FRONTEND
# ══════════════════════════════════════════

---

## T-049 | M | FE
### כותרת: Frontend — Grade Review Page with Annotated Exam Viewer
**תלויות:** T-024, T-046, T-047
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/grading/pages/grade-review/grade-review.component.ts` ב-Route `/grades/{studentExamId}/review`
- Layout: split-screen
  - **Left (60%)**: Annotated Exam Viewer
    - הצג מבחן מקורי (image)
    - Overlay annotations מה-AI: ✓ בירוק / −X ב-אדום ליד כל תשובה
    - Zoom in/out
    - Page navigation (prev/next)
    - Click על annotation → scroll ל-details בצד ימין
  - **Right (40%)**: Grade Panel
    - Student info: שם, ID
    - Total score: X / 100 (גדול, בולט)
    - Percentage + Pass/Fail badge
    - Questions list: כל שאלה עם:
      - ✓ / ✗ icon
      - Score: X/Y
      - AI explanation (בעברית)
      - "שנה ציון" כפתור
- Override Modal (mat-dialog):
  - Input: new score (number)
  - Toggle: נכון / לא נכון
  - Textarea: הערת מורה
  - Submit → PUT /api/v1/grades/questions/{id}/override
- Comments section בתחתית
- **"אשר ציון סופי"** כפתור → confirmation dialog → POST approve

**קריטריוני קבלה:**
- [ ] Annotations מוצגות על exam image
- [ ] Override עובד
- [ ] Approve עובד
- [ ] Mobile friendly (stack vertical)

---

## T-050 | S | FE
### כותרת: Frontend — Exam Grades Dashboard (All Students)
**תלויות:** T-049
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/grading/pages/grades-list/grades-list.component.ts` ב-Route `/exams/{id}/grades`
- Table columns: שם תלמיד, ציון, אחוז, סטטוס, פעולות
- Status filter: AI Graded / בבדיקה / אושר
- Sort by: ציון, שם, סטטוס
- Row click → navigate ל-review page
- Bulk actions:
  - Select all approved → batch export PDF
  - הצג summary: ממוצע, חציון, עברו/נכשלו
- Color coding: ≥60 ירוק, 50-59 כתום, <50 אדום

**קריטריוני קבלה:**
- [ ] All students grades מוצגים
- [ ] Filter ו-sort עובדים
- [ ] Color coding לציונים
- [ ] Bulk export trigger

---

# ══════════════════════════════════════════
# EPIC 7 — EXPORT — BACKEND
# ══════════════════════════════════════════

---

## T-051 | M | BE
### כותרת: Export Worker — PDF Generation with Annotations
**תלויות:** T-046, T-048
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Export.Worker` (.NET 9 Worker Service)
- התקן `QuestPDF` NuGet
- צור `ExamPdfGeneratorService`:
  - `GenerateStudentReportAsync(studentGradeId) → Stream`
  - PDF structure:
    1. **Page 1 — Cover**: שם תלמיד, שם מבחן, תאריך, ציון סופי (גדול), אחוז, Pass/Fail
    2. **Pages 2+**: כל page של המבחן המקורי (תמונה) + annotations overlay:
       - Draw ✓ (ירוק) ליד תשובות נכונות
       - Draw "−X" (אדום) ליד תשובות שגויות
       - Font שתומך בעברית (Heebo/Rubik מ-Google Fonts)
    3. **Final Page**: Summary table: שאלה, ניקוד מקסימלי, ניקוד שהתקבל, הערת AI, הערת מורה
    4. Footer בכל עמוד: תאריך, שם מורה, חותמת ExamAI

- `GenerateBatchReportAsync(examId) → Stream`:
  - Merge כל student PDFs לzip קובץ

- Annotations rendering:
  - שימוש ב-SkiaSharp לציור על images
  - RTL text rendering לעברית

**קריטריוני קבלה:**
- [ ] PDF נוצר עם תמונת המבחן
- [ ] Annotations מוצגות נכון
- [ ] Hebrew text ב-PDF
- [ ] Cover page עם ציון

---

## T-052 | S | BE
### כותרת: Export API — Export Endpoints
**תלויות:** T-051
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/grades/{studentGradeId}/export`:
  - Validate: grade exists + teacher ownership + status approved
  - Insert ל-`export.export_jobs`
  - Publish `ExportJobMessage` ל-RabbitMQ
  - Response 202: `{ exportJobId, status: "queued" }`

- צור `GET /api/v1/export/jobs/{exportJobId}/status`:
  - Return: status, progress

- צור `GET /api/v1/export/jobs/{exportJobId}/download`:
  - Validate: job completed
  - Generate presigned S3 URL (expire 15 דקות)
  - Return: `{ downloadUrl, expiresAt }`

- צור `POST /api/v1/exams/{examId}/export/batch`:
  - Export כל approved grades לexam זה כ-ZIP
  - Insert ל-`export.batch_export_jobs`

- Export Worker:
  - Consume מ-`export.jobs`
  - Generate PDF → Upload ל-S3: `{userId}/exports/{exportJobId}.pdf`
  - Update job status → `completed`
  - Notify teacher

**קריטריוני קבלה:**
- [ ] Export async (202 → poll → download)
- [ ] Presigned URL expire אחרי 15 דקות
- [ ] Batch ZIP עובד
- [ ] PDF נגיש להורדה

---

## T-053 | S | FE
### כותרת: Frontend — Export Buttons + Download Flow
**תלויות:** T-049, T-052
**Layer:** FE

**מה לעשות בדיוק:**
- הוסף לgrade review page: "ייצוא PDF" כפתור (רק כשstatus=approved)
- Export flow:
  1. POST /export → קבל exportJobId
  2. Poll /status כל 2 שניות
  3. הצג progress spinner: "מכין PDF..."
  4. כשcompleted → auto-download (window.open(downloadUrl))
- Batch export בgrades list: "ייצוא כל הציונים"
  - Progress modal עם percentage
- אם export נכשל → הצג שגיאה + retry button

**קריטריוני קבלה:**
- [ ] Single PDF download עובד
- [ ] Progress spinner מוצג
- [ ] Auto-download כשמוכן
- [ ] Retry עובד

---

# ══════════════════════════════════════════
# EPIC 8 — ANALYTICS — BACKEND
# ══════════════════════════════════════════

---

## T-054 | S | BE
### כותרת: Analytics API — Calculate and Store Exam Analytics
**תלויות:** T-046
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Analytics.API` project
- צור `AnalyticsCalculationService`:
  - `CalculateExamAnalyticsAsync(examId)`:
    - Load כל approved student_grades לexam
    - חשב: average, median, std deviation, min, max, pass rate (>=60%)
    - חשב distribution: bucket ל-10-point intervals (0-10, 11-20, ..., 91-100)
    - Upsert ל-`analytics.exam_analytics`
  - `CalculateQuestionAnalyticsAsync(examId)`:
    - Per question: correct_count, incorrect_count, average_score, difficulty_index
    - Common mistakes: קרא explanations מGPT grades → group similar errors
    - Upsert ל-`analytics.question_analytics`
  - Trigger: כשgrade approved → publish `AnalyticsUpdateRequiredEvent` → debounce 30 שניות → recalculate

- צור `GET /api/v1/analytics/exams/{examId}`:
  - Return exam_analytics + question_analytics
  - Cache ב-Redis 5 דקות

- צור `GET /api/v1/analytics/exams/{examId}/recommendations`:
  - שלח analytics data לGPT-4o → קבל recommendations בעברית
  - Cache 1 שעה (יקר)

**קריטריוני קבלה:**
- [ ] Analytics מחושבים נכון
- [ ] Distribution נכון
- [ ] GPT recommendations בעברית
- [ ] Cache פועל

---

# ══════════════════════════════════════════
# EPIC 8 — ANALYTICS — FRONTEND
# ══════════════════════════════════════════

---

## T-055 | M | FE
### כותרת: Frontend — Class Analytics Dashboard
**תלויות:** T-024, T-054
**Layer:** FE

**מה לעשות בדיוק:**
- צור `features/analytics/pages/analytics/analytics.component.ts` ב-Route `/exams/{id}/analytics`
- Layout: dashboard עם cards וcharts
- **Summary Cards** (top row):
  - ממוצע כיתה: XX
  - חציון: XX
  - עברו: XX / YY (ZZ%)
  - ציון גבוה ביותר / נמוך ביותר

- **Score Distribution Chart** (bar chart, Chart.js):
  - X axis: ranges (0-10, 11-20, ...)
  - Y axis: מספר תלמידים
  - Bar צבע: אדום <60, כתום 60-79, ירוק ≥80
  - Hover tooltip: "X תלמידים"

- **Question Performance Chart** (horizontal bar):
  - Y axis: מספר שאלה
  - X axis: % תשובות נכונות
  - Color: אדום (<50%), כתום (50-79%), ירוק (≥80%)
  - Sorted: מהקשה לקלה

- **AI Recommendations Section**:
  - בועות / cards עם המלצות בעברית
  - "שאלה 3 הייתה הכי קשה — שקול לחזור על הנושא"

- **Common Mistakes Table**:
  - Per question: top 3 שגיאות נפוצות

**קריטריוני קבלה:**
- [ ] כל charts מוצגים
- [ ] Data בעברית
- [ ] Charts responsive
- [ ] Recommendations מוצגות

---

# ══════════════════════════════════════════
# EPIC 9 — NOTIFICATION WORKER
# ══════════════════════════════════════════

---

## T-056 | S | BE
### כותרת: Notification Worker — SendGrid Email Templates
**תלויות:** T-005
**Layer:** BE

**מה לעשות בדיוק:**
- צור `ExamAI.Notification.Worker` (.NET 9 Worker Service)
- התקן `SendGrid` NuGet
- צור `EmailService`:
  - `SendAsync(to, subject, templateId, templateData)`
  - Templates ב-SendGrid Dashboard (Dynamic Templates):
    - `VERIFY_EMAIL`: subject, verification link
    - `PASSWORD_RESET`: reset link
    - `GRADING_COMPLETED`: student name, exam name, score, review link
    - `INVOICE_RECEIPT`: amount, pages count, invoice number, download link
    - `PAYMENT_FAILED`: amount, update payment link
    - `OCR_FAILED`: student name, exam name, retry link
    - `LOW_CONFIDENCE`: student name, questions count requiring review
  - כל templates בעברית
  - Unsubscribe link בכל email (GDPR)

- Consume מ-`notification.jobs` queue
- Handle events:
  - `UserRegisteredEvent` → VERIFY_EMAIL
  - `PasswordResetRequestedEvent` → PASSWORD_RESET
  - `GradingCompletedEvent` → GRADING_COMPLETED
  - `InvoicePaidEvent` → INVOICE_RECEIPT
  - `PaymentFailedEvent` → PAYMENT_FAILED

**קריטריוני קבלה:**
- [ ] כל email types נשלחים
- [ ] Templates בעברית
- [ ] Unsubscribe link קיים
- [ ] Failed send → retry 3 times

---

# ══════════════════════════════════════════
# EPIC 9 — ADMIN PANEL — BACKEND
# ══════════════════════════════════════════

---

## T-057 | S | BE
### כותרת: Admin API — User Management Endpoints
**תלויות:** T-019
**Layer:** BE

**מה לעשות בדיוק:**
- כל admin endpoints: `[Authorize(Policy = "CanAccessAdmin")]`
- צור `GET /api/v1/admin/users`:
  - Query: search (email/name), role filter, status filter, page, pageSize
  - Response: paginated users list עם subscription info

- צור `GET /api/v1/admin/users/{id}`:
  - Full user profile + subscription + usage + recent audit logs

- צור `PUT /api/v1/admin/users/{id}/status`:
  - Request: `{ "status": "suspended" | "active", "reason": "string" }`
  - Update user status
  - אם suspended → revoke all sessions
  - Audit log

- צור `PUT /api/v1/admin/users/{id}/role`:
  - Request: `{ "role": "admin" | "teacher" }`
  - Update roles

- צור `DELETE /api/v1/admin/users/{id}`:
  - Hard delete (GDPR compliance)
  - Delete all personal data
  - Keep anonymized audit logs

**קריטריוני קבלה:**
- [ ] Admin-only access
- [ ] User status change revokes sessions
- [ ] Hard delete removes personal data

---

## T-058 | S | BE
### כותרת: Admin API — System Configuration: OCR Models + AI Prompts
**תלויות:** T-019, T-044
**Layer:** BE

**מה לעשות בדיוק:**
- צור `system_configs` table ב-`admin` schema:
  ```sql
  CREATE TABLE admin.system_configs (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT NOT NULL,
    description TEXT,
    updated_by UUID,
    updated_at TIMESTAMPTZ DEFAULT NOW()
  );
  ```

- צור `GET /api/v1/admin/config`:
  - Return כל configs (except secrets)

- צור `PUT /api/v1/admin/config/{key}`:
  - Update value
  - Audit log
  - Invalidate Redis cache

- Config keys:
  - `ocr.primary_model` (azure_di / custom)
  - `ocr.confidence_threshold` (default: 0.65)
  - `grading.primary_model` (gpt-4o / gpt-4-turbo)
  - `grading.prompt_template` (הprompt שמשמש לgrading)
  - `grading.fallback_model`
  - `billing.free_pages_limit` (default: 25)

- הגרסה Worker/Service להשתמש ב-`ISystemConfigService` עם Redis cache (TTL 5 min)

**קריטריוני קבלה:**
- [ ] Admin יכול לשנות prompts
- [ ] Prompt changes מיד בתוקף (cache invalidation)
- [ ] Audit log לכל שינוי config

---

## T-059 | S | BE
### כותרת: Admin API — Logs + Audit Trail Endpoints
**תלויות:** T-048, T-006
**Layer:** BE

**מה לעשות בדיוק:**
- צור `GET /api/v1/admin/audit-logs`:
  - Query: userId, action, resourceType, dateFrom, dateTo, page, pageSize
  - Search ב-Elasticsearch (index: `examai-audit-*`)
  - Response: paginated audit logs

- צור `GET /api/v1/admin/system-logs`:
  - Query Seq API לlogs
  - Filter: level (error/warning/info), service, dateRange, searchText
  - Proxy לSeq REST API

- צור `GET /api/v1/admin/metrics/dashboard`:
  - Return:
    - Total users, active users (30 days)
    - Total exams, pages processed (month)
    - Revenue (month)
    - Error rate (from Elasticsearch logs)
    - Average OCR confidence
    - Average grading confidence

**קריטריוני קבלה:**
- [ ] Audit search עובד עם Elasticsearch
- [ ] System logs מוצגים
- [ ] Dashboard metrics נכונים

---

# ══════════════════════════════════════════
# EPIC 9 — ADMIN PANEL — FRONTEND
# ══════════════════════════════════════════

---

## T-060 | M | FE
### כותרת: Frontend — Admin Panel Layout + User Management Page
**תלויות:** T-024, T-057
**Layer:** FE

**מה לעשות בדיוק:**
- Admin guard: redirect לdashboard אם לא admin
- Route: `/admin` עם admin sidebar
- **User Management** (`/admin/users`):
  - Table: email, שם, תפקיד, סטטוס, plan, הצטרף, פעולות
  - Search field, role/status filters
  - Row actions: השהה / הפעל, שנה תפקיד, מחק
  - Confirm dialog לפעולות הרסניות
  - User detail slide-out panel: כל פרטי משתמש + usage + audit history

- **Metrics Dashboard** (`/admin/dashboard`):
  - Summary cards: משתמשים, מבחנים, עמודים, הכנסה
  - Chart: registrations over time
  - Chart: pages processed per day
  - Error rate gauge

**קריטריוני קבלה:**
- [ ] Admin guard עובד
- [ ] User management מלא
- [ ] Metrics מוצגים
- [ ] Confirm dialogs להרסני

---

## T-061 | S | FE
### כותרת: Frontend — Admin Config Editor + Audit Log Viewer
**תלויות:** T-058, T-059
**Layer:** FE

**מה לעשות בדיוק:**
- **System Config** (`/admin/config`):
  - Key-value table
  - Inline edit עם save
  - Prompt template: textarea (code editor style)
  - Confidence threshold: slider
  - Save → confirmation "שינוי זה ישפיע על כל הבדיקות החדשות"

- **Audit Log** (`/admin/audit`):
  - Table: זמן, משתמש, פעולה, resource, IP
  - Filters: תאריך, משתמש, פעולה
  - Click row → JSON diff viewer (old vs new value)
  - Export to CSV

**קריטריוני קבלה:**
- [ ] Config editable
- [ ] Audit log מוצג
- [ ] JSON diff מוצג
- [ ] Export עובד

---

# ══════════════════════════════════════════
# EPIC 10 — SECURITY HARDENING
# ══════════════════════════════════════════

---

## T-062 | S | BE
### כותרת: Security — Input Validation + SQL Injection + XSS Protection
**תלויות:** T-003
**Layer:** BE

**מה לעשות בדיוק:**
- **SQL Injection**: EF Core parameterized queries בכל מקום. אסור `FromSqlRaw` עם user input ישירות. Code review checklist.
- **XSS**:
  - הוסף `HtmlSanitizer` NuGet לכל fields שמוצגים ב-frontend (comments, names)
  - Response headers: `Content-Security-Policy: default-src 'self'`
  - Angular: `DomSanitizer` ב-pipe לכל untrusted HTML
- **Input Validation**:
  - FluentValidation על כל API requests
  - Max lengths על כל string fields
  - Reject null bytes (\x00) בכל string input
  - File name sanitization: strip path traversal (`../`, `./`)
- **CSRF**:
  - Angular: מוסיף XSRF header אוטומטית (default)
  - Backend: validate `X-XSRF-TOKEN` על state-changing requests
  - SameSite=Strict על cookies

**קריטריוני קבלה:**
- [ ] OWASP ZAP scan עובר
- [ ] No raw SQL queries
- [ ] XSS sanitization בplace
- [ ] CSRF protection

---

## T-063 | S | BE
### כותרת: Security — Encryption at Rest + File Security
**תלויות:** T-032
**Layer:** BE

**מה לעשות בדיוק:**
- **S3 Encryption**:
  - Enable SSE-S3 על bucket (server-side encryption)
  - Bucket policy: `Deny` אם לא encrypted
  - Presigned URLs: max 15 minutes expiry
  - Bucket: private (no public access)

- **Database Encryption**:
  - PostgreSQL: Transparent Data Encryption ב-production (cloud provider)
  - Sensitive fields (token hashes): stored as SHA-256, never plaintext

- **Redis Encryption**:
  - TLS transport ב-production
  - Sensitive cache keys: prefix with `sec:` + encrypt value

- **Secrets Management**:
  - Development: `dotnet user-secrets`
  - Production: HashiCorp Vault / AWS Secrets Manager
  - אסור secrets ב-code או config files ב-Git

- **File Access Control**:
  - Validate user owns file before serving presigned URL
  - File paths include userId (prevent IDOR)

**קריטריוני קבלה:**
- [ ] S3 bucket private
- [ ] Presigned URLs work but expire
- [ ] No secrets in Git
- [ ] IDOR not possible via file paths

---

## T-064 | S | BE
### כותרת: Security — GDPR Compliance Endpoints
**תלויות:** T-003, T-019
**Layer:** BE

**מה לעשות בדיוק:**
- צור `POST /api/v1/gdpr/export-my-data`:
  - Compile כל data על user:
    - Profile, exams, grades, billing history, audit logs
  - Generate JSON file
  - Upload ל-S3 (private)
  - שלח email עם download link (expire 48 שעות)
  - Response: 202 Accepted

- צור `DELETE /api/v1/gdpr/delete-my-account`:
  - Request: `{ "confirmEmail": "string", "reason": "string" }`
  - Validate email matches
  - Schedule deletion (30 יום delay — grace period)
  - Mark account as `deletion_scheduled`
  - שלח email עם confirmation + cancel link
  - After 30 days: hard delete (anonymize audit logs, delete files)

- צור `PUT /api/v1/gdpr/consent`:
  - Update marketing consent
  - Log consent change ב-audit

- הוסף Privacy Policy + Terms links לRegistration

**קריטריוני קבלה:**
- [ ] Data export עובד
- [ ] Account deletion עם 30-day grace period
- [ ] Audit logs anonymized לאחר deletion
- [ ] Consent tracked

---

# ══════════════════════════════════════════
# EPIC 11 — KUBERNETES & CI/CD
# ══════════════════════════════════════════

---

## T-065 | S | INFRA
### כותרת: Dockerfiles for All Services
**תלויות:** T-003, T-008
**Layer:** INFRA

**מה לעשות בדיוק:**
- צור Dockerfile לכל service (multi-stage build):
  ```dockerfile
  # Stage 1: Build
  FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
  WORKDIR /src
  COPY . .
  RUN dotnet publish -c Release -o /app

  # Stage 2: Runtime
  FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
  WORKDIR /app
  COPY --from=build /app .
  USER app  # non-root user
  ENTRYPOINT ["dotnet", "ExamAI.*.dll"]
  ```
- .NET APIs: expose port 8080
- Workers: no port
- Angular:
  ```dockerfile
  FROM node:22-alpine AS build
  COPY . .
  RUN npm ci && ng build --configuration=production
  FROM nginx:alpine
  COPY --from=build /app/dist /usr/share/nginx/html
  COPY nginx.conf /etc/nginx/conf.d/default.conf
  ```
- `.dockerignore` לכל project
- Non-root user בכל container
- Health check בכל Dockerfile

**קריטריוני קבלה:**
- [ ] כל images build בהצלחה
- [ ] Non-root user
- [ ] Health checks מוגדרים
- [ ] Image size < 200MB

---

## T-066 | M | INFRA
### כותרת: Kubernetes Manifests for All Services
**תלויות:** T-065
**Layer:** INFRA

**מה לעשות בדיוק:**
- צור `infra/k8s/` עם structure:
  ```
  k8s/
  ├── base/
  │   ├── namespace.yaml
  │   ├── identity-api/
  │   │   ├── deployment.yaml
  │   │   ├── service.yaml
  │   │   └── hpa.yaml
  │   ├── exam-api/
  │   ├── ocr-worker/
  │   ├── grading-worker/
  │   ├── export-worker/
  │   ├── notification-worker/
  │   ├── billing-api/
  │   ├── analytics-api/
  │   ├── admin-api/
  │   ├── gateway/
  │   └── frontend/
  └── overlays/
      ├── staging/
      └── production/
  ```
- כל Deployment:
  - `replicas: 2`
  - Resource limits: CPU 500m, Memory 512Mi
  - Liveness probe: GET /health
  - Readiness probe: GET /health/ready
  - `rollingUpdate: maxUnavailable: 0, maxSurge: 1`
  - Env vars from ConfigMap + Secret
- Workers: `replicas: 2`, no service needed
- HPA לכל API: min=2, max=10, CPU threshold=70%
- OCR/Grading Workers HPA: scale based on RabbitMQ queue depth (KEDA)

**קריטריוני קבלה:**
- [ ] כל services deploy בK8s
- [ ] Health checks מוגדרים
- [ ] HPA פועל
- [ ] Zero-downtime rolling update

---

## T-067 | S | INFRA
### כותרת: GitHub Actions CD Pipeline — Build, Push, Deploy
**תלויות:** T-065, T-066
**Layer:** INFRA

**מה לעשות בדיוק:**
- עדכן `.github/workflows/cd.yml`:
  1. Trigger: push ל-`main`
  2. Build Docker images לכל service
  3. Tag: `{registry}/{service}:{git-sha}-{date}`
  4. Push ל-registry (Docker Hub / ECR / GCR)
  5. Update K8s image tags: `kubectl set image deployment/{svc} {svc}={tag}`
  6. Wait for rollout: `kubectl rollout status deployment/{svc}`
  7. Run smoke tests
  8. Slack notification: success/failure
- Secrets ב-GitHub Actions:
  - `REGISTRY_TOKEN`
  - `KUBE_CONFIG`
  - `SLACK_WEBHOOK`
- Staging deploy: automatic (כל push ל-develop)
- Production deploy: manual approval gate

**קריטריוני קבלה:**
- [ ] CD מ-main → K8s אוטומטי
- [ ] Manual approval לproduction
- [ ] Rollback אוטומטי אם smoke tests נכשלים
- [ ] Slack notification

---
Claude couldn't finish this response. Try again in a moment.

You are out of free messages until 11:00 PM
Get more
