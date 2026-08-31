# -----------------------------
# Stage 1 - Build
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY src/Employee.API/Employee.API.csproj Employee.API/

WORKDIR /src/Employee.API

RUN dotnet restore

COPY src/Employee.API/ .

RUN dotnet publish -c Release -o /app/publish

# -----------------------------
# Stage 2 - Runtime
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# FIX for NetSecurityNative / libgssapi_krb5.so.2 error
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Force SQL Auth, not Windows Auth
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_NET_SECURITY_NATIVESHIMS=false

EXPOSE 80
EXPOSE 8080

ENTRYPOINT ["dotnet","Employee.API.dll"]