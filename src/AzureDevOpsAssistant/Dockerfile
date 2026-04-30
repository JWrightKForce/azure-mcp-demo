FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AzureDevOpsAssistant.csproj", "."]
RUN dotnet restore "./AzureDevOpsAssistant.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AzureDevOpsAssistant.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureDevOpsAssistant.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureDevOpsAssistant.dll"]
