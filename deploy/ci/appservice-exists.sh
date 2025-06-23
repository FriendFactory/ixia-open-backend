#!/bin/bash
# Returns exit code 1 if image exists in ECR
# consisting of either version number or current git commit hash.
# otherwise, returns 0

GIT_HASH=$(git log -1 --format="%H")
ENV=$1
CURRENT_VERSION=$1-$2
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
# Source the environment file, we need the appservice repository for version verification
# shellcheck source=${DIR}/../.deploy/${ENV}/.env
source "${DIR}/../.deploy/${ENV}/.env"
ECR_NAME="$(echo "$APPSERVICE_REPOSITORY_URL" | cut -d/ -f1 --complement)"

if aws ecr describe-images --repository-name "$ECR_NAME" --image-ids imageTag="$GIT_HASH" >/dev/null 2>&1 &&
  ! aws ecr describe-images --repository-name "$ECR_NAME" --image-ids imageTag="$CURRENT_VERSION" >/dev/null 2>&1; then
  MANIFEST=$(aws ecr batch-get-image --repository-name appservice --image-ids imageTag="$GIT_HASH" --output json | jq --raw-output --join-output '.images[0].imageManifest')
  aws ecr put-image --repository-name appservice --image-tag "$CURRENT_VERSION" --image-manifest "$MANIFEST"
  echo "Only found version $GIT_HASH of $ECR_NAME in ECR, so tagging it with $CURRENT_VERSION."
  exit 1
fi

# Check if image exists tagged with version number and git hash
if aws ecr describe-images --repository-name "$ECR_NAME" --image-ids imageTag="$CURRENT_VERSION" >/dev/null 2>&1 &&
    aws ecr describe-images --repository-name "$ECR_NAME" --image-ids imageTag="$GIT_HASH" >/dev/null 2>&1; then
		echo "Found version $CURRENT_VERSION and $GIT_HASH of $ECR_NAME in ECR."
		exit 1
fi

# If we get here, we had no match in the repository, so we'll greenlight build.
echo "No $ECR_NAME image found in ECR for version \"$CURRENT_VERSION\" and git commit hash \"$GIT_HASH\"."
exit 0
