# Base runtime image for .NET 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# SDK build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for efficient layer caching
COPY ["HRWatch.Domain/HRWatch.Domain.csproj", "HRWatch.Domain/"]
COPY ["HRWatch.Application/HRWatch.Application.csproj", "HRWatch.Application/"]
COPY ["HRWatch.Infrastructure/HRWatch.Infrastructure.csproj", "HRWatch.Infrastructure/"]
COPY ["HRWatch.API/HRWatch.API.csproj", "HRWatch.API/"]

RUN dotnet restore "HRWatch.API/HRWatch.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/HRWatch.API"
RUN dotnet build "HRWatch.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "HRWatch.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HRWatch.API.dll"]
