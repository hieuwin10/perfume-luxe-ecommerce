# Multi-stage Dockerfile for ASP.NET Core 8 Web App
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Perfume.Domain/Perfume.Domain.csproj", "src/Perfume.Domain/"]
COPY ["src/Perfume.Application/Perfume.Application.csproj", "src/Perfume.Application/"]
COPY ["src/Perfume.Infrastructure/Perfume.Infrastructure.csproj", "src/Perfume.Infrastructure/"]
COPY ["src/Perfume.Web/Perfume.Web.csproj", "src/Perfume.Web/"]

RUN dotnet restore "src/Perfume.Web/Perfume.Web.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/src/Perfume.Web"
RUN dotnet build "Perfume.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Perfume.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=publish /app/publish .
COPY --chown=app:app --from=build /src/scripts ./scripts
USER app
ENTRYPOINT ["dotnet", "Perfume.Web.dll"]
