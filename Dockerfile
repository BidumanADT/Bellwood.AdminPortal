# ---- build stage ----
# Build context must be the parent directory (source/repos/) because of the
# ProjectReference to BellwoodMobileApp/BellwoodGlobal.Mobile/BellwoodGlobal.Core.
#
#   docker build -f Bellwood.AdminPortal/Dockerfile -t bellwood-adminportal .
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy referenced project first
COPY BellwoodMobileApp/BellwoodGlobal.Mobile/BellwoodGlobal.Core/BellwoodGlobal.Core.csproj \
     BellwoodMobileApp/BellwoodGlobal.Mobile/BellwoodGlobal.Core/

# Copy main project
COPY Bellwood.AdminPortal/Bellwood.AdminPortal.csproj Bellwood.AdminPortal/

RUN dotnet restore Bellwood.AdminPortal/Bellwood.AdminPortal.csproj

# Copy all source for both projects
COPY BellwoodMobileApp/BellwoodGlobal.Mobile/BellwoodGlobal.Core/ \
     BellwoodMobileApp/BellwoodGlobal.Mobile/BellwoodGlobal.Core/
COPY Bellwood.AdminPortal/ Bellwood.AdminPortal/

RUN dotnet publish Bellwood.AdminPortal/Bellwood.AdminPortal.csproj \
    -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Alpha

EXPOSE 8080

ENTRYPOINT ["dotnet", "Bellwood.AdminPortal.dll"]
