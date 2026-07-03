# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching for restore.
COPY IdealWeightNutrition.Modernized.sln ./
COPY src/IdealWeightNutrition.Api/IdealWeightNutrition.Api.csproj src/IdealWeightNutrition.Api/
COPY src/IdealWeightNutrition.Application/IdealWeightNutrition.Application.csproj src/IdealWeightNutrition.Application/
COPY src/IdealWeightNutrition.Contracts/IdealWeightNutrition.Contracts.csproj src/IdealWeightNutrition.Contracts/
COPY src/IdealWeightNutrition.Domain/IdealWeightNutrition.Domain.csproj src/IdealWeightNutrition.Domain/
COPY src/IdealWeightNutrition.Infrastructure/IdealWeightNutrition.Infrastructure.csproj src/IdealWeightNutrition.Infrastructure/

RUN dotnet restore src/IdealWeightNutrition.Api/IdealWeightNutrition.Api.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish src/IdealWeightNutrition.Api/IdealWeightNutrition.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render (and most PaaS) inject the port to listen on via $PORT; fall back to 8080 locally.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet IdealWeightNutrition.Api.dll"]
