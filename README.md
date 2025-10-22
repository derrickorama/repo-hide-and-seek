# R.E.P.O - Hide and Seek Mod

## Development

### Prerequisites

#### 1) Add your install paths

```
<Project>
  <PropertyGroup>
    <!-- Point this to YOUR local install of REPO -->
    <REPO_DIR>C:\<your_path_to>\Steam\steamapps\common\REPO</REPO_DIR>

    <!-- Optional: override plugin deploy directory -->
    <PluginsDir>$(REPO_DIR)\BepInEx\plugins</PluginsDir>

  </PropertyGroup>
</Project>
```

#### 2) Install BepInEx (mod loader)

1. Download **BepInExPack** for REPO (Windows).
2. Extract **the contents** of the pack **into your REPO folder**
3. **Launch the game once.**  
   If BepInEx is hooked, you’ll see:
    - A console window on startup (often)
    -  New folders: `BepInEx\plugins`, `BepInEx\config`, `BepInEx\log`, and a `LogOutput.log`
    -  If those appeared, BepInEx is installed ✅

#### 3) Use ILSpy to find classes & method

Goal: discover the types/methods you want to hook (e.g., menu controllers, gameplay systems)

- Install ILSpy (or dnSpyEx/rider decompiler).
  Open:
  $REPO_DIR\REPO_Data\Managed\Assembly-CSharp.dll
- Browse namespaces:
  - Look for classes like Menu*, *Controller, \*Manager, etc.
  - Expand a type to see Awake(), Start(), Update(), or custom methods.
  - Note the full type name (namespace + class) and method signatures.
    - Example: Game.UI.Menu.MenuPageMain : MonoBehaviour → method void Start().
    - Tip: If symbols look obfuscated (e.g., a.b.c), still note method signatures and call sites. You can hook them by full name.
