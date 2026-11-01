FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TrackWeatherWeb/TrackWeatherWeb.csproj", "TrackWeatherWeb/"]
RUN dotnet restore "TrackWeatherWeb/TrackWeatherWeb.csproj"
COPY ["TrackWeatherWeb/", "TrackWeatherWeb/"]
RUN dotnet publish "TrackWeatherWeb/TrackWeatherWeb.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Data protection keys live on a volume so sign-ins survive container restarts
RUN mkdir -p /keys && chown app:app /keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrackWeatherWeb.dll"]
