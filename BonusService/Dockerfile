FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
COPY . .
WORKDIR BonusService
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 50997

ENTRYPOINT ["dotnet", "BonusService.dll"]