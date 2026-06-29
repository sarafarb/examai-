using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using ExamAI.Identity.API.Data;
using ExamAI.Identity.API.Dtos;
using ExamAI.Identity.API.Events;
using ExamAI.Identity.API.Models;
using ExamAI.Identity.API.Services;
using Google.Apis.Auth; // <── ה-using החסר שפותר את השגיאה!

namespace ExamAI.Identity.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. POST /api/v1/auth/register
        app.MapPost("/api/v1/auth/register", async (
            RegisterRequest request, IValidator<RegisterRequest> validator, IdentityDbContext dbContext, IConnection rabbitConnection) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid) return Results.UnprocessableEntity(validationResult.ToDictionary());

            var emailLower = request.Email.ToLower();
            if (await dbContext.Users.AnyAsync(u => u.Email.ToLower() == emailLower))
                return Results.BadRequest(new { error = "Email is already registered." });

            var user = new User { Email = request.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12), FirstName = request.FirstName, LastName = request.LastName };
            dbContext.Users.Add(user);
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, Role = "teacher" });
            dbContext.Subscriptions.Add(new Subscription { UserId = user.Id, Plan = "free" });

            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            var rawToken = Convert.ToHexString(randomBytes);
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            dbContext.EmailVerifications.Add(new EmailVerification { UserId = user.Id, TokenHash = tokenHash, ExpiresAt = DateTime.UtcNow.AddHours(24) });
            await dbContext.SaveChangesAsync();

            PublishToRabbit(rabbitConnection, "user-registered-queue", new UserRegisteredEvent(user.Id, user.Email, rawToken));
            return Results.Created($"/api/v1/auth/register", new RegisterResponse(user.Id, user.Email, "Please verify your email"));
        });

        // 2. POST /api/v1/auth/verify-email
        app.MapPost("/api/v1/auth/verify-email", async (VerifyEmailRequest request, IdentityDbContext dbContext, IConnection rabbitConnection) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest(new { error = "Token is required." });

            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
            var verification = await dbContext.EmailVerifications.FirstOrDefaultAsync(v => v.TokenHash == tokenHash);

            if (verification == null) return Results.BadRequest(new { error = "Invalid token." });
            if (verification.UsedAt != null) return Results.BadRequest(new { error = "Token has already been used." });
            if (verification.ExpiresAt < DateTime.UtcNow) return Results.BadRequest(new { error = "Token has expired." });

            verification.UsedAt = DateTime.UtcNow;

            var user = await dbContext.Users.FindAsync(verification.UserId);
            if (user != null)
            {
                user.EmailVerified = true;
                Console.WriteLine($"[AUDIT LOG] User {user.Id} verified email successfully.");
                PublishToRabbit(rabbitConnection, "email-verified-queue", new EmailVerifiedEvent(user.Id, user.Email));
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(new { message = "Email verified successfully" });
        });

        // 3. POST /api/v1/auth/resend-verification
        app.MapPost("/api/v1/auth/resend-verification", async (ResendVerificationRequest request, IdentityDbContext dbContext, IMemoryCache cache, IConnection rabbitConnection) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email)) return Results.BadRequest(new { error = "Email is required." });

            var emailLower = request.Email.ToLower();
            var cacheKey = $"rate_limit_resend_{emailLower}";

            // פתרון אזהרת ה-Null: חילוץ בטוח ווידוא שהרשימה לא ריקה
            if (!cache.TryGetValue(cacheKey, out List<DateTime>? cachedAttempts) || cachedAttempts == null)
            {
                cachedAttempts = new List<DateTime>();
            }

            // סינון בטוח של ניסיונות ישנים
            var attemptsList = cachedAttempts.Where(time => time > DateTime.UtcNow.AddMinutes(-10)).ToList();

            if (attemptsList.Count >= 3)
            {
                return Results.Json(new { error = "Rate limit exceeded. Please try again later." }, statusCode: 429);
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
            if (user == null) return Results.BadRequest(new { error = "User not found." });
            if (user.EmailVerified) return Results.BadRequest(new { error = "Email is already verified." });

            var oldVerifications = await dbContext.EmailVerifications.Where(v => v.UserId == user.Id && v.UsedAt == null).ToListAsync();
            foreach (var old in oldVerifications)
            {
                old.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            }

            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            var rawToken = Convert.ToHexString(randomBytes);
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            dbContext.EmailVerifications.Add(new EmailVerification { UserId = user.Id, TokenHash = tokenHash, ExpiresAt = DateTime.UtcNow.AddHours(24) });
            
            attemptsList.Add(DateTime.UtcNow);
            cache.Set(cacheKey, attemptsList, TimeSpan.FromMinutes(10));

            await dbContext.SaveChangesAsync();

            PublishToRabbit(rabbitConnection, "user-registered-queue", new VerificationResentEvent(user.Id, user.Email, rawToken));
            Console.WriteLine($"[AUDIT LOG] Verification token resent for user {user.Id}.");

            return Results.Ok(new { message = "Verification email resent successfully." });
        });

        // 4. POST /api/v1/auth/login
        app.MapPost("/api/v1/auth/login", async (
            LoginRequest request,
            IdentityDbContext dbContext,
            ITokenService tokenService,
            IDistributedCache cache,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var emailLower = request.Email.ToLower();
            var cacheKey = $"login_attempts:{emailLower}";

            var lockStatus = await cache.GetStringAsync(cacheKey);
            if (lockStatus == "locked")
            {
                return Results.Json(new { error = "Too many attempts. Account locked for 15 minutes." }, statusCode: 429);
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
            
            if (user == null || user.IsDeleted || user.IsSuspended)
            {
                await cache.SetStringAsync(cacheKey, "failed", new DistributedCacheEntryOptions()); 
                return Results.Json(new { error = "Invalid email or password." }, statusCode: 401);
            }

            if (!user.EmailVerified)
            {
                return Results.Json(new { error = "Email not verified. Please verify your email first." }, statusCode: 403);
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                var attemptsStr = await cache.GetStringAsync(cacheKey);
                int attempts = string.IsNullOrEmpty(attemptsStr) || attemptsStr == "failed" ? 0 : int.Parse(attemptsStr);
                attempts++;

                if (attempts >= 5)
                {
                    await cache.SetStringAsync(cacheKey, "locked", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                    });
                    return Results.Json(new { error = "Too many attempts. Account locked for 15 minutes." }, statusCode: 429);
                }
                else
                {
                    await cache.SetStringAsync(cacheKey, attempts.ToString(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                    });
                }

                return Results.Json(new { error = "Invalid email or password." }, statusCode: 401);
            }

            await cache.RemoveAsync(cacheKey);

            var roles = new[] { "teacher" }; 
            var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var refreshToken = tokenService.GenerateRefreshToken();

            var refreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            dbContext.Sessions.Add(session);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"[AUDIT LOG] User {user.Id} logged in successfully from IP: {ipAddress}.");

            var responseUser = new LoginResponseUser(user.Id, user.Email, user.FirstName, roles);
            return Results.Ok(new LoginResponse(accessToken, refreshToken, 900, responseUser));
        });
        // 5. POST /api/v1/auth/google
        app.MapPost("/api/v1/auth/google", async (
            GoogleLoginRequest request,
            IdentityDbContext dbContext,
            ITokenService tokenService,
            IConfiguration configuration,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return Results.BadRequest(new { error = "Google ID token is required." });

            var googleClientId = configuration["GoogleSettings:ClientId"];
            GoogleJsonWebSignature.Payload payload;

            // א. אימות ה-Token מול שרתי Google
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GOOGLE AUTH ERROR] Token validation failed: {ex.Message}");
                return Results.Json(new { error = "Invalid Google token." }, statusCode: 401);
            }

            // ב. חילוץ הפרטים מתוך ה-Payload
            var googleId = payload.Subject; // ה-ID הייחודי של המשתמש בגוגל
            var email = payload.Email;
            var firstName = payload.GivenName ?? payload.Name ?? "Google";
            var lastName = payload.FamilyName ?? "User";

            // ג. חיפוש משתמש תחילה לפי google_id, ואז לפי email
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // אם לא נמצא לפי Google ID, נחפש לפי המייל (אולי נרשם בעבר ידנית)
                user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user != null)
                {
                    // המשתמש קיים - נקשר את ה-Google ID לחשבון הקיים (Link Account)
                    user.GoogleId = googleId;
                    if (!user.EmailVerified) user.EmailVerified = true; // גוגל כבר אימתה את המייל
                }
                else
                {
                    // המשתמש לא קיים בכלל - רישום אוטומטי (Just-In-Time Provisioning)
                    user = new User
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        EmailVerified = true, // מסומן אוטומטית כמאומת
                        GoogleId = googleId,
                        PasswordHash = null   // אין סיסמה מקומית
                    };
                    dbContext.Users.Add(user);
                    
                    // הוספת רול ומנוי ברירת מחדל
                    dbContext.UserRoles.Add(new UserRole { UserId = user.Id, Role = "teacher" });
                    dbContext.Subscriptions.Add(new Subscription { UserId = user.Id, Plan = "free" });
                }

                await dbContext.SaveChangesAsync();
            }

            // ד. בדיקת חסימות (למקרה שהמשתמש הקיים מושעה/מחוק)
            if (user.IsDeleted || user.IsSuspended)
            {
                return Results.Json(new { error = "Account is suspended or deleted." }, statusCode: 401);
            }

            // ה. הפקת טוקנים וניהול סשן (בדיוק כמו ב-Login הרגיל - חתימה מתוקנת!)
            var roles = new[] { "teacher" }; 
            var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var refreshToken = tokenService.GenerateRefreshToken();

            var refreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            dbContext.Sessions.Add(session);
            await dbContext.SaveChangesAsync();

            // ו. Audit Log וסיום
            Console.WriteLine($"[AUDIT LOG] User {user.Id} logged in via Google OAuth from IP: {ipAddress}.");

            var responseUser = new LoginResponseUser(user.Id, user.Email, user.FirstName, roles);
            return Results.Ok(new LoginResponse(accessToken, refreshToken, 900, responseUser));
        });
        // 6. POST /api/v1/auth/refresh (Refresh Token Rotation)
        app.MapPost("/api/v1/auth/refresh", async (
            RefreshRequest request,
            IdentityDbContext dbContext,
            ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.BadRequest(new { error = "Refresh token is required." });

            // א. Hash של ה-Token שהתקבל לצורך השוואה ב-DB
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
            
            // ב. חיפוש הסשן בבסיס הנתונים
            var session = await dbContext.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash);

            // ג. בדיקות תקינות: קיים, לא בוטל, לא פג תוקף
            if (session == null || session.RevokedAt != null || session.ExpiresAt < DateTime.UtcNow)
            {
                return Results.Json(new { error = "Invalid or expired refresh token." }, statusCode: 401);
            }

            // ד. בדיקה שהמשתמש עדיין פעיל במערכת
            var user = await dbContext.Users.FindAsync(session.UserId);
            if (user == null || user.IsDeleted || user.IsSuspended)
            {
                return Results.Json(new { error = "User account is inactive or not found." }, statusCode: 401);
            }

            // ה. ביטול ה-Token הנוכחי (Rotation)
            session.RevokedAt = DateTime.UtcNow;

            // ו. הנפקת זוג טוקנים חדש לחלוטין
            var roles = new[] { "teacher" }; 
            var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(newRefreshToken)));

            // ז. שמירת הסשן החדש ברצף השרשרת
            var newSession = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = newRefreshTokenHash,
                IpAddress = session.IpAddress,
                UserAgent = session.UserAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            dbContext.Sessions.Add(newSession);
            await dbContext.SaveChangesAsync();

            // ח. החזרת תשובה במבנה זהה ל-Login
            var responseUser = new LoginResponseUser(user.Id, user.Email, user.FirstName, roles);
            return Results.Ok(new LoginResponse(newAccessToken, newRefreshToken, 900, responseUser));
        });

        // 7. POST /api/v1/auth/logout
        app.MapPost("/api/v1/auth/logout", async (
            IdentityDbContext dbContext,
            IDistributedCache cache,
            HttpContext httpContext) =>
        {
            // א. חילוץ מזהה ה-jti, ה-expiration ומזהה המשתמש מתוך ה-Access Token הנוכחי
            var jti = httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                              ?? httpContext.User.FindFirst("sub")?.Value;

            // ב. הכנסת ה-Access Token ל-Blacklist ב-Redis עם TTL דינמי
            if (!string.IsNullOrEmpty(jti) && !string.IsNullOrEmpty(expClaim))
            {
                if (long.TryParse(expClaim, out long expUnix))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    var remainingLifetime = expTime - DateTime.UtcNow;

                    if (remainingLifetime > TimeSpan.Zero)
                    {
                        await cache.SetStringAsync($"blacklist:{jti}", "blacklisted", new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = remainingLifetime
                        });
                    }
                }
            }

            // ג. ביטול ה-Refresh Tokens הפעילים של המשתמש מה-DB
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out Guid userId))
            {
                var activeSessions = await dbContext.Sessions
                    .Where(s => s.UserId == userId && s.RevokedAt == null)
                    .ToListAsync();

                foreach (var s in activeSessions)
                {
                    s.RevokedAt = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync();
            }

            return Results.NoContent(); // Response 204
        }).RequireAuthorization(); // דורש Access Token תקף כדי לגשת
        // 8. POST /api/v1/auth/forgot-password
        app.MapPost("/api/v1/auth/forgot-password", async (
            ForgotPasswordRequest request,
            IdentityDbContext dbContext,
            IDistributedCache cache,
            IConnection rabbitConnection) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required." });

            var emailLower = request.Email.ToLower();
            var rateLimitKey = $"forgot_password_rate:{emailLower}";

            // א. בדיקת Rate Limit ב-Redis (מקסימום 3 בקשות בשעה)
            var attemptsStr = await cache.GetStringAsync(rateLimitKey);
            int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);

            if (attempts >= 3)
            {
                return Results.Json(new { error = "Too many password reset requests. Try again later." }, statusCode: 429);
            }

            // עדכון המונה בקש
            attempts++;
            await cache.SetStringAsync(rateLimitKey, attempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            // ב. חיפוש המשתמש - אם לא קיים, מחזירים 200 (מניעת Account Enumeration)
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower && !u.IsDeleted && !u.IsSuspended);
            if (user == null)
            {
                return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
            }

            // ג. יצירת טוקן איפוס מאובטח (32 בתים) ושמירת ה-Hash שלו
            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            var rawToken = Convert.ToHexString(randomBytes);
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // תקף לשעה אחת
            };

            dbContext.PasswordResets.Add(passwordReset);
            await dbContext.SaveChangesAsync();

            // ד. פרסום אירוע ל-RabbitMQ לשליחת המייל
            PublishToRabbit(rabbitConnection, "forgot-password-queue", new ForgotPasswordRequestedEvent(user.Id, user.Email, rawToken));

            return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
        });

        // 9. POST /api/v1/auth/reset-password
        app.MapPost("/api/v1/auth/reset-password", async (
            ResetPasswordRequest request,
            IdentityDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "Token and new password are required." });

            if (request.NewPassword != request.ConfirmPassword)
                return Results.BadRequest(new { error = "Passwords do not match." });

            if (request.NewPassword.Length < 6)
                return Results.BadRequest(new { error = "Password must be at least 6 characters long." });

            // א. הפקת Hash מהטוקן שהתקבל לצורך איתור ב-DB
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
            var resetRecord = await dbContext.PasswordResets.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

            // ב. ולידציה של רשומת האיפוס
            if (resetRecord == null || resetRecord.UsedAt != null || resetRecord.ExpiresAt < DateTime.UtcNow)
            {
                return Results.BadRequest(new { error = "Invalid, used, or expired reset token." });
            }

            var user = await dbContext.Users.FindAsync(resetRecord.UserId);
            if (user == null || user.IsDeleted || user.IsSuspended)
            {
                return Results.BadRequest(new { error = "Account is unavailable." });
            }

            // ג. עדכון הסיסמה החדשה באמצעות BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            resetRecord.UsedAt = DateTime.UtcNow;

            // ד. ביטול כל הסשנים הפעילים של המשתמש (אלץ Re-Login בכל המכשירים)
            var activeSessions = await dbContext.Sessions.Where(s => s.UserId == user.Id && s.RevokedAt == null).ToListAsync();
            foreach (var session in activeSessions)
            {
                session.RevokedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"[AUDIT LOG] User {user.Id} successfully reset their password. All active sessions revoked.");
            return Results.Ok(new { message = "Password has been successfully reset." });
        });

        // 10. GET /api/v1/auth/me (Get Current User Profile with Redis Cache)
        app.MapGet("/api/v1/auth/me", async (
            IdentityDbContext dbContext,
            IDistributedCache cache,
            HttpContext httpContext) =>
        {
            // א. חילוץ מזהה המשתמש מה-JWT
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                              ?? httpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Results.Json(new { error = "Unauthorized access." }, statusCode: 401);
            }

            var cacheKey = $"user:{userId}:profile";

            // ב. ניסיון שליפה מהקש (Redis Cache)
            var cachedProfile = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedProfile))
            {
                var responseFromCache = JsonSerializer.Deserialize<UserProfileResponse>(cachedProfile);
                if (responseFromCache != null) return Results.Ok(responseFromCache);
            }

            // ג. שליפה מציאותית מבסיס הנתונים (כולל רולים ומנוי)
            var user = await dbContext.Users.FindAsync(userId);
            if (user == null || user.IsDeleted || user.IsSuspended)
            {
                return Results.Json(new { error = "User not found or inactive." }, statusCode: 401);
            }

            var roles = await dbContext.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role).ToArrayAsync();
            if (roles.Length == 0) roles = new[] { "teacher" }; // Default Fallback

            var sub = await dbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            var subInfo = new SubscriptionInfoDto(sub?.Plan ?? "free", sub?.PagesUsed ?? 0, sub?.PagesLimit ?? 25);

            var profileResponse = new UserProfileResponse(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                null, // AvatarUrl placeholder
                user.EmailVerified,
                roles,
                subInfo
            );

            // ד. שמירה בקש למשך 5 דקות של שקט תעשייתי
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(profileResponse), cacheOptions);

            return Results.Ok(profileResponse);
        }).RequireAuthorization();
        // נקודות קצה ייעודיות לבדיקת מנגנון ה-RBAC וה-Policies בזמן אמת
        app.MapGet("/api/v1/exams/manage-test", () => Results.Ok("You have access to manage exams!"))
            .RequireAuthorization("CanManageExams");

        app.MapGet("/api/v1/admin/dashboard-test", () => Results.Ok("Welcome to the Admin Dashboard!"))
            .RequireAuthorization("CanAccessAdmin");
    }

    private static void PublishToRabbit<T>(IConnection connection, string queueName, T @event)
    {
        try
        {
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        }
        catch (Exception ex)
{
    Console.WriteLine($"[RABBIT ERROR] Failed to publish to {queueName}: {ex}");
}
    }
}