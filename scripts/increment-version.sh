#!/bin/bash
# Bash script to increment version in .csproj file

set -e

VERSION_BUMP="${1:-patch}"  # Default to patch if not specified

if [[ ! "$VERSION_BUMP" =~ ^(major|minor|patch)$ ]]; then
    echo "Error: Version bump must be 'major', 'minor', or 'patch'"
    exit 1
fi

CSPROJ_PATH="Buser.AsyncCompression/Buser.AsyncCompression.csproj"

if [ ! -f "$CSPROJ_PATH" ]; then
    echo "Error: Project file not found: $CSPROJ_PATH"
    exit 1
fi

# Extract current version using sed/grep
CURRENT_VERSION=$(grep -oP '<Version>\K[^<]+' "$CSPROJ_PATH" | head -1)

if [ -z "$CURRENT_VERSION" ]; then
    echo "Error: Version not found in project file"
    exit 1
fi

echo "Current version: $CURRENT_VERSION"

# Parse version components
IFS='.' read -ra VERSION_PARTS <<< "$CURRENT_VERSION"
MAJOR="${VERSION_PARTS[0]}"
MINOR="${VERSION_PARTS[1]}"
PATCH="${VERSION_PARTS[2]}"

# Increment version based on bump type
case "$VERSION_BUMP" in
    major)
        MAJOR=$((MAJOR + 1))
        MINOR=0
        PATCH=0
        ;;
    minor)
        MINOR=$((MINOR + 1))
        PATCH=0
        ;;
    patch)
        PATCH=$((PATCH + 1))
        ;;
esac

NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "New version: $NEW_VERSION"

# Update version in .csproj using sed
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$CSPROJ_PATH"
else
    # Linux
    sed -i "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$CSPROJ_PATH"
fi

echo "Version updated successfully to $NEW_VERSION"
echo "$NEW_VERSION"

