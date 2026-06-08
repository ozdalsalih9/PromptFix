FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PromptFix.sln ./
COPY src/PromptFix.Api/PromptFix.Api.csproj src/PromptFix.Api/
COPY tests/PromptFix.Api.Tests/PromptFix.Api.Tests.csproj tests/PromptFix.Api.Tests/
RUN dotnet restore src/PromptFix.Api/PromptFix.Api.csproj

COPY src/PromptFix.Api/ src/PromptFix.Api/
RUN dotnet publish src/PromptFix.Api/PromptFix.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://127.0.0.1:5064
ENV OLLAMA__BASEURL=http://127.0.0.1:11434
ENV OLLAMA__MODEL=promptforge:4b
ENV OLLAMA__TIMEOUTSECONDS=60

ENTRYPOINT ["dotnet", "PromptFix.Api.dll"]
