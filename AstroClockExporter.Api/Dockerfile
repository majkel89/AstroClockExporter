FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN apk update && apk add clang
COPY ["AstroClockExporter.Api/AstroClockExporter.Api.csproj", "AstroClockExporter.Api/"]
COPY ["AstroClockExporter.Core/AstroClockExporter.Core.csproj", "AstroClockExporter.Core/"]
RUN dotnet restore "AstroClockExporter.Api/AstroClockExporter.Api.csproj"
COPY . .
WORKDIR "/src/AstroClockExporter.Api"
RUN dotnet build "AstroClockExporter.Api.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false && \
    rm -rf /app/publish/appsettings.Development.json /app/publish/config.yml

FROM alpine:3.22 AS runtime
ARG APP_UID=nobody
ARG PORT=8080
ENV ASPNETCORE_URLS="http://0.0.0.0:${PORT}/"
USER $APP_UID
WORKDIR /app
EXPOSE $PORT
COPY --from=publish --chown=$APP_UID:$APP_UID --chmod=540 /app/publish .
CMD ["/app/AstroClockExporter.Api"]
