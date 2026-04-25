FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore "Gateway/TicketFlow.Gateway/TicketFlow.Gateway.csproj"
RUN dotnet publish "Gateway/TicketFlow.Gateway/TicketFlow.Gateway.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TicketFlow.Gateway.dll"]
