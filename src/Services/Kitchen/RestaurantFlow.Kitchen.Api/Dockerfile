FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/Services/Kitchen/RestaurantFlow.Kitchen.Api/RestaurantFlow.Kitchen.Api.csproj
RUN dotnet publish src/Services/Kitchen/RestaurantFlow.Kitchen.Api/RestaurantFlow.Kitchen.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestaurantFlow.Kitchen.Api.dll"]

