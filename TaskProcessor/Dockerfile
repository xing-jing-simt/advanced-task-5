FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5001

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["TaskProcessor.csproj", "./"]
RUN dotnet restore "TaskProcessor.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TaskProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskProcessor.dll"]