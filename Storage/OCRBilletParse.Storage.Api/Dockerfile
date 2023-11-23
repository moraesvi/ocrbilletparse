#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Storage/OCRBilletParse.Storage.Api/OCRBilletParse.Storage.Api.csproj", "Storage/OCRBilletParse.Storage.Api/"]
COPY ["OCRBilletParse.Common/OCRBilletParse.Common.csproj", "OCRBilletParse.Common/"]
COPY ["Storage/OCRBilletParse.Storage/OCRBilletParse.Storage.csproj", "Storage/OCRBilletParse.Storage/"]
RUN dotnet restore "Storage/OCRBilletParse.Storage.Api/OCRBilletParse.Storage.Api.csproj"
COPY . .
WORKDIR "/src/Storage/OCRBilletParse.Storage.Api"
RUN dotnet build "OCRBilletParse.Storage.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OCRBilletParse.Storage.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OCRBilletParse.Storage.Api.dll"]