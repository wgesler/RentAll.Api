# Runtime image (includes Chromium already)
FROM mcr.microsoft.com/playwright/dotnet:v1.44.0-jammy AS base
WORKDIR /app
EXPOSE 8080

# Build stage (must match .NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RentAll.Api/RentAll.Api.csproj", "RentAll.Api/"]
COPY ["RentAll.Infrastructure/RentAll.Infrastructure.csproj", "RentAll.Infrastructure/"]
COPY ["RentAll.Domain/RentAll.Domain.csproj", "RentAll.Domain/"]

RUN dotnet restore "RentAll.Api/RentAll.Api.csproj"

COPY . .
WORKDIR "/src/RentAll.Api"
RUN dotnet publish "RentAll.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "RentAll.Api.dll"]
