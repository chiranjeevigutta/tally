# 1. build the React app
FROM node:22-alpine AS web
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2. build and publish the API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/src/Tally.Api/Tally.Api.csproj Tally.Api/
RUN dotnet restore Tally.Api/Tally.Api.csproj
COPY backend/src/Tally.Api/ Tally.Api/
RUN dotnet publish Tally.Api/Tally.Api.csproj -c Release -o /app --no-restore

# 3. runtime image, API serves the React build from wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# fonts are needed for the PDF and Excel exports
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/app/data/tally.db"
RUN mkdir -p /app/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tally.Api.dll"]
