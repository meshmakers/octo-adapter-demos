FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG OCTO_PRIVATE_NUGET_SERVICE
ARG OCTO_PRIVATE_NUGET_CERTIFICATE
ARG OCTO_VERSION

COPY ${OCTO_PRIVATE_NUGET_CERTIFICATE} /usr/local/share/ca-certificates/nugetca.crt
RUN chmod 644 /usr/local/share/ca-certificates/nugetca.crt && update-ca-certificates

WORKDIR /src
COPY ["Directory.Build.props", ""]
COPY ["src/AdapterMeshDemo/AdapterMeshDemo.csproj", "src/AdapterMeshDemo/"]
RUN dotnet restore "src/AdapterMeshDemo/AdapterMeshDemo.csproj" -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -p:OctoVersion=${OCTO_VERSION}
COPY . .
WORKDIR "/src/src/AdapterMeshDemo"
RUN dotnet build "AdapterMeshDemo.csproj" -c Release -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -p:OctoVersion=${OCTO_VERSION} -o /app/build

FROM build AS publish
RUN dotnet publish "AdapterMeshDemo.csproj" -c Release -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -p:OctoVersion=${OCTO_VERSION} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Meshmakers.Octo.Communication.MeshAdapter.Demo.dll"]
