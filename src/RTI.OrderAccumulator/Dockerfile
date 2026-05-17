# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RTI.Shared/RTI.Shared.csproj", "src/RTI.Shared/"]
COPY ["src/RTI.OrderAccumulator/RTI.OrderAccumulator.csproj", "src/RTI.OrderAccumulator/"]

RUN dotnet restore "src/RTI.OrderAccumulator/RTI.OrderAccumulator.csproj"

COPY . .
WORKDIR "/src/src/RTI.OrderAccumulator"
RUN dotnet publish "RTI.OrderAccumulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5002
EXPOSE 5003

ENV ASPNETCORE_URLS=http://*:5002
ENV ASPNETCORE_HTTP_PORTS=5002

ENTRYPOINT ["dotnet", "RTI.OrderAccumulator.dll"]