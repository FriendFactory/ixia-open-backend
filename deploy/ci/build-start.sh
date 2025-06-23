#!/bin/bash
ENV=$1
REV=$2

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

if [[ ${ENV} == "" ]]; then
    echo "Environment name must be specified as first argument"
    exit 1
fi
if [[ ${REV} == "" ]]; then
    echo "Branch or commit must be specified as second argument"
    exit 1
fi

${DIR}/notify-slack.sh "${ENV}" "Build started by ${BUILD_USER} for revision ${REV}"

echo "ACTION_PLAN: ${ACTION_PLAN}, git branch: $(git branch), ENV: ${ENV}, REV: ${REV}"

git config user.name "xxxxxxxxx"
git config user.email "xxxxxxxxx"
git config pull.ff no

if echo ${REV} | grep "origin" ; then
    REV_BRANCH=$(echo "${REV}" | sed -e 's/^origin\///')
else
    REV_BRANCH=${REV}
fi

git checkout -f "${REV_BRANCH}"

# If not in a branch, we are in a commit so we skip bump version and commit.
if [[ "${REV_BRANCH}" != "$(git branch --show-current)" ]]; then
    echo "Not in a branch, skipping version bump in Chart.yaml"
    exit 0
fi

if echo ${REV} | grep "origin" ; then
    git reset --hard origin/${REV_BRANCH}
else
    git reset --hard ${REV_BRANCH}
fi
git pull --rebase

# Source the envirnoment file, we need the appservice repository for version verification
# shellcheck source=${DIR}/../.deploy/${ENV}/.env
source ${DIR}/../.deploy/"${ENV}"/.env
ECR_NAME="$(echo "$APPSERVICE_REPOSITORY_URL" | cut -d/ -f1 --complement)"

FREVER_CHART="${DIR}/../application/helm-chart/frever-app/Chart.yaml"
CURRENT_VER=$(grep appVersion "$FREVER_CHART" | cut -d '"' -f 2)
MAJOR_VER=$(echo "$CURRENT_VER" | cut -d'.' -f 1)
MINOR_VER=$(echo "$CURRENT_VER" | cut -d'.' -f 2)
PATCH_VER=$(set -o pipefail; aws ecr describe-images --repository-name "${ECR_NAME}" --query \
	          "sort_by(imageDetails,& imagePushedAt)[*].imageTags" | \
            jq --sort-keys -r ".[][] | select(.|test(\"^$ENV-${MAJOR_VER}.${MINOR_VER}\"))" | \
		        sort -V -r | head -n1 | cut -d"." -f 3)

if [[ "$?" != 0 ]]; then
    echo "Error: Failed to get image tags from ECR. Aborting."
    exit 1
fi

if [[ ${#PATCH_VER} -gt 6 || "${PATCH_VER}" = "" ]]; then
    echo "PATCH_VER: ${PATCH_VER} is too long or empty, setting to 0"
    PATCH_VER=0
fi

echo "Current Chart appVersion: $CURRENT_VER"

case $ACTION_PLAN in
    breaking_changes)
    # release new major version (+1.x.x)
    (( MAJOR_VER++ )) || true
    MINOR_VER=0
    PATCH_VER=0
    ;;
    deploy_new_minor)
    # release new major version (x.+1.x)
    (( MINOR_VER++ )) || true
    PATCH_VER=0
    ;;
    patch)
    # release new patch version (x.x.+1)
    (( PATCH_VER++ )) || true
    ;;
esac

RELEASE_VER="${MAJOR_VER}.${MINOR_VER}.${PATCH_VER}"
GIT_COMMIT_TAG=$(aws ecr describe-images --repository-name "${ECR_NAME}" --image-ids imageTag="$ENV-$RELEASE_VER" \
                --query "imageDetails[*].imageTags" | jq -r ".[][] | select(.|test(\"$RELEASE_VER\")|not)")
GIT_COMMIT=$(git show -s --format=%H)

if [[ "$RELEASE_VER" != "$CURRENT_VER" || "$GIT_COMMIT" != "$GIT_COMMIT_TAG" ]]; then
   if [[ ! -f $FREVER_CHART ]]; then
	   echo "FREVER_CHART not set correctly"
	   exit 1
   fi
   sed -i -e "s/${CURRENT_VER}/${RELEASE_VER}/" "$FREVER_CHART"
   git add "$FREVER_CHART"
   git commit -m "Bumping version: ${CURRENT_VER} to ${RELEASE_VER}"
   git push
   echo "New version: $RELEASE_VER"
   #Add taggit of code here
   #git tag $RELEASE_VER
fi

if [[ $? != "0" ]]
then
    "${DIR}"/notify-slack.sh "${ENV}" "Error checking out the ${REV} revision"
    exit 1
fi
