#!/bin/bash

# Validate inputs
if [[ $# -ne 3 ]]; then
  echo "Usage: $0 '<commit_message>' '<issue_number>' '<tag>'"
  exit 1
fi

COMMIT_MESSAGE=$1
ISSUE_NUMBER=$2
TAG=$3
TIMESTAMP=$(date +%Y%m%d%H%M%S)
BRANCH_NAME="feature/issue-${ISSUE_NUMBER}-${TIMESTAMP}"

# Create a new branch
git checkout -b $BRANCH_NAME

# Clean appsettings file
if [ -f "src/appsettings.Example.json" ]; then
  echo "Cleaning appsettings.Example.json..."
  sed -i.bak 's/"secret_key": "[^"]*"/"secret_key": "REPLACE_ME"/' src/appsettings.Example.json
  git add src/appsettings.Example.json
fi

# Make a commit
git add .
git commit -m "$COMMIT_MESSAGE" -m "Fixes #$ISSUE_NUMBER"

# Push the new branch
git push -u origin $BRANCH_NAME

# Create a local tag
git tag $TAG

# Attempt to push the tag
if ! git push origin $TAG; then
  echo "Warning: Could not push tag '$TAG'. It can be created after the PR merge."
fi

# Instructions for PR
echo "Open a pull request for branch '$BRANCH_NAME' at your repository."
echo "If the tag could not be pushed, please create it after the merge."