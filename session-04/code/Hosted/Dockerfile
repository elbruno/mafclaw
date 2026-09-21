FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY Directory.Build.props NuGet.Config ./
COPY code/Contracts/ code/Contracts/
COPY code/Agent/ code/Agent/
COPY code/Hosted/ code/Hosted/
COPY evals/ evals/
RUN dotnet publish code/Hosted/MafClaw.Session04.Hosted.csproj -c Release -r linux-x64 --self-contained false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8088
EXPOSE 8088
USER $APP_UID
ENTRYPOINT ["dotnet", "MafClaw.Session04.Hosted.dll"]
