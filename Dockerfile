FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG APP_UID=1000
RUN groupadd -g $APP_UID tsugroup && useradd -r -u $APP_UID -g tsugroup tsuuser

USER root
RUN mkdir /app && chown -R tsuuser:tsugroup /app

RUN mkdir /app/app_data && chown -R tsuuser:tsugroup /app/app_data

USER tsuuser
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tsu-absences-api.csproj", "./"]
RUN dotnet restore "tsu-absences-api.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "tsu-absences-api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "tsu-absences-api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN chown -R tsuuser:tsugroup /app
USER tsuuser

ENTRYPOINT ["dotnet", "tsu-absences-api.dll"]