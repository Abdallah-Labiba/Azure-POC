# Use the official .NET 9 runtime image as the base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["POC.Api.csproj", "."]
RUN dotnet restore "./POC.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "POC.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "POC.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "POC.Api.dll"]
