FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["LithoManager.Api/LithoManager.Api.csproj", "LithoManager.Api/"]
COPY ["LithoManager.Application/LithoManager.Application.csproj", "LithoManager.Application/"]
COPY ["LithoManager.Infrastructure/LithoManager.Infrastructure.csproj", "LithoManager.Infrastructure/"]

RUN dotnet restore "LithoManager.Api/LithoManager.Api.csproj"

COPY . .

WORKDIR "/src/LithoManager.Api"

RUN dotnet publish "LithoManager.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "LithoManager.Api.dll"]