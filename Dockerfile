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
# APP_UID se fija de forma explicita (es el uid del usuario "app" de la imagen
# oficial de .NET) para no depender de que la imagen base lo exporte.
# ASPNETCORE_HTTP_PORTS solo aplica cuando PORT no esta definido (docker run
# local); en Render manda PORT y lo aplica Program.cs con UseUrls.
ENV APP_UID=1654 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# Los directorios de datos deben pertenecer al usuario no root. SQLite crea el
# archivo .db en /app/App_Data y EnsureCreated() fallaria con permisos de root.
RUN mkdir -p /app/App_Data /var/data \
    && chown -R ${APP_UID}:${APP_UID} /app /var/data

EXPOSE 8080

# --chown para que los archivos publicados pertenezcan tambien al usuario final
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/publish .

USER ${APP_UID}

ENTRYPOINT ["dotnet", "ParcialProgramacion.dll"]
