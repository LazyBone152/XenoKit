XenoKit is a tool for editing skills and movesets with a 3D Viewer and real-time feedback on changes. It will also be able to load and edit files.

The Outliner:
This is where files/skills/movesets/characters are loaded and managed from. The selected item will become the active one. For characters, they can be set as a Actor from here.

Tabs:
Animation
-Primary
-Face
Camera
State (bcm)
Action (bac)
Projectile (bsa)
Hitbox 
-Melee
-Projectile (shot)
Effect
Audio
-SE
-Voice
System
-Slots/Costumes (characters)
-Define (cus)
-Power Up (PUP)

Files/Saving:
-Do Arg0 duplicate validation on saving, as none is done while editing.

Manual Load:
-Skills/Movesets can be manually loaded
-Only a singular EAN, CAM and VOX can be loaded
-Additional EAN, CAM and VOX can not be added while editing
-System edits (CUS, PUP) not available
-Files will be saved back to their original paths
-On Load, ask for Skill ID and Type (default to X2M ID)

Frame Advance notes:
Simulate(): takes in a bool for this. If false it wont advance frane
Update(): Will advance frame if SceneManager.IsPlaying == true

TimeScale:
-Tail anims have their own timeScale
-Main body anims (Skill/Chara/Common) timeScale applies to when other bacTypes start, but doesnt affect other anim speed
-Main body timeScale does not affect tail anims
-TimeScale type DOES affect tail anims
---
-(TimeScale * ThisAnimTimeScale) = for animation playback
-(TimeScale * MainAnimTimeScale) = for bac frame advance
-MainAnimTimeScale should be on SceneManager, seperate from TimeScale (from bacType)


Known Issues:
-Character VOX, SE and AMK files are always unique and never borrowed - think this is fixed?
-OuterlinerProperties allows editing Arg0 to null (thus it showing "Default"), but then wont allow it to be edited back (because it locks editing of "Default" Arg0s)
-Properties is completely broken (due to rework of how File args work)