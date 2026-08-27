FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/DreamLens.Api/DreamLens.Api.csproj", "src/DreamLens.Api/"]
COPY ["src/PersonaKit/PersonaKit.csproj", "src/PersonaKit/"]
RUN dotnet restore "src/DreamLens.Api/DreamLens.Api.csproj"
COPY . .
WORKDIR "/src/src/DreamLens.Api"
RUN dotnet publish "DreamLens.Api.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DreamLens.Api.dll"]
