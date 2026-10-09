# Multi-stage build for ReturnFlow AI (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project definitions for caching
COPY ["ReturnFlow.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/ReturnFlow.Core/ReturnFlow.Core.csproj", "src/ReturnFlow.Core/"]
COPY ["src/ReturnFlow.Web/ReturnFlow.Web.csproj", "src/ReturnFlow.Web/"]
COPY ["tests/ReturnFlow.UnitTests/ReturnFlow.UnitTests.csproj", "tests/ReturnFlow.UnitTests/"]

RUN dotnet restore "ReturnFlow.sln"

# Copy full source and publish
COPY . .
RUN dotnet publish "src/ReturnFlow.Web/ReturnFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReturnFlow.Web.dll"]
