# Use the official .NET 10.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy the csproj files and restore dependencies
COPY PolicyAPI/PolicyAPI.csproj ./PolicyAPI/
COPY PolicyEFCore/PolicyEFCore.csproj ./PolicyEFCore/
RUN dotnet restore PolicyAPI/PolicyAPI.csproj

# Copy the remaining files and build the release
COPY . ./
RUN dotnet publish PolicyAPI/PolicyAPI.csproj -c Release -o out

# Use the official .NET 10.0 ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Render dynamically injects a PORT environment variable. 
# We configure ASP.NET Core to bind to it, default to port 80 if not set.
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "PolicyAPI.dll"]
