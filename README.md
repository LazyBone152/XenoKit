XenoKit is an tool that can preview and edit many aspects of skills and characters in DB Xenoverse 2. It features a 3D viewport where changes can be made and viewed in real time.

This is an early ALPHA release. Expect stuff to break and for some features to be incomplete or to be not yet implemented. 


*****************************************************
Feature Overview
*****************************************************

Viewport:

-Can preview characters, animations, cameras, hitboxes and effects, depending on what is selected in the editor

-The original game shaders are used, so characters and effects should look similar to the actual game

Animation:

-Copy/paste of animations, keyframes, and bones

-Direct keyframe and bone manipulation

-Rescale, reverse, offset and other similar features

Camera:

-Create new cameras from scratch, using the viewport to set keyframes

-Copy/paste of cameras and keyframes

-Direct keyframe manipulation

-Rescale, reverse, offset, shakes

Action (BAC):

-Preview of BAC entries (animations, cameras, audio, hitboxes and time scale are all simulated)

-Create and modify BAC entries and types

-Copy/paste of BAC entries and BAC types (automatically import any references, such as animations or audio)

-Find/replace feature

Audio:

-Uses an embedded version of ACE for audio editing

-Preview of cues within BAC entries

Effect:

-Preview of some effect types (EMP, ECF, EMO, LIGHT). Some particle (EMP) types are not implemented yet, however.

-Uses an embedded version of EEPK Organiser for effect editing (will have UI scaling issues if the window isn't big enough)

BCS:

-PartSet, Bodies and Color editing and previewing

-List of all files used by the character, with editing possible for supported file types (emd, emb, emm).

-The Model Viewer allows individual EMD files to be viewed and edited

Files:

-Skills and characters are loaded directly from the game. They cant be created in the editor (yet).

-Some files can be manually loaded by dropping them onto the window (eepk, acb, nsk)

-Viewer Mode functions like XenoViewer where you can drag model files in and immediately view them. Can do most things Xenoviewer can do, along with additional features such as editing and reloading.

CAC:

-Custom characters can be loaded from a user save file and displayed in the editor as an actor (in place of a regular character). Some appearence editing options are available.


*****************************************************
Building
*****************************************************
You must have the main project (https://github.com/LazyBone152/XV2-Tools) at the same directory level when building, since XenoKit relies on it. Build as a x64 application.


*****************************************************
Credits / Special Thanks:
*****************************************************
-Olganix: For helping me with my original problems with animations/skeletons.

