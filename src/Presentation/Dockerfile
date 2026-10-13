# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Presentation/Presentation.csproj",       "src/Presentation/"]
COPY ["src/Application/Application.csproj",         "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj",   "src/Infrastructure/"]
COPY ["src/Domain/Domain.csproj",                   "src/Domain/"]

# Если есть Directory.Build.props / Directory.Packages.props в корне — их тоже нужно скопировать ДО restore
COPY ["Directory.Build.props", "./"]
# COPY ["Directory.Packages.props", "./"]   # раскомментируйте, если такой файл есть
# COPY ["NuGet.config", "./"]               # если есть

RUN dotnet restore "src/Presentation/Presentation.csproj"

COPY . .
RUN find . -name "Dockerfile" -type f -delete

WORKDIR /src/src/Presentation
RUN dotnet build "Presentation.csproj" -c Release --no-restore -o /app/build

# ---------- publish ----------
FROM build AS publish
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080
ENTRYPOINT ["dotnet", "Presentation.dll"]