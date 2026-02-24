#!/bin/bash
# GitHub Setup Script for Boogie Woogie Mod

echo "================================================"
echo "Boogie Woogie Mod - GitHub Setup"
echo "================================================"
echo ""
echo "This script will help you push the mod to GitHub."
echo ""
echo "STEP 1: Create GitHub Repository"
echo "---------------------------------"
echo "Go to: https://github.com/new"
echo ""
echo "Suggested repository name: caves-of-qud-boogie-woogie"
echo "Description: Aoi Todo's Boogie Woogie cursed technique mod for Caves of Qud"
echo "Public repository recommended"
echo "DON'T initialize with README (we already have one)"
echo ""
read -p "Press ENTER once you've created the repository..."

echo ""
echo "STEP 2: Add Remote and Push"
echo "----------------------------"
echo "Detected GitHub username: SattaRIP"
echo ""
read -p "Enter repository name (or press ENTER for 'caves-of-qud-boogie-woogie'): " REPO_NAME
REPO_NAME=${REPO_NAME:-caves-of-qud-boogie-woogie}

REMOTE_URL="https://github.com/SattaRIP/${REPO_NAME}.git"

echo ""
echo "Adding remote: $REMOTE_URL"
git remote add origin "$REMOTE_URL"

echo ""
echo "Pushing to GitHub..."
git push -u origin main

if [ $? -eq 0 ]; then
    echo ""
    echo "================================================"
    echo "SUCCESS! Mod pushed to GitHub"
    echo "================================================"
    echo ""
    echo "Repository URL: https://github.com/SattaRIP/${REPO_NAME}"
    echo ""
    echo "NEXT STEPS:"
    echo "1. Add the preview image (Aoi Todo clapping) as preview.png"
    echo "2. Upload to Steam Workshop via Caves of Qud game menu"
    echo "3. After Steam upload, update workshop.json with Workshop ID"
    echo ""
else
    echo ""
    echo "ERROR: Failed to push to GitHub"
    echo "You may need to:"
    echo "1. Check the repository name is correct"
    echo "2. Authenticate with GitHub (gh auth login or git credential helper)"
    echo "3. Verify the repository exists"
fi
