# Should be build using solution root dir as context
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build


WORKDIR /source

COPY *.sln .

# To get fresh list of all COPY statement run following command from this file's folder:
# cd ../..; find . -type f -name "*.csproj" | awk '{print "COPY " $0 " " $0 }' | pbcopy ; cd - > /dev/null
COPY ./Microservices/Video/Frever.Video.Contract.Messages/Frever.Video.Contract.Messages.csproj ./Microservices/Video/Frever.Video.Contract.Messages/Frever.Video.Contract.Messages.csproj
COPY ./Microservices/Video/Frever.Video.Core.Test/Frever.Video.Core.Test.csproj ./Microservices/Video/Frever.Video.Core.Test/Frever.Video.Core.Test.csproj
COPY ./Microservices/Video/Frever.Video.Api/Frever.Video.Api.csproj ./Microservices/Video/Frever.Video.Api/Frever.Video.Api.csproj
COPY ./Microservices/Video/Frever.Video.Core.IntegrationTest/Frever.Video.Core.IntegrationTest.csproj ./Microservices/Video/Frever.Video.Core.IntegrationTest/Frever.Video.Core.IntegrationTest.csproj
COPY ./Microservices/Video/VideoServer.CreateConversionJobLambda/VideoServer.CreateConversionJobLambda.csproj ./Microservices/Video/VideoServer.CreateConversionJobLambda/VideoServer.CreateConversionJobLambda.csproj
COPY ./Microservices/Video/Frever.Video.Core.Features.PersonalFeed/Frever.Video.Core.Features.PersonalFeed.csproj ./Microservices/Video/Frever.Video.Core.Features.PersonalFeed/Frever.Video.Core.Features.PersonalFeed.csproj
COPY ./Microservices/Video/Frever.Video.Contract/Frever.Video.Contract.csproj ./Microservices/Video/Frever.Video.Contract/Frever.Video.Contract.csproj
COPY ./Microservices/Video/Frever.Video.Shared/Frever.Video.Shared.csproj ./Microservices/Video/Frever.Video.Shared/Frever.Video.Shared.csproj
COPY ./Microservices/Video/Frever.Video.Core/Frever.Video.Core.csproj ./Microservices/Video/Frever.Video.Core/Frever.Video.Core.csproj
COPY ./Microservices/NotificationService/NotificationService/NotificationService.csproj ./Microservices/NotificationService/NotificationService/NotificationService.csproj
COPY ./Microservices/NotificationService/NotificationService.Shared/NotificationService.Shared.csproj ./Microservices/NotificationService/NotificationService.Shared/NotificationService.Shared.csproj
COPY ./Microservices/NotificationService/NotificationService.Client/NotificationService.Client.csproj ./Microservices/NotificationService/NotificationService.Client/NotificationService.Client.csproj
COPY ./Microservices/Admin/Frever.Impersonate/Frever.Impersonate.csproj ./Microservices/Admin/Frever.Impersonate/Frever.Impersonate.csproj
COPY ./Microservices/Admin/Frever.AdminService.Core/Frever.AdminService.Core.csproj ./Microservices/Admin/Frever.AdminService.Core/Frever.AdminService.Core.csproj
COPY ./Microservices/Admin/Frever.AdminService.Api/Frever.AdminService.Api.csproj ./Microservices/Admin/Frever.AdminService.Api/Frever.AdminService.Api.csproj
COPY ./Microservices/AssetServer/AssetServer.csproj ./Microservices/AssetServer/AssetServer.csproj
COPY ./Microservices/ImageConverter/ImageConverter/ImageConverter.csproj ./Microservices/ImageConverter/ImageConverter/ImageConverter.csproj
COPY ./Microservices/Authorization/AuthServer.DataAccess/AuthServer.DataAccess.csproj ./Microservices/Authorization/AuthServer.DataAccess/AuthServer.DataAccess.csproj
COPY ./Microservices/Authorization/AuthServer.TokenGeneration/AuthServer.TokenGeneration.csproj ./Microservices/Authorization/AuthServer.TokenGeneration/AuthServer.TokenGeneration.csproj
COPY ./Microservices/Authorization/AuthServer/AuthServer.csproj ./Microservices/Authorization/AuthServer/AuthServer.csproj
COPY ./Microservices/Authorization/AuthServerShared/AuthServerShared.csproj ./Microservices/Authorization/AuthServerShared/AuthServerShared.csproj
COPY ./Microservices/Authorization/Frever.Auth.Core.Test/Frever.Auth.Core.Test.csproj ./Microservices/Authorization/Frever.Auth.Core.Test/Frever.Auth.Core.Test.csproj
COPY ./Microservices/Authorization/AuthServer.Permissions/AuthServer.Permissions.csproj ./Microservices/Authorization/AuthServer.Permissions/AuthServer.Permissions.csproj
COPY ./Microservices/Assets/AssetServer.Shared/AssetServer.Shared.csproj ./Microservices/Assets/AssetServer.Shared/AssetServer.Shared.csproj
COPY ./Microservices/Assets/AssetServer.AssetCopyingLambda/AssetServer.AssetCopyingLambda.csproj ./Microservices/Assets/AssetServer.AssetCopyingLambda/AssetServer.AssetCopyingLambda.csproj
COPY ./Microservices/Client/Frever.ClientService.Api/Frever.ClientService.Api.csproj ./Microservices/Client/Frever.ClientService.Api/Frever.ClientService.Api.csproj
COPY ./Microservices/Client/Frever.Client.Shared.Test/Frever.Client.Shared.Test.csproj ./Microservices/Client/Frever.Client.Shared.Test/Frever.Client.Shared.Test.csproj
COPY ./Microservices/Client/Frever.Client.Shared/Frever.Client.Shared.csproj ./Microservices/Client/Frever.Client.Shared/Frever.Client.Shared.csproj
COPY ./Microservices/Client/Frever.Client.Core.Test/Frever.Client.Core.Test.csproj ./Microservices/Client/Frever.Client.Core.Test/Frever.Client.Core.Test.csproj
COPY ./Microservices/Client/Frever.ClientService.Contract/Frever.ClientService.Contract.csproj ./Microservices/Client/Frever.ClientService.Contract/Frever.ClientService.Contract.csproj
COPY ./Microservices/Client/Frever.Client.Core/Frever.Client.Core.csproj ./Microservices/Client/Frever.Client.Core/Frever.Client.Core.csproj
COPY ./Microservices/Client/Frever.Client.Core.IntegrationTest/Frever.Client.Core.IntegrationTest.csproj ./Microservices/Client/Frever.Client.Core.IntegrationTest/Frever.Client.Core.IntegrationTest.csproj
COPY ./deploy/data-cloning/asset-cloning-approach/script-generator/Frever.Utils.TableDataCloneScriptGenerator/Frever.Utils.TableDataCloneScriptGenerator.csproj ./deploy/data-cloning/asset-cloning-approach/script-generator/Frever.Utils.TableDataCloneScriptGenerator/Frever.Utils.TableDataCloneScriptGenerator.csproj
COPY ./Utils/AssetUrlGenerator/AssetStorage.PathProviding.csproj ./Utils/AssetUrlGenerator/AssetStorage.PathProviding.csproj
COPY ./Utils/AssetUrlGenerator.Tests/AssetStorage.PathProviding.Tests.csproj ./Utils/AssetUrlGenerator.Tests/AssetStorage.PathProviding.Tests.csproj
COPY ./Shared/Frever.Protobuf/Frever.Protobuf.csproj ./Shared/Frever.Protobuf/Frever.Protobuf.csproj
COPY ./Shared/Frever.Common.IntegrationTesting/Frever.Common.IntegrationTesting.csproj ./Shared/Frever.Common.IntegrationTesting/Frever.Common.IntegrationTesting.csproj
COPY ./Shared/Frever.Cache/Frever.Cache.csproj ./Shared/Frever.Cache/Frever.Cache.csproj
COPY ./Shared/Frever.Shared.MainDb/Frever.Shared.MainDb.csproj ./Shared/Frever.Shared.MainDb/Frever.Shared.MainDb.csproj
COPY ./Shared/Frever.Shared.AssetStore/Frever.Shared.AssetStore.csproj ./Shared/Frever.Shared.AssetStore/Frever.Shared.AssetStore.csproj
COPY ./Common/Common.Models/Common.Models.csproj ./Common/Common.Models/Common.Models.csproj
COPY ./Common/Frever.Common.Testing/Frever.Common.Testing.csproj ./Common/Frever.Common.Testing/Frever.Common.Testing.csproj
COPY ./Common/Common.Infrastructure/Common.Infrastructure.csproj ./Common/Common.Infrastructure/Common.Infrastructure.csproj
COPY ./Lib/ACRCloudSdkCore/ACRCloudSdkCore.csproj ./Lib/ACRCloudSdkCore/ACRCloudSdkCore.csproj
COPY ./Jobs/Ixia.Job.RefreshSpotifyPopularity/Ixia.Job.RefreshSpotifyPopularity.csproj ./Jobs/Ixia.Job.RefreshSpotifyPopularity/Ixia.Job.RefreshSpotifyPopularity.csproj
COPY ./Jobs/Ixia.Job.LoadExternalSongsFromBlokur/Ixia.Job.LoadExternalSongsFromBlokur.csproj ./Jobs/Ixia.Job.LoadExternalSongsFromBlokur/Ixia.Job.LoadExternalSongsFromBlokur.csproj
COPY ./Jobs/Ixia.Job.RefillTokens/Ixia.Job.RefillTokens.csproj ./Jobs/Ixia.Job.RefillTokens/Ixia.Job.RefillTokens.csproj



COPY *.props .

RUN dotnet restore

COPY . .

# RUN dotnet build --no-restore -c release

WORKDIR /source/Jobs/Ixia.Job.LoadExternalSongsFromBlokur
RUN dotnet publish  --no-restore -c release -o /app/jobs/LoadExternalSongsFromBlokur

WORKDIR /source/Jobs/Ixia.Job.RefreshSpotifyPopularity
RUN dotnet publish  --no-restore -c release -o /app/jobs/RefreshSpotifyPopularity

WORKDIR /source/Jobs/Ixia.Job.RefillTokens
RUN dotnet publish  --no-restore -c release -o /app/jobs/RefillTokens

WORKDIR /source/Microservices/Authorization/AuthServer
RUN dotnet publish  --no-restore -c release -o /app/auth

WORKDIR /source/Microservices/AssetServer
RUN dotnet publish  --no-restore -c release -o /app/asset

WORKDIR /source/Microservices/NotificationService/NotificationService
RUN dotnet publish  --no-restore -c release -o /app/notification

WORKDIR /source/Microservices/Admin/Frever.AdminService.Api
RUN dotnet publish  --no-restore -c release -o /app/admin

WORKDIR /source/Microservices/Video/Frever.Video.Api
RUN dotnet publish  --no-restore -c release -o /app/video

WORKDIR /source/Microservices/Client/Frever.ClientService.Api
RUN dotnet publish  --no-restore -c release -o /app/client

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app ./