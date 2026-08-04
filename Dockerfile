FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY NEXA.sln ./
COPY Nexa.Web/Nexa.Web.csproj Nexa.Web/
RUN dotnet restore Nexa.Web/Nexa.Web.csproj
COPY Nexa.Web/ Nexa.Web/
RUN dotnet publish Nexa.Web/Nexa.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY content/videos ./content/videos
ENV PORT=5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV DataPath=/app/data/store.json
ENV VideoPath=/app/content/videos
ENV MP_ALLOW_SIMULATE=true
EXPOSE 5000
ENTRYPOINT ["dotnet", "Nexa.Web.dll"]
