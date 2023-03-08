# Clone CySim Files
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS restore-src
WORKDIR /app
COPY ./src/CySim.csproj .
# COPY ./src/CySim.sln .
RUN dotnet restore CySim.csproj 


FROM restore-src AS clone-src
WORKDIR /app
COPY ./src .


# Migrate data using ef
FROM clone-src AS migrate-src
WORKDIR /app
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations add DockerInit


# Build CySim project
FROM migrate-src AS build-src
WORKDIR /app
RUN dotnet publish CySim.csproj -c Debug -o out


# Running web server
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS run-src
RUN apk add --no-cache curl icu-libs
WORKDIR /app
COPY --from=build-src /app/out ./


# Restore CySim.Tests (Prepared to run)
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS run-tests
WORKDIR /app
COPY ./src ./src
COPY ./tests ./tests
WORKDIR /app/tests
RUN dotnet restore

