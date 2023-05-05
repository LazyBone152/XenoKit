# MonoGame WPF Interop

This (unoffical) package adds WPF support to MonoGame (MonoGame 3.8, requires **MonoGame.Framework.WindowsDX**).

You can host as many MonoGame controls in Wpf as you want by using the WpfGame control. Note that they are however limited to 60 FPS (this is a WPF limit).

### MonoGame.Framework.WpfInterop on [![NuGet Status](https://img.shields.io/nuget/v/MonoGame.Framework.WpfInterop.svg?style=flat)](https://www.nuget.org/packages/MonoGame.Framework.WpfInterop/)

## Example usage

```csharp

public class MyGame : WpfGame
{
    private IGraphicsDeviceService _graphicsDeviceManager;
    private WpfKeyboard _keyboard;
    private WpfMouse _mouse;

    protected override void Initialize()
    {
        // must be initialized. required by Content loading and rendering (will add itself to the Services)
        // note that MonoGame requires this to be initialized in the constructor, while WpfInterop requires it to
        // be called inside Initialize (before base.Initialize())
        _graphicsDeviceManager = new WpfGraphicsDeviceService(this);

        // wpf and keyboard need reference to the host control in order to receive input
        // this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
        _keyboard = new WpfKeyboard(this);
        _mouse = new WpfMouse(this);
        
        // must be called after the WpfGraphicsDeviceService instance was created
        base.Initialize();

        // content loading now possible
    }

    protected override void Update(GameTime time)
    {
        // every update we can now query the keyboard & mouse for our WpfGame
        var mouseState = _mouse.GetState();
        var keyboardState = _keyboard.GetState();
    }

    protected override void Draw(GameTime time)
    {
    }
}

```

Now you can use it in any of your WPF forms:

```
<MyGame Width="800" Height="480" />
```

# Breaking changes (compared to monogame)

Some of the Monogame classes are incompatible with WPF (Game always spawns its own window, Mouse doesn't care which control has focus, ..) so they had to be reimplemented.

As a convention, all reimplemented classes will have the prefix Wpf:

* WpfGame as a replacement for Game class. Note that due to WPF limitations the WpfGame will always run at a maximum 60 FPS in fixed step (Update and Draw are always called, no Updates are skipped). It is possible to lower the framerate via TargetElapsedTime)
* WpfMouse and WpfKeyboard provide input per host instance. When multiple WpfGame instances are spawned, only one will receive input at any time
* WpfGraphicsDeviceService as an implementation of IGraphicsDeviceService and IGraphicsDeviceManager (required by the content manager)
* WpfGameComponent and WpfDrawableGameComponent as a replacement for the original ones which required a reference to a Game instance

## Mouse behaviour

### Focus

By default the game takes focus on mouse (h)over. This can be disabled via the FocusOnMouseOver property of WpfGame

### Mouse capture

By default the game captures the mouse. This allows capture of mouse events outside the game (e.g. user holds and drags the mouse outside the window, then releases it -> game will still receive mouse up event). The downside is that no overlayed controls (e.g. textboxes on top of the game) will ever receive focus.

Alternatively this can be toggled off via CaptureMouseWithin property of WpfMouse and then allows focus on overlayed controls. The downside is that mouse events outside the game window are no longer registered (e.g. user holds and drags the mouse outside the window, then releases it -> game will still think the mouse is down until the window receives focus again)

# Change in behaviour (compared to monogame)

## RenderTargets

Rendertargets work slightly different in this WPF interop.

In a normal monogame the rendertarget would be used like this:

```

    // Draw into rendertarget
    GraphicsDevice.SetRenderTarget(_rendertarget);
    GraphicsDevice.Clear(Color.Transparent);
    _spriteBatch.Begin();
    _spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
    _spriteBatch.End();

    // setting null means we want to draw to the backbuffer again
    GraphicsDevice.SetRenderTarget(null);


    // these draw calls will now render onto backbuffer
    GraphicsDevice.Clear(Color.CornflowerBlue);
    _spriteBatch.Begin();
    _spriteBatch.Draw(this.rendertarget, Vector2.Zero, Color.White);
    _spriteBatch.End();
```

Instead the WPF interop always uses a rendertarget (internally used to display the renderoutput in WPF), thus in a WPF control the code needs to look like this instead:

```

    // get and cache the wpf rendertarget (there is always a default rendertarget)
    var wpfRenderTarget = (RenderTarget2D)GraphicsDevice.GetRenderTargets()[0].RenderTarget;

    // Draw into custom rendertarget
    GraphicsDevice.SetRenderTarget(_rendertarget);
    GraphicsDevice.Clear(Color.Transparent);
    _spriteBatch.Begin();
    _spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
    _spriteBatch.End();

    // instead of setting null, set it back to the wpf rendertarget
    // this will ensure that the output will end up visible to the user
    GraphicsDevice.SetRenderTarget(wpfRenderTarget);


    // these draw calls will now render onto backbuffer
    GraphicsDevice.Clear(Color.CornflowerBlue);
    _spriteBatch.Begin();
    _spriteBatch.Draw(this.rendertarget, Vector2.Zero, Color.White);
    _spriteBatch.End();
```

**The reason for this behaviour that the interop sample cannot use the backbuffer (null) and instead needs to use its own rendertarget to interop with WPF.**

## TabControls

It is perfectly possible to use the WpfGame controls inside TabControls.

By default, WPF fully unloads any tab that is deactivated (e.g. when switching to another tab) and fully reloads the tab when switching back.

The WpfGame **does not unload** in these cases. See the next few events for details.

### WpfGame.Activated/Deactivated

When a WpfGame is hosted inside a tab and the tab is changed, the WpfGame instance is **not unloaded**. Instead, Deactivated is fired. Likewise when the tab is activated again, Activated is fired.

If the parent window loses focus, Deactivated is fired (but only for the active tab) and when the window receives focus, Activated is called again (only for the active tab).

### Initialize/Dispose

Initialize is only called once (per instance, when the window is created) and Dispose is called for every game instance once the window closes.

This means, that initialize is called even for those instances that are in "disabled" tabs. However IsActive can be used to determine whether the current game is inside the active tab (see below).

### WpfGame.IsActive

Update/Draw are still called for all inactive tabs and any inactive tab has the IsActive property on WpfGame set to false. Only the active tab has IsActive set to true (and only when the window is the currently active window).
