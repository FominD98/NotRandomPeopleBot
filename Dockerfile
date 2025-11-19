# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY AiTelegramBot/AiTelegramBot.csproj AiTelegramBot/
RUN dotnet restore "AiTelegramBot/AiTelegramBot.csproj"

# Copy everything else and build
COPY AiTelegramBot/ AiTelegramBot/
WORKDIR /src/AiTelegramBot
RUN dotnet build "AiTelegramBot.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AiTelegramBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AiTelegramBot.dll"]
