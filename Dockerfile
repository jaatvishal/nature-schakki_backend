# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Natures_Chakki_Backend.sln ./
COPY API/API.csproj API/
COPY Core/Core.csproj Core/
COPY Infrastructure/Infrastructure.csproj Infrastructure/

RUN dotnet restore API/API.csproj

COPY API/ API/
COPY Core/ Core/
COPY Infrastructure/ Infrastructure/

RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "API.dll"]
