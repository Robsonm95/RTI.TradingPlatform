# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RTI.Shared/RTI.Shared.csproj", "src/RTI.Shared/"]
COPY ["src/RTI.OrderGenerator/RTI.OrderGenerator.csproj", "src/RTI.OrderGenerator/"]

RUN dotnet restore "src/RTI.OrderGenerator/RTI.OrderGenerator.csproj"

COPY . .
WORKDIR "/src/src/RTI.OrderGenerator"
RUN dotnet publish "RTI.OrderGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "RTI.OrderGenerator.dll"]