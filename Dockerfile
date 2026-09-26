# ---------- ETAPA 1: BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se restaura primero solo con los manifiestos para aprovechar la cache de Docker
COPY ParcialProgramacion.csproj ./
RUN dotnet restore ParcialProgramacion.csproj

COPY . .
RUN dotnet publish ParcialProgramacion.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- ETAPA 2: RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Render asigna la variable PORT. Kestrel debe escuchar en 0.0.0.0 para que
# el proxy de Render alcance la aplicacion. El valor por defecto cubre el
# docker run local; en Render lo sobrescribe la variable PORT real.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# Directorio donde SQLite persistira la base. En Render conviene montar un
# disco persistente en este mismo path (/var/data) para no perder los datos
# en cada reinicio del contenedor.
RUN mkdir -p /app/App_Data /var/data

EXPOSE 8080

COPY --from=build /app/publish .

# Usuario no root incluido en la imagen oficial de .NET
USER $APP_UID

ENTRYPOINT ["dotnet", "ParcialProgramacion.dll"]
