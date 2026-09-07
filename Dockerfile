FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AgencyFlow/AgencyFlow.csproj AgencyFlow/
RUN dotnet restore AgencyFlow/AgencyFlow.csproj

COPY AgencyFlow/ AgencyFlow/
RUN dotnet publish AgencyFlow/AgencyFlow.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "dotnet AgencyFlow.dll --urls http://0.0.0.0:${PORT:-8080}"]
