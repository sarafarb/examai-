# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["src/backend/ExamAI.Shared/ExamAI.Shared.csproj", "src/backend/ExamAI.Shared/"]
COPY ["src/backend/ExamAI.Workers.OCR/ExamAI.Workers.OCR.csproj", "src/backend/ExamAI.Workers.OCR/"]

RUN dotnet restore "src/backend/ExamAI.Workers.OCR/ExamAI.Workers.OCR.csproj"

COPY . .
WORKDIR "/src/src/backend/ExamAI.Workers.OCR"
RUN dotnet publish "ExamAI.Workers.OCR.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

USER app
# אין הגדרת פורט או Healthcheck מבוסס HTTP מכיוון שמדובר בקונטיינר רקע אל-קולי
ENTRYPOINT ["dotnet", "ExamAI.Workers.OCR.dll"]