# Contribution Guide

> Mirror's own contribution guide here: [CONTRIBUTING.md](https://github.com/vis2k/Mirror/blob/master/CONTRIBUTING.md).

## Introduction

[SpaceMirror](https://github.com/unitystation/Mirror) is [Unitystation](https://github.com/unitystation/unitystation)'s fork of [Mirror](https://github.com/vis2k/Mirror). Instead of using a copy of Mirror and applying our changes on top, we now maintain our own repository just for Mirror. This makes it significantly easier to update to newer Mirror versions, albeit with more process required to create modifications.

## Developing with SpaceMirror

Development standards follow Mirror's coding conventions instead of Unitystation's guidelines. See their [CONTRIBUTING.md](https://github.com/vis2k/Mirror/blob/master/CONTRIBUTING.md) document for more details, but the main takeaway is four spaces are used here, not tabs. Hopefully your IDE will recognise this for you.

> When developing Unitystation and working with Mirror, it is often useful to see the Mirror code. Now that it is packaged away this isn't available by default.
To change this: from the Unity editor, head to `Edit`->`Preferences`->`External Tools` and under `Generate .csproj files for:` check `Git packages` and hit `Regnerate project files`. You must have an IDE set (for example, Visual Studio Community 2022).

### Updating SpaceMirror

1. Rebase or merge onto [vis2k/Mirror/master](https://github.com/vis2k/Mirror) (or some tag, branch).
2. Increase the SpaceMirror package.json (`./Assets/Mirror/package.json`) version, following `major.minor.patch` rule of thumb.

### Modifying SpaceMirror

1. Consider updating SpaceMirror to the latest Mirror version first.
2. Apply your changes and increase the SpaceMirror package version as above.

#### Testing your changes 
1. Push to your local origin Mirror remote.
2. In the Unitystation repository you can modify the `./UnityProject/Packages/package.json` SpaceMirror entry to target your Mirror remote.

> `Unity Package Manager` lets you target specific branches, tags and commits. This is useful when you want to test an experimental branch, for example. See the syntax rules [here](https://docs.unity3d.com/Manual/upm-git.html#revision).