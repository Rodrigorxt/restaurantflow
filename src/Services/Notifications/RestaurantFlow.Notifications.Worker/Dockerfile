FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/Services/Notifications/RestaurantFlow.Notifications.Worker/RestaurantFlow.Notifications.Worker.csproj
RUN dotnet publish src/Services/Notifications/RestaurantFlow.Notifications.Worker/RestaurantFlow.Notifications.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "RestaurantFlow.Notifications.Worker.dll"]
