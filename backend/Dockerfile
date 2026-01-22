FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SmartSusChef.Api/SmartSusChef.Api.csproj", "SmartSusChef.Api/"]
RUN dotnet restore "SmartSusChef.Api/SmartSusChef.Api.csproj"
COPY . .
WORKDIR "/src/SmartSusChef.Api"
RUN dotnet build "SmartSusChef.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartSusChef.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartSusChef.Api.dll"]
