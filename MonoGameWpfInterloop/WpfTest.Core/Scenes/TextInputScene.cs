using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System.Linq;
using System.Text;

namespace WpfTest.Scenes
{
    /// <summary>
    /// Demo scene for text input.
    /// Allows toggling the mouse focus and capture behaviour.
    /// Demonstrates when text input is received inside the game.
    /// Should be used in combination with wpf textboxes besides/ontop of the game so users can see
    /// which behaviour allows textinput.
    /// </summary>
    public class TextInputScene : WpfGame
    {
        private WpfKeyboard _keyboard;
        private KeyboardState _keyboardState;
        private KeyboardState _previousKeyboardState;
        private WpfMouse _mouse;
        private MouseState _mouseState;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        /// <summary>
        /// Contains the message entered by the user while this panel had focus.
        /// User can use enter to clear this message.
        /// </summary>
        public string EnteredMessage { get; private set; }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(_mouseState.LeftButton == ButtonState.Pressed ? Color.Black : Color.CornflowerBlue);

            // since we share the GraphicsDevice with all hosts, we need to save and reset the states
            // this has to be done because spriteBatch internally sets states and doesn't reset themselves, fucking over any 3D rendering (which happens in the DemoScene)

            var blend = GraphicsDevice.BlendState;
            var depth = GraphicsDevice.DepthStencilState;
            var raster = GraphicsDevice.RasterizerState;
            var sampler = GraphicsDevice.SamplerStates[0];

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, $"Has focus: {IsFocused}", new Vector2(5, 50), Color.White);
            _spriteBatch.DrawString(_font, $"Focus on: {(FocusOnMouseOver ? "mouse hover" : "first click")}. (F1 in game to toggle this)", new Vector2(5, 50 + 20), Color.White);
            _spriteBatch.DrawString(_font, $"Mouse position: X: {_mouseState.X}, Y: {_mouseState.Y}", new Vector2(5, 50 + 40), Color.White);
            _spriteBatch.DrawString(_font, $"Mouse capture behaviour:: {(_mouse.CaptureMouseWithin ? "capture" : "no capture")}. (F2 in game to toggle this)", new Vector2(5, 50 + 60), Color.White);
            _spriteBatch.DrawString(_font, "If mouse is captured, a pressed mouse that is dragged outside the window will still register the mouse up event.", new Vector2(5, 50 + 80), Color.White);
            _spriteBatch.DrawString(_font, "Downside is that no overlayed controls are possible.", new Vector2(5, 50 + 100), Color.White);
            _spriteBatch.DrawString(_font, "If mouse is not captured, these events won't register (game will think mouse is down until the cursor enters the game again).", new Vector2(5, 50 + 120), Color.White);
            _spriteBatch.DrawString(_font, "Upside is that overlayed controls are possible.", new Vector2(5, 50 + 140), Color.White);
            _spriteBatch.End();

            // this base.Draw call will draw "all" components (we only added one)
            // since said component will use a spritebatch to render we need to let it draw before we reset the GraphicsDevice
            // otherwise it will just alter the state again and fuck over all the other hosts
            base.Draw(time);

            GraphicsDevice.BlendState = blend;
            GraphicsDevice.DepthStencilState = depth;
            GraphicsDevice.RasterizerState = raster;
            GraphicsDevice.SamplerStates[0] = sampler;

        }

        protected override void Initialize()
        {
            new WpfGraphicsDeviceService(this);
            base.Initialize();

            _keyboard = new WpfKeyboard(this);
            _mouse = new WpfMouse(this);

            // default font is pre-compiled font for Windows (Arial 12, ? as default char)
            // I get away with this because
            // 1) it's just a demo application
            // 2) it can only run on windows/directX anyway (interop for WPF afterall)
            // 3) This means it doesn't require content compiler to be installed on any machine that runs this demo

            _font = Content.Load<SpriteFont>("DefaultFont");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime time)
        {
            _mouseState = _mouse.GetState();
            _keyboardState = _keyboard.GetState();

            if (KeyPressed(Keys.F1))
            {
                FocusOnMouseOver = !FocusOnMouseOver;
            }
            if (KeyPressed(Keys.F2))
            {
                _mouse.CaptureMouseWithin = !_mouse.CaptureMouseWithin;
            }
            if (KeyPressed(Keys.Delete))
            {
                // clear message
                EnteredMessage = "";
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = (int)Keys.A; i <= (int)Keys.Z; i++)
                {
                    if (KeyPressed((Keys)i))
                    {
                        // key pressed, add key to queue
                        sb.Append(((char)i));

                    }
                }
                if (KeyPressed(Keys.Space))
                {
                    sb.Append(' ');
                }
                // concat to message
                EnteredMessage += sb.ToString().ToLower();
            }
            _previousKeyboardState = _keyboardState;
            base.Update(time);
        }

        private bool KeyPressed(Keys k)
        {
            return _previousKeyboardState.GetPressedKeys().Contains(k) && !_keyboardState.GetPressedKeys().Contains(k);
        }
    }
}
