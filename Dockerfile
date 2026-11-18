FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY StudioSport.API/*.csproj StudioSport.API/
COPY StudioSport.Core/*.csproj StudioSport.Core/
COPY StudioSport.Data/*.csproj StudioSport.Data/
COPY StudioSport.Service/*.csproj StudioSport.Service/
COPY StudioSport.Tests/*.csproj StudioSport.Tests/
RUN dotnet restore StudioSport.API/StudioSport.API.csproj

COPY . .

# העתקה מפורשת של תוכן הדפים אחרי הניקוי — מבטלת caching של wwwroot
COPY StudioSport.API/wwwroot/ ./StudioSport.API/wwwroot/

RUN dotnet publish StudioSport.API/StudioSport.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "StudioSport.API.dll"]