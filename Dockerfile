#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y libopus0
RUN apt-get install -y ffmpeg
RUN apt-get install -y libopus-dev
RUN apt-get install -y opus-tools
RUN apt-get install -y libogg0 
RUN apt-get install -y libsodium-dev
RUN apt-get install -y libsodium23

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DotNet.Docker.csproj", "."]
RUN dotnet restore "./DotNet.Docker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DotNet.Docker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotNet.Docker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNet.Docker.dll"]