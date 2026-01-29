# -------- BUILD --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Ferryx-Hub/Ferryx-Hub.csproj", "Ferryx-Hub/"]
RUN dotnet restore "Ferryx-Hub/Ferryx-Hub.csproj"

COPY . .
WORKDIR "/src/Ferryx-Hub"
RUN dotnet publish "Ferryx-Hub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# -------- RUNTIME --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Ferryx default port
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Config path (opsiyonel ama ileride kolaylýk)
RUN mkdir -p /etc/ferryx

# IMPORTANT: root kalsýn (conf auto-create + /etc yazma için)
# USER root (default zaten root)

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ferryx-Hub.dll"]
