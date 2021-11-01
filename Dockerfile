FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["GS1ApplicationIdentifierScrapper.csproj", "./"]
RUN dotnet restore "GS1ApplicationIdentifierScrapper.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "GS1ApplicationIdentifierScrapper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GS1ApplicationIdentifierScrapper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GS1ApplicationIdentifierScrapper.dll"]
