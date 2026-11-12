FROM node:24-alpine AS ui
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ ./
RUN dotnet publish -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=ui /src/dist ./wwwroot
RUN mkdir -p /app/storage /app/keys && chown -R $APP_UID /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Society.Api.dll"]
