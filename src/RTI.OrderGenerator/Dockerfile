FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RTI.OrderGenerator/RTI.OrderGenerator.csproj RTI.OrderGenerator/
COPY RTI.Shared/RTI.Shared.csproj RTI.Shared/
RUN dotnet restore RTI.OrderGenerator/RTI.OrderGenerator.csproj

COPY . .
WORKDIR /src/RTI.OrderGenerator
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "RTI.OrderGenerator.dll"]
