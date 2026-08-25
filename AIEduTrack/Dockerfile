# Базовый образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Копируем сертификаты Минцифры (для работы с GigaChat)
# Убедись, что папка Certificates с файлами .cer скопирована в проект AIEduTrack!
COPY ["AIEduTrack/Certificates/russian_trusted_root_ca.cer", "/usr/local/share/ca-certificates/russian_trusted_root_ca.crt"]
COPY ["AIEduTrack/Certificates/russian_trusted_sub_ca.cer", "/usr/local/share/ca-certificates/russian_trusted_sub_ca.crt"]
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

USER $APP_UID

# Образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AIEduTrack/AIEduTrack.csproj", "AIEduTrack/"]
RUN dotnet restore "./AIEduTrack/AIEduTrack.csproj"
COPY . .
WORKDIR "/src/AIEduTrack"
RUN dotnet build "./AIEduTrack.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AIEduTrack.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальная сборка
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Если вы используете SQLite или локальные JSON-файлы, выдаем права на запись
USER root
RUN chown -R app:app /app

USER app
ENTRYPOINT ["dotnet", "AIEduTrack.dll"]