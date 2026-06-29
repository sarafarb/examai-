@echo off
cls
echo =======================================================
echo  Launching the ENTIRE ExamAI Microservices Ecosystem! 🚀
echo =======================================================

:: --- CORE SERVICES ---
echo [1/10] Starting Gateway (Port 5176)...
start "ExamAI Gateway" dotnet run --project ExamAI.Gateway

echo [2/10] Starting Identity API (Port 5126)...
start "ExamAI Identity" dotnet run --project ExamAI.Identity.API

:: --- FEATURE APIs ---
echo [3/10] Starting Exam API...
start "ExamAI Exam API" dotnet run --project ExamAI.Exam.API

echo [4/10] Starting Admin API...
start "ExamAI Admin API" dotnet run --project ExamAI.Admin.API

echo [5/10] Starting Analytics API...
start "ExamAI Analytics API" dotnet run --project ExamAI.Analytics.API

echo [6/10] Starting Billing API...
start "ExamAI Billing API" dotnet run --project ExamAI.Billing.API

:: --- BACKGROUND WORKERS ---
echo [7/10] Starting OCR Worker...
start "ExamAI OCR Worker" dotnet run --project ExamAI.OCR.Worker

echo [8/10] Starting Grading Worker...
start "ExamAI Grading Worker" dotnet run --project ExamAI.Grading.Worker

echo [9/10] Starting Notification Worker...
start "ExamAI Notification Worker" dotnet run --project ExamAI.Notification.Worker

echo [10/10] Starting Export Worker...
start "ExamAI Export Worker" dotnet run --project ExamAI.Export.Worker

echo =======================================================
echo  All 10 services are spinning up in separate windows!
echo  Keep an eye on them for compilation or database errors.
echo =======================================================
pause