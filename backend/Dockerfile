FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ChatWithDocs.sln .
COPY src/ChatWithDocs.Domain/ChatWithDocs.Domain.csproj src/ChatWithDocs.Domain/
COPY src/ChatWithDocs.Application/ChatWithDocs.Application.csproj src/ChatWithDocs.Application/
COPY src/ChatWithDocs.Infrastructure/ChatWithDocs.Infrastructure.csproj src/ChatWithDocs.Infrastructure/
COPY src/ChatWithDocs.Api/ChatWithDocs.Api.csproj src/ChatWithDocs.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/ChatWithDocs.Api/ChatWithDocs.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatWithDocs.Api.dll"]
