using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;

namespace XenoKit.Engine
{
    public enum MouseButtons { Left, Right, Middle, X1, X2 };

    public static class Input
    {
        public static MouseState MouseState { get; set; }
        public static MouseState PreviousMouseState { get; set; }
        public static KeyboardState KeyboardState { get; set; }
        public static Vector2 MousePosition {  get { return MouseState.Position.ToVector2(); } }

        //Scrolling
        /// <summary>
        /// Current MouseWheelValue, as of the previous frame. 
        /// </summary>
        private static int CurrentMouseWheelValue = 0;
        public static int MouseScrollThisFrame = 0;

        //Set externally whenever a mouse button is held down, allowing for exclusive control of that press. Mainly needed so that the camera wont recieve control when dealing with other mouse click events.
        public static bool IsLeftClickHeldDown = false;
        public static bool IsRightClickHeldDown = false;

        //Events
        public static event EventHandler LeftDoubleClick;

        //Left Click
        private static bool _wasLeftMouseReleased = false;
        private static int _currentLeftDoubleClickPeriod = 0;
        private static Vector2 _mouseLocationAtDoubleClickStart;

        //Const
        private const int DoubleClickPeriod = 60;

        public static void Update(WpfMouse mouse, WpfKeyboard keyboard)
        {
            PreviousMouseState = MouseState;
            MouseState = mouse.GetState();
            KeyboardState = keyboard.GetState();

            //Update scroll
            MouseScrollThisFrame = MouseState.ScrollWheelValue - CurrentMouseWheelValue;
            CurrentMouseWheelValue = MouseState.ScrollWheelValue;

            //Events
            HandleLeftMouseDoubleClick();

        }

        private static void HandleLeftMouseDoubleClick()
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
                Log.Add("Double clicked", LogType.Info);
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

        public static bool WasButtonHeld(MouseButtons button)
        {
            return (GetButtonState(button, MouseState) == ButtonState.Pressed
                    && GetButtonState(button, PreviousMouseState) == ButtonState.Pressed);
        }

        public static ButtonState GetButtonState(MouseButtons button, MouseState state)
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
        public static bool IsKeyDown(Keys key)
        {
            return KeyboardState.IsKeyDown(key);
        }

        public static bool IsKeyUp(Keys key)
        {
            return KeyboardState.IsKeyUp(key);
        }

        #endregion
    }

}
