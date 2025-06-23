# Should be build using solution root dir as context
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /source

COPY . .

RUN dotnet build

COPY ./deploy/application/execute-tests.sh .
