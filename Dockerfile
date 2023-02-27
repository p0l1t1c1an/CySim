# Clone CySim Files
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS restore
WORKDIR /app
COPY CySim.csproj .
COPY CySim.sln .
RUN dotnet restore

FROM restore AS clone
COPY . ./

# Build CySim project
FROM clone AS build
WORKDIR /app
RUN dotnet publish CySim.csproj -c Debug -o out

# Migrate data using ef
FROM clone AS migrate
WORKDIR /app
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations add DockerInit

# Running web server
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS run
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build /app/out ./
