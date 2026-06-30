# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# העתקת קבצי הפרוג'קט ופתרון תלויות מרכזי
COPY ["src/backend/Directory.Packages.props", "./"]
COPY ["src/backend/ExamAI.Shared/ExamAI.Shared.csproj", "src/backend/ExamAI.Shared/"]
COPY ["src/backend/ExamAI.Identity.API/ExamAI.Identity.API.csproj", "src/backend/ExamAI.Identity.API/"]

RUN dotnet restore "src/backend/ExamAI.Identity.API/ExamAI.Identity.API.csproj"

# העתקת שאר הקוד וביצוע פבליש
COPY . .
WORKDIR "/src/src/backend/ExamAI.Identity.API"
RUN dotnet publish "ExamAI.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# התקנת curl לצורך בדיקות תקינות מתקדמות במידת הצורך
RUN apk add --no-cache curl

# אבטחה: מעבר למשתמש non-root המובנה של דוטנט
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# בדיקת תקינות קונטיינר
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ExamAI.Identity.API.dll"]