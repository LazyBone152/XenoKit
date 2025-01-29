using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop.Input;
using XenoKit.Editor;

namespace XenoKit.Engine
{
    public enum MouseButtons { Left, Right, Middle, X1, X2 };

    public class Input
    {
        private GameBase game;

        public MouseState MouseState { get; private set; }
        public MouseState PreviousMouseState { get; private set; }
        public KeyboardState KeyboardState { get; private set; }

        private Vector2 _prevMousePos;
        private Vector2 _mousePos;
        public Vector2 MousePosition => _mousePos;

        //public Vector2 MousePosition {  get { return MouseState.Position.ToVector2(); } }
        //public Vector2 AltMousePosition => new Vector2(MouseState.Position.X, MouseState.Position.Y);

        //Scrolling
        /// <summary>
        /// Current MouseWheelValue, as of the previous frame. 
        /// </summary>
        private int CurrentMouseWheelValue = 0;
        public int MouseScrollThisFrame = 0;

        //Set externally whenever a mouse button is held down, allowing for exclusive control of that press. Mainly needed so that the camera wont recieve control when dealing with other mouse click events.
        public object LeftClickHeldDownContext { get; set; }
        public object RightClickHeldDownContext { get; set; }

        //Events
        public event EventHandler LeftDoubleClick;

        //Left Click
        private bool _wasLeftMouseReleased = false;
        private int _currentLeftDoubleClickPeriod = 0;
        private Vector2 _mouseLocationAtDoubleClickStart;

        //Const
        private const int DoubleClickPeriod = 60;

        public Input(GameBase game)
        {
            this.game = game;
        }

        public void Update(WpfMouse mouse, WpfKeyboard keyboard)
        {
            PreviousMouseState = MouseState;
            _prevMousePos = _mousePos;
            MouseState = mouse.GetState();
            KeyboardState = keyboard.GetState();

            //_mousePos = new Vector2((game.GraphicsDevice.Viewport.Width - MouseState.X) * game.SuperSamplingFactor, MouseState.Y * game.SuperSamplingFactor);
            _mousePos = MouseState.Position.ToVector2();

            //Update scroll
            MouseScrollThisFrame = MouseState.ScrollWheelValue - CurrentMouseWheelValue;
            CurrentMouseWheelValue = MouseState.ScrollWheelValue;

            //Events
            HandleLeftMouseDoubleClick();

        }

        private void HandleLeftMouseDoubleClick()
        {
            //If mouse has moved position drastically, then dont raise the event
            if((MousePosition.X < _mouseLocationAtDoubleClickStart.X - 10 || MousePosition.X > _mouseLocationAtDoubleClickStart.X + 10) || 
                (MousePosition.Y < _mouseLocationAtDoubleClickStart.Y - 10 || MousePosition.Y > _mouseLocationAtDoubleClickStart.Y + 10) && _wasLeftMouseReleased)
            {
                _wasLeftMouseReleased = false;
                _currentLeftDoubleClickPeriod = 0;
            }

            //Double clicked
            if (MouseState.LeftButton == ButtonState.Pressed && _currentLeftDoubleClickPeriod > 0 && _wasLeftMouseReleased)
            {
                LeftDoubleClick?.Invoke(MouseState, new EventArgs());
                _wasLeftMouseReleased = false;
            }

            //Mouse is relased after first click
            if(MouseState.LeftButton == ButtonState.Released && _currentLeftDoubleClickPeriod > 0)
            {
                _wasLeftMouseReleased = true;
            }

            //Initial click
            if (MouseState.LeftButton == ButtonState.Pressed && _currentLeftDoubleClickPeriod == 0)
            {
                _mouseLocationAtDoubleClickStart = MousePosition;
                _currentLeftDoubleClickPeriod = DoubleClickPeriod;
            }

            //Reset values if no second click happens
            if (_currentLeftDoubleClickPeriod == 0)
                _wasLeftMouseReleased = false;

            if (_currentLeftDoubleClickPeriod > 0)
                _currentLeftDoubleClickPeriod--;
        }

        #region Mouse

        public bool WasButtonHeld(MouseButtons button)
        {
            return (GetButtonState(button, MouseState) == ButtonState.Pressed
                    && GetButtonState(button, PreviousMouseState) == ButtonState.Pressed);
        }

        public ButtonState GetButtonState(MouseButtons button, MouseState state)
        {
            if (button == MouseButtons.Left)
                return state.LeftButton;
            if (button == MouseButtons.Middle)
                return state.MiddleButton;
            if (button == MouseButtons.Right)
                return state.RightButton;
            if (button == MouseButtons.X1)
                return state.XButton1;
            if (button == MouseButtons.X2)
                return state.XButton2;

            return ButtonState.Released;
        }

        #endregion

        #region Keyboard
        public bool IsKeyDown(Keys key)
        {
            return KeyboardState.IsKeyDown(key);
        }

        public bool IsKeyUp(Keys key)
        {
            return KeyboardState.IsKeyUp(key);
        }

        #endregion
    }

}
