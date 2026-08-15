FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/Web/RestaurantFlow.Web/RestaurantFlow.Web.csproj src/Web/RestaurantFlow.Web/
RUN dotnet restore src/Web/RestaurantFlow.Web/RestaurantFlow.Web.csproj
COPY . .
RUN dotnet publish src/Web/RestaurantFlow.Web/RestaurantFlow.Web.csproj -c Release -o /app --no-restore

FROM nginx:1.29-alpine
COPY src/Web/RestaurantFlow.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 8080
