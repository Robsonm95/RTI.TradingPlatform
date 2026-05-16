FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RTI.OrderAccumulator/RTI.OrderAccumulator.csproj RTI.OrderAccumulator/
COPY RTI.Shared/RTI.Shared.csproj RTI.Shared/
RUN dotnet restore RTI.OrderAccumulator/RTI.OrderAccumulator.csproj

COPY . .
WORKDIR /src/RTI.OrderAccumulator
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5002;https://+:5003
EXPOSE 5002
EXPOSE 5003

ENTRYPOINT ["dotnet", "RTI.OrderAccumulator.dll"]
