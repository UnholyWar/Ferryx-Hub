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

EXPOSE 18080

RUN mkdir -p /etc/ferryx

COPY --from=build /app/publish .

# ✅ ferryx CLI wrapper
RUN printf '#!/bin/sh\nexec dotnet /app/Ferryx-Hub.dll "$@"\n' > /usr/local/bin/ferryx \
 && chmod +x /usr/local/bin/ferryx


ENTRYPOINT ["dotnet", "Ferryx-Hub.dll"]
# CMD'yi tamamen kaldır


