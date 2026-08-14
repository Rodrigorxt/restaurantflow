FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/Services/Payments/RestaurantFlow.Payments.Api/RestaurantFlow.Payments.Api.csproj
RUN dotnet publish src/Services/Payments/RestaurantFlow.Payments.Api/RestaurantFlow.Payments.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestaurantFlow.Payments.Api.dll"]

