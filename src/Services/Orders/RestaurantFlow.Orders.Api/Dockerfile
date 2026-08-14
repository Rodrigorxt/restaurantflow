FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/Services/Orders/RestaurantFlow.Orders.Api/RestaurantFlow.Orders.Api.csproj
RUN dotnet publish src/Services/Orders/RestaurantFlow.Orders.Api/RestaurantFlow.Orders.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestaurantFlow.Orders.Api.dll"]

