@echo off
echo Starting API Gateway in Development mode...
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --launch-profile swagger