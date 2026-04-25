FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore "Services/SimulationWorker/TicketFlow.SimulationWorker.csproj"
RUN dotnet publish "Services/SimulationWorker/TicketFlow.SimulationWorker.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TicketFlow.SimulationWorker.dll"]
