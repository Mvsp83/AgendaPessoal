FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AgendaPessoal.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish AgendaPessoal.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ["dotnet", "AgendaPessoal.dll"]
