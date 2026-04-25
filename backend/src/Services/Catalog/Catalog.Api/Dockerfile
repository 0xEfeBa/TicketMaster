FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "Services/Catalog/Catalog.Api/Catalog.Api.csproj"
# Build and publish a release
RUN dotnet publish "Services/Catalog/Catalog.Api/Catalog.Api.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
