#define ERROR_CHECK_NICE_TOUCH
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using GodotExtensions;
using NiceTouch.GestureGeneration;

namespace NiceTouch
{
    /// <summary>
    /// A class that manages an infinite number of touches
    /// </summary>
    internal class NiceTouch : Control
    {
        public event EventHandler<Touch> TouchAdded;
        public event EventHandler<Touch> TouchRemoved;
        public event EventHandler AfterInput;

        readonly Dictionary<int, Touch> _touches = new Dictionary<int, Touch>();

        readonly bool _allowMouse = true;
        readonly bool _acceptTouch = true;
        readonly bool _acceptMouse = true;

        static double Time => Godot.Time.GetTicksUsec() / (double)1000000;

        static int _incrementingTouchIndex = 0;
        readonly Dictionary<int, int> _touchIndices = new Dictionary<int, int>();
        readonly HashSet<int> _mouseButtonsPressed = new HashSet<int>();

        const int MouseButtonMask = (int)ButtonList.MaskLeft | (int)ButtonList.MaskRight | (int)ButtonList.MaskMiddle |
                                     (int)ButtonList.MaskXbutton1 | (int)ButtonList.MaskXbutton2;

        bool _lastRemovedWasTouch; // hacky bool needed to reign in a godot bug (or feature?)

        public override void _Ready()
        {
            _ = new GestureGenerator(this, new NiceTouchForwarder());
        }

        public override void _Input(InputEvent input)
        {
            switch (input)
            {
                case InputEventMouseButton mouse:
                    HandleMouseButtonEvent(mouse);
                    break;
                case InputEventMouseMotion mouse:
                    HandleMouseMotionEvent(mouse);
                    break;
                case InputEventScreenTouch touch:
                    HandleTouchEvent(touch);
                    break;
                case InputEventScreenDrag drag:
                    HandleTouchDragEvent(drag);
                    break;
            }
            
            AfterInput.Invoke(this, EventArgs.Empty);
        }

        public override void _Process(float delta)
        {
            double time = Time;
            foreach (KeyValuePair<int, Touch> touchEntry in _touches)
            {
                Touch touch = touchEntry.Value;
                if (time - touch.LastUpdateTime > delta)
                {
                    touch.Update(time, touch.Position);
                }
            }
        }

        void HandleTouchDragEvent(InputEventScreenDrag drag)
        {
            if (_acceptTouch)
                AcceptEvent();

            DragTouch(drag.Index, Time, drag.Position);
        }

        void HandleTouchEvent(InputEventScreenTouch touch)
        {
            if (_acceptTouch)
                AcceptEvent();

            if (touch.Pressed)
            {
                AddTouch(touch.Index, Time, touch.Position);
                return;
            }

            RemoveTouch(touch.Index, Time, touch.Position);
            _lastRemovedWasTouch = true;
        }

        void HandleMouseMotionEvent(InputEventMouseMotion mouse)
        {
            if (!_allowMouse) return;
            if(_acceptMouse) AcceptEvent();
            double time = Time;
            foreach (int mouseButton in _mouseButtonsPressed)
            {
                int index = MouseToTouchIndex(mouseButton);
                DragTouch(index, time, mouse.Position);
            }
        }

        void HandleMouseButtonEvent(InputEventMouseButton mouse)
        {
            if ((MouseButtonMask & mouse.ButtonIndex) == 0)
                return;
            
            if (!_allowMouse) return;
            
            if(_acceptMouse) AcceptEvent();

            // workaround for godot sending a redundant double-click event on double-taps
            if (mouse.Doubleclick && _lastRemovedWasTouch)
                return;

            int buttonIndex = MouseToTouchIndex(mouse.ButtonIndex);
            if (mouse.Pressed)
            {
                _mouseButtonsPressed.Add(mouse.ButtonIndex);
                AddTouch(buttonIndex, Time, mouse.Position);
            }
            else
            {
                _mouseButtonsPressed.Remove(mouse.ButtonIndex);
                RemoveTouch(buttonIndex, Time, mouse.Position);
                _lastRemovedWasTouch = false;
            }
        }

        static int MouseToTouchIndex(int mouseButton)
        {
            return (mouseButton * -1) - 1;
        }

        static bool IsMouseButton(int index) => index < 0;

        void AddTouch(int index, double time, Vector2 position)
        {
            int touchIndex = _incrementingTouchIndex++;
            _touchIndices[index] = touchIndex;
            Touch touch = new Touch(time, touchIndex, position);
            _touches[touchIndex] = touch;
            TouchAdded.Invoke(this, touch);
        }

        async void RemoveTouch(int index, double time, Vector2 position)
        {
            int touchIndex = _touchIndices[index];
            _touchIndices.Remove(index);
            
            Touch removedTouch = _touches[touchIndex];
            
            // if a touch is removed before any receiving nodes receive it through the main input loop, they won't
            // get the touch released event. this ensures that there's at least one frame granted.
            // this may cause problems in the efficient rendering mode.... ¯\_(ツ)_/¯ 
             while (Engine.GetIdleFrames() == removedTouch.FrameCreated)
             {
                 await Task.Delay(10);
                 // for some reason, godot on android freezes with Yield
                 //await Task.Yield();
             }
                
            removedTouch.Update(time, position);
            _touches.Remove(touchIndex);
            TouchRemoved.Invoke(this, removedTouch);
        }

        void DragTouch(int index, double time, Vector2 position)
        {
            int touchIndex = _touchIndices[index];
            _touches[touchIndex].Update(time, position);
        }
    }
}