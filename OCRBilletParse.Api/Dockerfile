#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["OCRBilletParse.Api/OCRBilletParse.Api.csproj", "OCRBilletParse.Api/"]
COPY ["OCRBilletParse/OCRBilletParse.csproj", "OCRBilletParse/"]
COPY ["OCRBilletParse.Common/OCRBilletParse.Common.csproj", "OCRBilletParse.Common/"]
RUN dotnet restore "OCRBilletParse.Api/OCRBilletParse.Api.csproj"
COPY . .
WORKDIR "/src/OCRBilletParse.Api"
RUN dotnet build "OCRBilletParse.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OCRBilletParse.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OCRBilletParse.Api.dll"]