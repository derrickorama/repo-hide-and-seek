# R.E.P.O - Hide and Seek Mod

## Development

Add this file to the root of your project:

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
