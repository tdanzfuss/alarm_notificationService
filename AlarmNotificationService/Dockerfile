#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# BUILD with buildx to be able to run on PI https://www.docker.com/blog/multi-arch-images/
# docker buildx build -f "C:\Users\tdanzfuss\source\repos\AlarmNotificationService\AlarmNotificationService\Dockerfile" --platform linux/amd64,linux/arm/v7 -t tdanzfuss/alarmnotificationservice:armv7 --push .

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim-arm32v7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["AlarmNotificationService/AlarmNotificationService.csproj", "AlarmNotificationService/"]
RUN dotnet restore "AlarmNotificationService/AlarmNotificationService.csproj"
COPY . .
WORKDIR "/src/AlarmNotificationService"
RUN dotnet build "AlarmNotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlarmNotificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlarmNotificationService.dll"]