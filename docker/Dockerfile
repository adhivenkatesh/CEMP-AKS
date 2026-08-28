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

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet","Employee.API.dll"]