
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/TaskFlow.Api/TaskFlow.Api.csproj TaskFlow.Api/
RUN dotnet restore TaskFlow.Api/TaskFlow.Api.csproj


COPY src/TaskFlow.Api/ TaskFlow.Api/
WORKDIR /src/TaskFlow.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish 


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app


RUN addgroup --system taskflow && adduser --system --ingroup taskflow taskflow
USER taskflow

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
