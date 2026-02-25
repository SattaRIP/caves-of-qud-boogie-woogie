# Steam Workshop Upload - Common Issues & Solutions

## CRITICAL: k_EResultFileNotFound Error

**Error Messages:**
- "Unknown result: k_EResultFileNotFound"
- "Item invalid ... dunno why! :("

**Root Cause:**
The workshop.json file references `"ImagePath": "preview.png"` but the preview.png file doesn't exist in the mod directory.

**Solution:**
Steam Workshop REQUIRES the preview image file to exist if ImagePath is specified in workshop.json.

### Option 1: Create Placeholder Image (RECOMMENDED)
```bash
cd "/path/to/mod/directory"
magick -size 512x512 xc:transparent preview.png
```
This creates a transparent 512x512 PNG that satisfies Steam's requirement.

### Option 2: Remove ImagePath from workshop.json
Remove the `"ImagePath"` line entirely from workshop.json, then add the preview image through Steam Workshop's web interface after upload.

**This issue has occurred with:**
- BoogieWoogie mod (2024-02-24)
- [Add other mods as they encounter this]

## Why This Happens

When you specify `"ImagePath": "preview.png"` in workshop.json, Steam's upload process:
1. Validates all referenced files exist
2. Fails with k_EResultFileNotFound if ANY referenced file is missing
3. Shows vague "Item invalid" error instead of specifying which file

## Prevention Checklist

Before uploading ANY mod to Steam Workshop:

- [ ] Check if workshop.json has `"ImagePath"` field
- [ ] If yes, verify the referenced image file exists
- [ ] If no image exists, either create placeholder or remove ImagePath
- [ ] Recommended image specs: 512x512 or 1024x1024 PNG
- [ ] After successful upload, replace placeholder with actual preview image

## Quick Fix Command

Run this in any mod directory before uploading:
```bash
if grep -q '"ImagePath"' workshop.json; then
    IMAGE=$(grep '"ImagePath"' workshop.json | cut -d'"' -f4)
    if [ ! -f "$IMAGE" ]; then
        echo "Creating placeholder: $IMAGE"
        magick -size 512x512 xc:transparent "$IMAGE"
    fi
fi
```

## Remember

**ALWAYS create preview.png (even if blank) when ImagePath is in workshop.json!**

This is NOT optional - Steam will reject the upload otherwise.
