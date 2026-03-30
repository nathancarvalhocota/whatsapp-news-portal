FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj WhatsAppNewsPortal.Api/
RUN dotnet restore WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj

COPY . .
RUN dotnet publish WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
ENV COMPlus_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "WhatsAppNewsPortal.Api.dll"]
