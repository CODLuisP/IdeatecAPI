# Etapa 1: SDK para build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar csproj y restaurar dependencias primero (capa cacheada)
COPY ["src/IdeatecAPI.API/IdeatecAPI.API.csproj", "src/IdeatecAPI.API/"]
COPY ["src/IdeatecAPI.Application/IdeatecAPI.Application.csproj", "src/IdeatecAPI.Application/"]
COPY ["src/IdeatecAPI.Domain/IdeatecAPI.Domain.csproj", "src/IdeatecAPI.Domain/"]
COPY ["src/IdeatecAPI.Infrastructure/IdeatecAPI.Infrastructure.csproj", "src/IdeatecAPI.Infrastructure/"]
RUN dotnet restore "src/IdeatecAPI.API/IdeatecAPI.API.csproj"

# Copiar todo el código y compilar
COPY . .
WORKDIR "/src/src/IdeatecAPI.API"
RUN dotnet build "IdeatecAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etapa 2: Publicar
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IdeatecAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime (imagen mínima)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Crear directorio para almacenamiento de XML
RUN mkdir -p /app/storage

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "IdeatecAPI.API.dll"]
