FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
# FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-noble-chiseled

#
# Container Security
# https://techcommunity.microsoft.com/t5/azure-developer-community-blog/hardening-an-asp-net-container-running-on-kubernetes/ba-p/2542224
#
# not 80 => errors with RunAsNonRoot
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080
# readOnlyRootFilesystem = true works with this:
ENV COMPlus_EnableDiagnostics=0

WORKDIR /app#
COPY /artifacts .
COPY /override .

ENTRYPOINT ["dotnet", "webgis-cms.dll"]