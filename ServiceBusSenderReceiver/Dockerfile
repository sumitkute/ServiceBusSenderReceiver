FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY . ServiceBusSenderReceiver/
RUN dotnet restore ServiceBusSenderReceiver/ServiceBusSenderReceiver.csproj
WORKDIR /src/ServiceBusSenderReceiver
RUN dotnet build ServiceBusSenderReceiver.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ServiceBusSenderReceiver.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ServiceBusSenderReceiver.dll"]
