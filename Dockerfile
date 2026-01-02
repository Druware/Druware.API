FROM mcr.microsoft.com/dotnet/sdk:8.0

# Dotnet setup for the Kestrel API spinup

RUN mkdir -p /app
WORKDIR /app
COPY ./app/ .
ENTRYPOINT ["dotnet", "Druware.API.dll", "--urls", "http://::8000"]
