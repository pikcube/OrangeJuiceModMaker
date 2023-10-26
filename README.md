# 100% Orange Juice Mod Maker

A WPF app I wrote to aid in the creation of 100% Orange Juice mods. Requires an installation of the game on your system to extract the required files.

The app uses the build in file browser to eliminate copying and pasting of file paths, automatically encodes images and songs in the correct format using ffmpeg and imagemagick, and provides an interface to help set important properties.

On first run, the app will unpack game textures into the AppData directory. If you have 100% installed in the default location this will happen automatically, otherwise you'll need to help the application locate your instilation of the game (assuming you bought it through steam, it should be in your steamapps directory.

This app uses Winget to manage version updates and calls the winget upgrade command to check for updates. It uses ffmpeg and ImageMagick to handle conversions. It uses 7zip to unzip .pak files from the game data. By using this app, you agree to running these tools.

The app uses System Version 2 (which is the current mod specification), which is a combination of json and directory structures to manage mods. The mods themselves are always stored in this format and any edits will be immediately previewable in 100% Orange Juice.

Each time you launch the game, the app will check if the game textures have changed and if they have, it will unpack them again (this happens any time new characters are released). Future versions may look into speeding up this process but for now it has to unpack all the contents again.
