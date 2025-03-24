FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG OCTO_PRIVATE_NUGET_SERVICE
ARG OCTO_PRIVATE_NUGET_CERTIFICATE

COPY ${OCTO_PRIVATE_NUGET_CERTIFICATE} /usr/local/share/ca-certificates/nugetca.crt
RUN chmod 644 /usr/local/share/ca-certificates/nugetca.crt && update-ca-certificates

WORKDIR /src
COPY ["Directory.Build.props", ""]
COPY ["src/Communication.Plugs.Demo/Communication.Plugs.Demo.csproj", "src/Communication.Plugs.Demo/"]
RUN dotnet restore "src/Communication.Plugs.Demo/Communication.Plugs.Demo.csproj"
COPY . .
WORKDIR "/src/src/Communication.Plugs.Demo"
RUN dotnet build "Communication.Plugs.Demo.csproj" -c Release -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -o /app/build

FROM build AS publish
RUN dotnet publish "Communication.Plugs.Demo.csproj" -c Release -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Meshmakers.Octo.Communication.Plugs.Demo.dll"]
