#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Run:  docker run -it -p 83:80 -p 84:443 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=84 -e ASPNETCORE_Kestrel__Certificates__Default__Password="Password" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/wild.lucasnet.int.pfx -e ASPNETCORE_ENVIRONMENT="Development" -v C:\Develop\Certificates\:/https/ mtlucas/dynamostudentmanager:latest
#       docker run -it -p 83:80 -p 84:443 -e ASPNETCORE_URLS="http://+" -e ASPNETCORE_ENVIRONMENT="Development" mtlucas/dynamostudentmanager:latest

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DynamoStudentManager.csproj", "."]
RUN dotnet restore "./DynamoStudentManager.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DynamoStudentManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DynamoStudentManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV DYNAMO_ENDPOINT="http://dynamodb-1.lucasnet.int:8000"
ENV AWS_REGION=us-west-2
ENV AWS_ACCESS_KEY_ID=local
ENV AWS_SECRET_ACCESS_KEY=local
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DynamoStudentManager.dll"]