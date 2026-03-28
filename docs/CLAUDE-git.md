# Git & LFS

- Binary assets (`.fbx`, `.png`, `.jpg`, `.wav`, `.mp3`, `.ogg`, `.psd`, `.tga`, `.tif`, `.tiff`, `.exr`, `.obj`, `.mp4`, `.jpeg`) are tracked by Git LFS
- `.unity`, `.prefab`, `.asset`, `.mat` files are text-serialized YAML — not LFS tracked (correct)
- Generated files (`.csproj`, `.sln`, `Library/`, `Temp/`) are gitignored
