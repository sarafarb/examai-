# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# העתקת קבצי הפרוג'קט ופתרון תלויות מרכזי (Cache שכבה)
COPY ["src/backend/Directory.Packages.props", "./"]
COPY ["src/backend/ExamAI.Shared/ExamAI.Shared.csproj", "src/backend/ExamAI.Shared/"]
COPY ["src/backend/ExamAI.Grading.Worker/ExamAI.Grading.Worker.csproj", "src/backend/ExamAI.Grading.Worker/"]

# שים לב ששינינו כאן ל-Gateway
RUN dotnet restore "src/backend/ExamAI.Grading.Worker/ExamAI.Grading.Worker.csproj"

# העתקת שאר הקוד וביצוע פבליש
COPY . .
# שים לב ששינינו את תיקיית העבודה ל-Gateway
WORKDIR "/src/src/backend/ExamAI.Gateway"
RUN dotnet publish "ExamAI.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# התקנת curl לצורך בדיקות תקינות
RUN apk add --no-cache curl

# אבטחה: מעבר למשתמש non-root המובנה של דוטנט
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# בדיקת תקינות קונטיינר
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# שים לב ששינינו את ה-dll ל-Grading.Worker
ENTRYPOINT ["dotnet", "ExamAI.Grading.Worker.dll"]