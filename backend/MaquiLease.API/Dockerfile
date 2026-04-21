FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MaquiLease.API.csproj", "./"]
RUN dotnet restore "MaquiLease.API.csproj"
COPY . .
RUN dotnet build "MaquiLease.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MaquiLease.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Instalar dependencias globales del sistema (como soporte de fuentes para QuestPDF o ML en distros Linux)
RUN apt-get update && apt-get install -y libfontconfig1

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MaquiLease.API.dll"]
