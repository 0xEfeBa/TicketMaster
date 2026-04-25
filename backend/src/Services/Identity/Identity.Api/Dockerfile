FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore "Services/Identity/Identity.Api/Identity.Api.csproj"
RUN dotnet publish "Services/Identity/Identity.Api/Identity.Api.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
