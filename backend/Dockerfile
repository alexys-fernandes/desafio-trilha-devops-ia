FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY HabitApp.API/HabitApp.API.csproj ./HabitApp.API/
COPY HabitApp.Application/HabitApp.Application.csproj ./HabitApp.Application/
COPY HabitApp.Domain/HabitApp.Domain.csproj ./HabitApp.Domain/
COPY HabitApp.Domain.Services/HabitApp.Domain.Services.csproj ./HabitApp.Domain.Services/
COPY HabitApp.Infrastructure.Data/HabitApp.Infrastructure.Data.csproj ./HabitApp.Infrastructure.Data/
COPY HabitApp.Infrastructure.IOC/HabitApp.Infrastructure.IOC.csproj ./HabitApp.Infrastructure.IOC/

RUN dotnet restore ./HabitApp.API/HabitApp.API.csproj

COPY . .

RUN dotnet publish ./HabitApp.API/HabitApp.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HabitApp.API.dll"]
