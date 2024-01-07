FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Druware.API.csproj", "./"]
COPY ["../Druware.Extensions/Druware.Extensions.csproj", "../Druware.Extensions/"]
COPY ["../Druware.Server/Druware.Server.csproj", "../Druware.Server/"]
COPY ["../Druware.Server.Content/Druware.Server.Content.csproj", "../Druware.Server.Content/"]
COPY ["../Druware.Server.Controllers/Druware.Server.Controllers.csproj", "../Druware.Server.Controllers/"]
RUN dotnet restore "Druware.API.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Druware.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Druware.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Druware.API.dll"]
