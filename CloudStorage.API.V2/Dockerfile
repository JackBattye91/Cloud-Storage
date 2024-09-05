#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS dotnet
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM dotnet AS build
COPY /Publish .
ENTRYPOINT ["dotnet", "CloudStorage.API.V2.dll"]