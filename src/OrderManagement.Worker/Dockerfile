FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["NuGet.config", "."]
COPY ["src/OrderManagement.Worker/OrderManagement.Worker.csproj", "src/OrderManagement.Worker/"]
COPY ["src/OrderManagement.Application/OrderManagement.Application.csproj", "src/OrderManagement.Application/"]
COPY ["src/OrderManagement.Domain/OrderManagement.Domain.csproj", "src/OrderManagement.Domain/"]
COPY ["src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj", "src/OrderManagement.Infrastructure/"]
RUN dotnet restore "src/OrderManagement.Worker/OrderManagement.Worker.csproj"

COPY . .
RUN dotnet publish "src/OrderManagement.Worker/OrderManagement.Worker.csproj" --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "OrderManagement.Worker.dll"]
