* `package` - installable `UPM`
* `samples` - Unity sample projects
* `src` - package source code

# package

Install via `https://github.com/semuserable/com.example.semus-2.git?path=/package`

# src

* initialize
```bash
git clone
git submodule update --init --recursive
```
* make sure `Directory.Build.props` contains valid `Unity` path
```xml
<PropertyGroup Condition="Exists('C:\Program Files\Unity\Hub\Editor\$(UnityVersion)\Editor\Data\Managed\UnityEngine.dll')">
    <UnityPath>C:\Program Files\Unity\Hub\Editor\$(UnityVersion)\Editor\Data\Managed</UnityPath>
</PropertyGroup>
```
* open up `Libraries.sln` and do the development

# samples

## unity-of-bugs

`UPM` project is already included. Open up `packages/manifest.json` to see all included packages.
```json
{
    "dependencies": {
        "com.example.semus": "file:../../../package"
    }
}
```

* open the project in `Unity`
* inside the project open `Scenes/BugFarmScene` scene
* navigate to `Component/Sentry` and include valid `DSN`
* play the scene and throw some exceptions
* observe `Issues` on `sentry.io` 
