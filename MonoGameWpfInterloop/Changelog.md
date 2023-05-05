# Changelog

**v1.9.2**

* Fixed a bug with unsupported keybindings by [LynxStar](https://github.com/MarcStan/monogame-framework-wpfinterop/pull/29)

**v1.9.1**

* Fixed bug that causes flickering due to sync issue ([issue #27](https://github.com/MarcStan/monogame-framework-wpfinterop/issues/27)) by [georg-eckert-zeiss](https://github.com/MarcStan/monogame-framework-wpfinterop/pull/28)


**v1.9.0**

* Added support for .Net Core 3.1 and .Net 5 by cross targeting .Net 4.5.2 and .Net Core 3.1

**v1.8.0**

* Upgraded to Monogame 3.8

**v1.7.1**

* Fixed crash when resizing a WpfGame that has MSAA turned on

**v1.7.0**

* Upgraded to Monogame 3.7
    * fixes MSAA in DirectX based monogame
* WpfGraphicsDeviceService now properly calls all IGraphicsDeviceService events
* added support for DPI scaling on WpfGraphicsDeviceManager
* by default, each WpfGame now has its own GraphicsDevice instance
    * to enable old behaviour of one global GraphicsDevice instance: call WpfGame.UseASingleSharedGraphicsDevice = true **before the first WpfGame instance is created**)

**v1.6.0**

Switch to Monogame 3.6 and content builder nuget (removes the dependency to install monogame locally).

**v1.5.3**

* Now locking D3D11 base image while cloning rendertarget (fixes hitching issues that occured on some systems)

**v1.5.2**

* Fixed bug where User calling Dispose manually would decrement reference counter for graphicsdevice twice ([issue #14](https://gitlab.com/MarcStan/MonoGame.Framework.WpfInterop/issues/14))

**v1.5.1**

* Fixed bug that causes flickering when draw calls put the system under load ([issue #12](https://gitlab.com/MarcStan/MonoGame.Framework.WpfInterop/issues/12))

**v1.5.0**

* Fixed bug that causes crashes when WpfGame was hosted inside TabControls and tabs where switched
* Added Activated/Deativated events on WpfGame that fire on window focus/focus lost (see section Gotchas\TabControls for details when used inside tabs)
* Added IsActive property on WpfGame that indicates whether the current windows is the active one or not (see also section Gotchas\TabvControls for details when used inside tabs)

**v1.4.0**

* WpfGame now has "FocusOnMouseOver" which allows changing the behaviour (defaults to true)
* WpfMouse now has "CaptureMouseWithin" which allows changing the capture behaviour (defaults to true)
* TargetElapsedTime can now be set by the user to reduce the framerate if desired (defaults to 60 fps)

**v1.3.2**

* Fixed mouse scrollwheel value incorrectly resetting whenever any other mouse event fired (move, button down/up)
* Exceptions thrown in Initialize() are no longer silently swallowed (bug in x64 WPF OnLoaded exceptions handling). Exception is now rethrown manually

**v1.3.1**

* Moved to Gitlab and updated all links to Gitlab

**v1.3.0**

* Added support for GameComponents (WpfGameComponent and WpfDrawableGameComponent) that mirror the behaviour of the original ones (which cannot be used due to requiring a reference to Game)

**v1.2.2**

* fixed mistake in readme

**v1.2.1**

* Bugfix in demoscene

**v1.2.0**

* Added dependency on MonoGame.Framework.WindowsDX nuget package to prevent accidental use of different platform versions

**v1.1.1**

* Added SetCursor function to WpfMouse which now allows resetting the cursor (the monogame Mouse.SetPosition function would throw due to not finding a Winforms window)

**v1.1.0**

* New class "WpfGame" that derives from D3D11Host. It provides a cleaner interface and is more similar to the original Game class
* Input is now available via WpfMouse and WpfKeyboard

**v1.0.0**

* Initial release, can render and load content
* Input is not working yet
