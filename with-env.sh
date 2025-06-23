#!/bin/bash

### Opens Frever backend solution in Rider with pre-configured environment variables
### with configuration to specified environment

### The real HiveTextModerationKey and HiveVisualModerationKey could be found at https://friendfactory.atlassian.net/browse/FREV-12914

set -u
set -e

ENV=$1

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ENV_FILE=~/dev/frever/configs/.env.${ENV}
CF_PKEY_FILE=~/dev/frever/configs/.${ENV}.cf-rsa-pkey

cat ${ENV_FILE} > /dev/null
cat ${CF_PKEY_FILE} > /dev/null

if [[ $? != "0" ]]; then
    echo "Error configuring environment"
    exit 1
fi

export $(cat ${ENV_FILE} | xargs)

CLOUD_FRONT_CERT_PRIVATE_KEY=$(cat ${CF_PKEY_FILE})

env \
    JAEGER_DETAILED_TRACING=true \
    JAEGER_ENABLE=false \
    JAEGER_SERVICE_NAME=auth \
    JAEGER_ENDPOINT=http://localhost:4317 \
    ASPNETCORE_ENVIRONMENT=${ENV_NAME} \
    Logging__LogLevel__Default=${LOG_LEVEL} \
    AWS_REGION=${AWS_REGION} \
    AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID} \
    AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY} \
    AWS__Region=eu-central-1  \
    AWS__bucket_name=${AWS_BUCKET} \
    RunMigrations=${RUN_MIGRATIONS} \
    Redis__ConnectionString=${REDIS_CONNECTION_STRING} \
    Redis__ClientIdentifier=Local \
    AWS__AssetCopyingQueue=${QUEUE_ASSET_COPYING} \
    ServiceDiscovery__Asset=${SERVER_ASSET} \
    ServiceDiscovery__Auth=${SERVER_AUTH} \
    ServiceDiscovery__Client=${SERVER_CLIENT} \
    ServiceDiscovery__Chat=${SERVER_CHAT} \
    ServiceDiscovery__Main=${SERVER_MAIN} \
    ServiceDiscovery__Notification=${SERVER_NOTIFICATION} \
    ServiceDiscovery__Social=${SERVER_SOCIAL} \
    ServiceDiscovery__Video=${SERVER_VIDEO} \
    ServiceDiscovery__VideoFeed=${SERVER_VIDEOFEED} \
    ServiceDiscovery__MachineLearning="https://localhost.localdomain:8811" \
    ConnectionStrings__MainDbWritable=${MAIN_DB_CONNECTION_STRING} \
    ConnectionStrings__MainDbReadReplica=${MAIN_DB_CONNECTION_STRING} \
    ConnectionStrings__AuthDbWritable=${AUTH_DB_CONNECTION_STRING} \
    ConnectionStrings__TestDbWritable=${TEST_DB_CONNECTION_STRING} \
    ConnectionStrings__TestDbReadReplica=${TEST_DB_CONNECTION_STRING} \
    CloudFrontHost=${ASSET_CDN_HOST} \
    ExternalUrls__Auth=${SERVER_AUTH} \
    ExternalUrls__Main=${SERVER_MAIN} \
    ExternalUrls__Asset=${SERVER_ASSET} \
    ExternalUrls__Video=${SERVER_VIDEO} \
    ExternalUrls__Social=${SERVER_SOCIAL} \
    ExternalUrls__Notification=${SERVER_NOTIFICATION} \
    ExternalUrls__AssetManager=${SERVER_ADMIN} \
    ExternalUrls__Client=${SERVER_CLIENT} \
    ExternalUrls__Chat=${SERVER_CHAT} \
    AssetService__AssetCdnHost=${ASSET_CDN_HOST} \
    AssetCopying__AssetCopyingQueueUrl=${QUEUE_ASSET_COPYING} \
    AssetCopying__BucketName=${AWS_BUCKET} \
    ModulAI__ServiceUrl=${SERVER_MODULAI_RECOMMENDATIONS} \
    ApiIdentifier="0.0" \
    EnvironmentType=${ENV_TYPE} \
    AcrCloud__Host="xxxxxxxxx" \
    AcrCloud__AccessKey="xxxxxxxxx" \
    AcrCloud__AccessSecret="xxxxxxxxx" \
    AbstractApi__ApiKey="xxxxxxxxx" \
    MusicProviderApiSettings__TrackDetailsUrl=https://api.7digital.com/1.2/track/details \
    MusicProviderApiSettings__ApiUrl=https://api.7digital.com/1.2 \
    MusicProviderApiSettings__CountryCode=SE \
    MusicProviderApiSettings__UsageTypes="download,subscriptionstreaming,adsupportedstreaming" \
    MusicProviderOAuthSettings__OAuthConsumerKey="xxxxxxxxx" \
    MusicProviderOAuthSettings__OAuthConsumerSecret="xxxxxxxxx" \
    MusicProviderOAuthSettings__OAuthSignatureMethod="HMAC-SHA1" \
    MusicProviderOAuthSettings__OAuthVersion="1.2" \
    MediaConverterQueue=${MEDIA_CONVERTER_QUEUE} \
    OnboardingOptions__FreverOfficialEmail="xxxxxxxxx" \
    OnboardingOptions__LikeVideoTaskSortOrder="2" \
    OnboardingOptions__RequiredTaskCount="2"\
    OnboardingOptions__RequiredVideoCount="10"\
    Templates__ThumbnailConversionJob=template-thumbnail \
    Templates__ConversionJobRoleArn=xxxxxxxxx \
    Templates__UserTemplatesSubCategoryName="Frever Stars" \
    ModerationProviderApiSettings__HiveTextModerationKey=${HIVE_TEXT_MODERATION_KEY} \
    ModerationProviderApiSettings__HiveVisualModerationKey=${HIVE_VISUAL_MODERATION_KEY} \
    RateLimit__FreverVideoAndAssetDownload="10" \
    RateLimit__SevenDigitalSongDownload="5" \
    RateLimit__HardLimitPerUserPerHour="50000" \
    Blokur__ApiToken=xxxxxxxxx \
    Blokur__TempFolder=/Users/sergiitokariev/Downloads \
    Blokur__SevenDigitalProviderIdentifier=112233 \
    ChatServiceOptions__ReportMessageEmail="xxxxxxxxx" \
    ReportMessageEmail="xxxxxxxxx" \
    SpotifyPopularity__Bucket="frever-analytics-misc" \
    SpotifyPopularity__Prefix="spotify/popularity" \
    SpotifyPopularity__FullDataCsvFileName="spotify_popularity_full.csv" \
    AppsFlyerSettings__AndroidAppId=xxxxxxxxx \
    AppsFlyerSettings__AppleAppId=xxxxxxxxx \
    AppsFlyerSettings__Token="xxxxxxxxx" \
    InAppPurchases__GoogleApiKeyBase64="xxxxxxxxx" \
    Twilio__Secret=${TWILIO_SECRET} \
    Twilio__Sid=${TWILIO_SID} \
    Twilio__MessagingServiceSid=${TWILIO_MESSAGING_SERVICE_SID} \
    Twilio__VerifyServiceSid=${TWILIO_VERIFICATION_SERVICE_SID} \
    AI__StableDiffusionApiKey=${STABLE_DIFFUSION_API_KEY} \
    AI__ReplicateApiKey=${REPLICATE_API_KEY} \
    AI__KlingAccessKey=112233 \
    AI__KlingSecretKey=112233 \
    AI__PixVerseApiKey="api-key" \
    AppStoreApi__KeyId="${APP_STORE_API_KEY_ID}" \
    AppStoreApi__IssuerId="${APP_STORE_API_ISSUER_ID}" \
    AppStoreApi__KeyDataBase64="${APP_STORE_KEY_DATA_BASE64}" \
    AppStoreApi__SharedSecret="${APP_STORE_API_SHARED_SECRET}" \
    IngestVideoS3BucketName=${AWS_BUCKET} \
    DestinationVideoS3BucketName=${AWS_BUCKET} \
    CloudFrontHost=${CLOUD_FRONT_HOST} \
    CloudFrontCertPrivateKey="${CLOUD_FRONT_CERT_PRIVATE_KEY}" \
    CloudFrontCertKeyPairId=${CLOUD_FRONT_CERT_KEY_PAIR_ID} \
    ConvertJobTemplateName=video-conversion \
    VideoThumbnailJobTemplateName=video-thumbnail \
    TranscodingJobTemplateName=ExtractMp3FromVideo \
    ConvertJobRoleArn="xxxxxxxxx" \
    VideoPlayerPageUrl="https://video.frever.com/" \
    VideoReportNotificationEmail="xxxxxxxxx" \
    VideoConversionSqsQueue=${VIDEO_CONVERSION_SQS_QUEUE} \
    ConversionJobSqsQueue=${CONVERSION_JOB_SQS_QUEUE} \
    MediaConverterQueue=${MEDIA_CONVERT_QUEUE} \
    ExtractAudioQueue=${MEDIA_CONVERT_QUEUE} \
    DeleteAccountEmail="xxxxxxxxx" \
    FreverOfficialEmail="xxxxxxxxx" \
    ContentGenerationOptions__ApiKeySalt="xxxxxxxxx" \
    ContentGenerationOptions__ApiKeyHash="xxxxxxxxx" \
    ComfyUiApiSettings__QueueUrl="xxxxxxxxx" \
    ComfyUiApiSettings__ResponseQueueUrl="xxxxxxxxx" \
    open Server.sln
