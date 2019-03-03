FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY src/DuaBot.csproj src/
RUN dotnet restore src/DuaBot.csproj
COPY . .
WORKDIR /src/src
RUN dotnet build DuaBot.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DuaBot.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DuaBot.dll"]
