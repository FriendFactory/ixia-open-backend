#!/bin/bash

set -u
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
CF_PKEY_FILE=${DIR}/test.pkey
CLOUD_FRONT_CERT_PRIVATE_KEY=$(cat ${CF_PKEY_FILE})

rm -rf ${DIR}/deploy/environment/frever-monitoring/cdk/node_modules

find -name "bin" -type d | xargs rm -rf
find -name "obj" -type d | xargs rm -rf


DOCKER_NETWORK=frever-it-net
REDIS_CONTAINER_NAME=redis-frever-it
POSTGRES_CONTAINER_NAME=postgres-frever-it
BACKEND_CONTAINER_NAME=frever-backend-it

docker network inspect $DOCKER_NETWORK || docker network create $DOCKER_NETWORK

docker ps -q --filter "name=${REDIS_CONTAINER_NAME}" | xargs -r docker stop
docker ps -q -a --filter "name=${REDIS_CONTAINER_NAME}" | xargs -r docker container rm
docker run --network ${DOCKER_NETWORK} -d --rm --name ${REDIS_CONTAINER_NAME} redis/redis-stack-server:latest


docker ps -q --filter "name=${POSTGRES_CONTAINER_NAME}" | xargs -r docker stop
docker ps -q -a --filter "name=${POSTGRES_CONTAINER_NAME}" | xargs -r docker container rm
docker run --network ${DOCKER_NETWORK} -d --rm --name ${POSTGRES_CONTAINER_NAME} -e POSTGRES_PASSWORD=test -e POSTGRES_USER=test -e POSTGRES_DB=frevertest postgis/postgis

echo "Waiting until Postgres is ready..."

bash -c "until docker exec $POSTGRES_CONTAINER_NAME pg_isready ; do sleep 5 ; done"
sleep 30

echo "Postgres is ready! Start test run"

POSTGRES_CONNECTION_STRING="Host=${POSTGRES_CONTAINER_NAME};Port=5432;Database=frevertest;Username=test;Password=test";
REDIS_CONNECTION_STRING="${REDIS_CONTAINER_NAME}:6379,allowAdmin=true"


docker build -t ${BACKEND_CONTAINER_NAME}:latest -f ${DIR}/deploy/application/test.Dockerfile ${DIR}

docker ps -q --filter "name=${BACKEND_CONTAINER_NAME}" | xargs -r docker stop
docker ps -q -a --filter "name=${BACKEND_CONTAINER_NAME}" | xargs -r docker container rm
docker run --network ${DOCKER_NETWORK} --rm --name ${BACKEND_CONTAINER_NAME} --volume ${DIR}/test-results/:/host/ \
    -e JAEGER_DETAILED_TRACING=true \
    -e JAEGER_ENABLE=false \
    -e JAEGER_SERVICE_NAME=auth \
    -e JAEGER_ENDPOINT=http://localhost:4317 \
    -e ASPNETCORE_ENVIRONMENT=test \
    -e Logging__LogLevel__Default=Trace \
    -e AWS_REGION=eu-central-1 \
    -e AWS_ACCESS_KEY_ID=xxxxxxxxx \
    -e AWS_SECRET_ACCESS_KEY=xxxxxxxxx \
    -e AWS__Region=eu-central-1 \
    -e AWS__bucket_name=frever-i-dont-exists \
    -e RunMigrations=false \
    -e Redis__ConnectionString=${REDIS_CONNECTION_STRING} \
    -e Redis__ClientIdentifier=Local \
    -e AWS__AssetCopyingQueue="xxxxxxxxx" \
    -e ServiceDiscovery__Asset="http://localhost:5004/file-storage/" \
    -e ServiceDiscovery__Auth="http://localhost:5002/auth/" \
    -e ServiceDiscovery__Client="http://localhost:5011/client/" \
    -e ServiceDiscovery__Chat="http://localhost:5013/chat/" \
    -e ServiceDiscovery__Main="http://localhost:5001/main/" \
    -e ServiceDiscovery__Notification="http://localhost:5008/notification/" \
    -e ServiceDiscovery__Social="http://localhost:5007/social/" \
    -e ServiceDiscovery__Video="http://localhost:5005/video/" \
    -e ServiceDiscovery__VideoFeed="http://localhost:5012/videofeed/" \
    -e ServiceDiscovery__MachineLearning="https://localhost.localdomain:8811" \
    -e ConnectionStrings__MainDbWritable=${POSTGRES_CONNECTION_STRING} \
    -e ConnectionStrings__MainDbReadReplica=${POSTGRES_CONNECTION_STRING} \
    -e ConnectionStrings__AuthDbWritable=${POSTGRES_CONNECTION_STRING} \
    -e ConnectionStrings__TestDbWritable=${POSTGRES_CONNECTION_STRING} \
    -e ConnectionStrings__TestDbReadReplica=${POSTGRES_CONNECTION_STRING} \
    -e CloudFrontHost="xxxxxxxxx" \
    -e ExternalUrls__Auth="http://localhost:5002/auth/" \
    -e ExternalUrls__Main="http://localhost:5001/main/" \
    -e ExternalUrls__Asset="http://localhost:5004/file-storage/" \
    -e ExternalUrls__Video="http://localhost:5005/video/" \
    -e ExternalUrls__Social="http://localhost:5007/social/" \
    -e ExternalUrls__Notification="http://localhost:5008/notification/" \
    -e ExternalUrls__AssetManager="http://localhost:5010/admin/" \
    -e ExternalUrls__Client="http://localhost:5011/client/" \
    -e ExternalUrls__Chat="http://localhost:5013/chat/" \
    -e AssetService__AssetCdnHost="xxxxxxxxx" \
    -e AssetCopying__AssetCopyingQueueUrl="xxxxxxxxx" \
    -e AssetCopying__BucketName="frever-i-dont-exists" \
    -e ModulAI__ServiceUrl="http://localhost:8011/" \
    -e ApiIdentifier="0.0" \
    -e EnvironmentType=test \
    -e AcrCloud__Host="xxxxxxxxx" \
    -e AcrCloud__AccessKey="xxxxxxxxx" \
    -e AcrCloud__AccessSecret="xxxxxxxxx" \
    -e AbstractApi__ApiKey="xxxxxxxxx" \
    -e MusicProviderApiSettings__TrackDetailsUrl=https://api.7digital.com/1.2/track/details \
    -e MusicProviderApiSettings__ApiUrl=https://api.7digital.com/1.2 \
    -e MusicProviderApiSettings__CountryCode=SE \
    -e MusicProviderApiSettings__UsageTypes="download,subscriptionstreaming,adsupportedstreaming" \
    -e MusicProviderOAuthSettings__OAuthConsumerKey="xxxxxxxxx" \
    -e MusicProviderOAuthSettings__OAuthConsumerSecret="xxxxxxxxx" \
    -e MusicProviderOAuthSettings__OAuthSignatureMethod="HMAC-SHA1" \
    -e MusicProviderOAuthSettings__OAuthVersion="1.2" \
    -e MediaConverterQueue=test-queue \
    -e OnboardingOptions__FreverOfficialEmail="xxxxxxxxx" \
    -e OnboardingOptions__LikeVideoTaskSortOrder="2" \
    -e OnboardingOptions__RequiredTaskCount="2" \
    -e OnboardingOptions__RequiredVideoCount="10" \
    -e Templates__ThumbnailConversionJob=template-thumbnail \
    -e Templates__ConversionJobRoleArn=xxxxxxxxx \
    -e Templates__UserTemplatesSubCategoryName="Frever Stars" \
    -e ModerationProviderApiSettings__HiveTextModerationKey=8idontexist \
    -e ModerationProviderApiSettings__HiveVisualModerationKey=88fakesecret99 \
    -e RateLimit__FreverVideoAndAssetDownload="10" \
    -e RateLimit__SevenDigitalSongDownload="5" \
    -e RateLimit__HardLimitPerUserPerHour="50000" \
    -e Blokur__ApiToken=xxxxxxxxx \
    -e Blokur__TempFolder=/Users/sergiitokariev/Downloads \
    -e Blokur__SevenDigitalProviderIdentifier=112233 \
    -e ChatServiceOptions__ReportMessageEmail="xxxxxxxxx" \
    -e ReportMessageEmail="xxxxxxxxx" \
    -e SpotifyPopularity__Bucket="frever-analytics-misc" \
    -e SpotifyPopularity__Prefix="spotify/popularity" \
    -e SpotifyPopularity__FullDataCsvFileName="spotify_popularity_full.csv" \
    -e AppsFlyerSettings__AndroidAppId=xxxxxxxxx \
    -e AppsFlyerSettings__AppleAppId=xxxxxxxxx \
    -e AppsFlyerSettings__Token="xxxxxxxxx" \
    -e Twilio__Secret=1234567890 \
    -e Twilio__Sid=AC1234567890 \
    -e Twilio__MessagingServiceSid=MG1234567890 \
    -e Twilio__VerifyServiceSid=VA123345678 \
    -e AI__StableDiffusionApiKey=sk-1239494994944 \
    -e AI__ReplicateApiKey=r8_1234456789 \
    -e AI__KlingAccessKey=1234567890 \
    -e AI__KlingSecretKey=1234567890 \
    -e IngestVideoS3BucketName=frever-i-dont-exists \
    -e DestinationVideoS3BucketName=frever-i-dont-exists \
    -e CloudFrontHost="xxxxxxxxx" \
    -e CloudFrontCertPrivateKey="${CLOUD_FRONT_CERT_PRIVATE_KEY}" \
    -e CloudFrontCertKeyPairId=xxxxxxxxx \
    -e ConvertJobTemplateName=video-conversion \
    -e VideoThumbnailJobTemplateName=video-thumbnail \
    -e TranscodingJobTemplateName=ExtractMp3FromVideo \
    -e ConvertJobRoleArn="xxxxxxxxx" \
    -e VideoPlayerPageUrl="https://video.frever.com/" \
    -e VideoReportNotificationEmail="xxxxxxxxx" \
    -e VideoConversionSqsQueue="xxxxxxxxx" \
    -e ConversionJobSqsQueue="xxxxxxxxx" \
    -e ComfyUiApiSettings__QueueUrl="xxxxxxxxx" \
    -e MediaConverterQueue="test" \
    -e ExtractAudioQueue="test" \
    -e DeleteAccountEmail="xxxxxxxxx" \
    -e FreverOfficialEmail="xxxxxxxxx" \
    -e ContentGenerationOptions__ApiKeySalt="xxxxxxxxx" \
    -e ContentGenerationOptions__ApiKeyHash="xxxxxxxxx" \
    -e AI__PixVerseApiKey="api-key" \
    ${BACKEND_CONTAINER_NAME}:latest ./execute-tests.sh


set +e

docker ps -q --filter "name=${REDIS_CONTAINER_NAME}" | xargs -r docker stop
docker ps -q --filter "name=${POSTGRES_CONTAINER_NAME}" | xargs -r docker stop
docker ps -q --filter "name=${BACKEND_CONTAINER_NAME}" | xargs -r docker stop
docker image remove -f ${BACKEND_CONTAINER_NAME}
docker network rm ${DOCKER_NETWORK}