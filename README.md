# AvatarPaint - Sansar Scripting Demo

This repository provides a Sansar scripting demo, "AvatarPaint," which allows avatars to appear painted with a specific color or be randomly colorized on a per-material basis. This effect is triggered by avatar interactions with designated trigger volumes, and avatars can be "cleansed" of the effect in a separate cleansing area. The demo provides two main scripts:

![Screenshot of AvatarPaint in Sansar](screenshot1.jpg)

1. **FLS_PaintBucket_Paint_1a.cs** - Applies a color or random tint to avatars when they enter a paint trigger volume.
2. **FLS_PaintBucket_Cleanser_1a.cs** - Restores avatars' original material properties when they enter the cleansing area trigger volume.

### Repository

- **Repository Name:** [sansar-avatarpaint](https://github.com/iamfreelight/sansar-avatarpaint)

## Scripts Overview

### FLS_PaintBucket_Paint_1a.cs
This script applies color or random tint to avatars upon entering the specified paint trigger volume.

- **Features**:
  - `Randomize All Materials` (toggle): If enabled, each avatar material is tinted a different random color.
  - `Colorize Color`: When randomization is off, applies a uniform color to all avatar materials.
  - `Emissive Level`: Adjusts the emissive intensity of the applied colors.

### FLS_PaintBucket_Cleanser_1a.cs
This script restores avatars' original colors and emissive properties when they enter the cleansing area trigger volume.

- **Features**:
  - `rbTriggerSpawnpoint`: Tracks each avatar’s original material properties when they enter the scene or designated area
  - `rbTriggerCleanserArea`: Restores avatars' original colors and emissive values upon entry.
  - `Cleansing Speed`: Controls the duration for which the cleansing effect fades in.
  - Maintains a record of each avatar’s original materials properties to enable restoration.

## How to Use
1. Set up trigger volumes in your scene.
   - **Paint Trigger Volume**: Assign `FLS_PaintBucket_Paint_1a` to volumes where avatars can get "painted."
   - **Cleansing Trigger Volume**: Assign `FLS_PaintBucket_Cleanser_1a` to a designated "rinse" area.
2. Configure the color and emissive level in the `FLS_PaintBucket_Paint_1a` script and the cleansing speed in the `FLS_PaintBucket_Cleanser_1a` script.
3. Drop these scripts onto respective volumes in your scene to create the interactive avatar "painting" and "cleansing" experience.

## Demo
A pre-configured demo is available for free download on the Sansar store: [AvatarPaint Demo](https://store.sansar.com/freelight).
This demo is also setup in my Sansar world '[Scripting Experiments]()'
