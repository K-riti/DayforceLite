FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/DayforceLite.API/DayforceLite.API.csproj", "src/DayforceLite.API/"]
COPY ["src/DayforceLite.Core/DayforceLite.Core.csproj", "src/DayforceLite.Core/"]
COPY ["src/DayforceLite.Infrastructure/DayforceLite.Infrastructure.csproj", "src/DayforceLite.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/DayforceLite.API/DayforceLite.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/DayforceLite.API"
RUN dotnet build "DayforceLite.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DayforceLite.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DayforceLite.API.dll"]
