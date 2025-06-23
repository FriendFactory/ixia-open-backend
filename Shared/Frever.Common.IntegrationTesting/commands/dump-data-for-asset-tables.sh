#!/bin/bash

set -e

PG_DUMP=/Applications/Postgres.app/Contents/Versions/14/bin/pg_dump         # Replace with /usr/bin if you have full installation
OUT_DIR=/Users/sergiitokariev/dev/frever/db-backups/              # Replace with suitable directory
DB_NAME="frever_main_full"

${PG_DUMP} --file="${OUT_DIR}/asset-data.sql" --dbname=${DB_NAME} --inserts  \
    -t '"CharacterSpawnPosition"' \
    -t '"CharacterSpawnPositionAndFormation"' \
    -t '"BodyAnimation"' \
    -t '"BodyAnimationAndBodyAnimationCategory"' \
    -t '"BodyAnimationAndCharacterSpawnPosition"' \
    -t '"Color"' \
    -t '"ExternalPlaylist"' \
    -t '"LightSettings"' \
    -t '"MovementType"' \
    -t '"OnboardingQuestGroup"' \
    -t '"OnboardingQuest"' \
    -t '"OnboardingReward"' \
    -t '"Prop"' \
    -t '"PropAndBodyAnimation"' \
    -t '"PropAndSetLocation"' \
    -t '"PropCategory"' \
    -t '"PropSubCategory"' \
    -t '"PropWorldSize"' \
    -t '"SetLocation"' \
    -t '"SetLocationAndCharacteSpawnPosition"' \
    -t '"SetLocationBundle"' \
    -t '"Song"' \
    -t '"ThemeCollection"' \
    -t '"ThemeCollectionAndWardrobe"' \
    -t '"UmaAsset"' \
    -t '"UmaAssetFile"' \
    -t '"UmaAssetFileAndUnityAssetType"' \
    -t '"UmaBundle"' \
    -t '"UmaBundleAllDependency"' \
    -t '"UmaBundleDirectDependency"' \
    -t '"Vfx"' \
    -t '"VfxPositionGroup"' \
    -t '"VfxPositionGroupAndVfxSpawnPosition"' \
    -t '"VfxSpawnPosition"' \
    -t '"Wardrobe"' \
    --data-only \
    --no-privileges \
    --no-acl \
    --disable-triggers \
    --no-owner \
    --no-comments

aws s3 cp "${OUT_DIR}/asset-data.sql" xxxxxxxxx

## PER TABLE VARIANT

# TABLES=(SetLocationAndCharacterSpawnPosition CharacterSpawnPosition CharacterSpawnPositionAndFormation BodyAnimation
#  BodyAnimationAndBodyAnimationCategory BodyAnimationAndCharacterSpawnPosition
#  Color ExternalPlaylist LightSettings MovementType OnboardingQuestGroup OnboardingQuest
#  OnboardingReward Prop PropAndBodyAnimation PropAndSetLocation PropCategory PropSubCategory
#  PropWorldSize SetLocation
#  SetLocationBackground SetLocationBackgroundSettings
#  SetLocationBundle Album Song ThemeCollection ThemeCollectionAndWardrobe
#  UmaAsset UmaAssetFile UmaAssetFileAndUnityAssetType UmaBundle UmaBundleAllDependency UmaBundleDirectDependency
#  Vfx VfxPositionGroup VfxPositionGroupAndVfxSpawnPosition VfxSpawnPosition Wardrobe)


# for T in ${TABLES[@]}; do
#     TABLE_NAME=\'\"${T}\"\'

#     echo ${T}

#     ${PG_DUMP} --file="${OUT_DIR}/${T}.sql" --dbname=${DB_NAME} --inserts  \
#         -t \"${T}\" \
#         --data-only \
#         --no-privileges \
#         --no-acl \
#         --disable-triggers \
#         --no-owner \
#         --no-comments
# done
