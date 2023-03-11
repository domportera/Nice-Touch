using System.Collections.Generic;
using System.Diagnostics;
using NiceTouch.GestureReceiving;

namespace NiceTouch
{
    public partial class NiceTouchForwarder : IGestureReceiver
    {
        // todo: raw version of each gesture that is unclaimed, fruit ninja style with typical godot bindings
        // these classes are made, just need to create and call them

        readonly Dictionary<Touch, HashSet<IGestureInterpreter>> _claimedTouches =
            new Dictionary<Touch, HashSet<IGestureInterpreter>>();

        readonly Dictionary<IGestureInterpreter, HashSet<Touch>> _touchesClaimedByInterpreters =
            new Dictionary<IGestureInterpreter, HashSet<Touch>>();

        readonly HashSet<Touch> _unclaimedTouches = new HashSet<Touch>();

        readonly Stack<HashSet<IGestureInterpreter>> _recycledInterpreterCollections =
            new Stack<HashSet<IGestureInterpreter>>();

        readonly Stack<HashSet<Touch>> _recycledTouchCollections = new Stack<HashSet<Touch>>();
        
        public void OnSingleTouch(object sender, TouchData touchData)
        {
            Touch touch = touchData.Touch;
            _unclaimedTouches.Add(touch);
            if (touchData.Pressed)
            {
                BeginSingleTouch(touch);
            }
            else
            {
                // todo: delay endsingletouch if it's in use by a multi-gesture?
                EndSingleTouch(touch);
            }
        }
        
        public void OnSingleDrag(object sender, Touch touch)
        {
            HashSet<IGestureInterpreter> touchers = _claimedTouches[touch];
            foreach (var g in touchers)
                g.OnSingleDrag(touch);
        }

        public void OnSingleLongPress(object sender, Touch touch)
        {
            HashSet<IGestureInterpreter> touchers = _claimedTouches[touch];
            foreach (var g in touchers)
                g.OnSingleTap(touch);
        }

        public void OnSingleSwipe(object sender, Touch touch)
        {
            HashSet<IGestureInterpreter> touchers = _claimedTouches[touch];
            foreach (var g in touchers)
                g.OnSingleSwipe(touch);
        }

        public void OnSingleTap(object sender, Touch touch)
        {
            HashSet<IGestureInterpreter> touchers = _claimedTouches[touch];
            foreach (var g in touchers)
                g.OnSingleTap(touch);
        }
        
        // comprised of one drag + one drag/hold
        public void OnTwist(object sender, TwistData gesture)
        {
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);
            
            foreach (IGestureInterpreter g in fullReceivers)
                g.OnTwist(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                Debug.Assert(receiver.JustOneTouch); //twists only have two touches so this must be one
                Touch touch = receiver.Touches[0];
                receiver.Interpreter.OnSingleDrag(touch);
            }
        }

        // comprised of one drag + one drag/hold
        public void OnPinch(object sender, PinchData gesture)
        { 
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);
            
            foreach (IGestureInterpreter g in fullReceivers)
                g.OnPinch(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                Debug.Assert(receiver.JustOneTouch); //pinches only have two touches so this must be one
                Touch touch = receiver.Touches[0];
                receiver.Interpreter.OnSingleDrag(touch);
                
                // todo: pinches and  twists should be allowed to contain a stationary touch
            }
        }
        
        public void OnMultiDrag(object sender, MultiDragData gesture)
        {
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);
            
            foreach (IGestureInterpreter g in fullReceivers)
                g.OnMultiDrag(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                if (receiver.JustOneTouch)
                {
                    receiver.Interpreter.OnSingleDrag(receiver.Touches[0]);
                    continue;
                }

                var multiGesture = new MultiDragData(receiver.Touches);
                receiver.Interpreter.OnMultiDrag(multiGesture);
            }
        }

        public void OnMultiLongPress(object sender, MultiLongPressData gesture)
        {
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);
            
            foreach (IGestureInterpreter g in fullReceivers)
                g.OnMultiLongPress(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                if (receiver.JustOneTouch)
                {
                    receiver.Interpreter.OnSingleLongPress(receiver.Touches[0]);
                    continue;
                }

                var multiGesture = new MultiLongPressData(receiver.Touches);
                receiver.Interpreter.OnMultiLongPress(multiGesture);
            }
        }

        public void OnMultiSwipe(object sender, MultiSwipeData gesture)
        {
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);
            
            foreach (IGestureInterpreter g in fullReceivers)
                g.OnMultiSwipe(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                if (receiver.JustOneTouch)
                {
                    receiver.Interpreter.OnSingleSwipe(receiver.Touches[0]);
                    continue;
                }

                var multiGesture = new MultiSwipeData(receiver.Touches);
                receiver.Interpreter.OnMultiSwipe(multiGesture);
            }
        }

        public void OnMultiTap(object sender, MultiTapData gesture)
        {
            FilterTouches(ref gesture, 
                out List<IGestureInterpreter> fullReceivers, 
                out List<InterpreterWithTouches> partialReceivers);

            foreach (IGestureInterpreter g in fullReceivers)
                g.OnMultiTap(gesture);

            foreach (InterpreterWithTouches receiver in partialReceivers)
            {
                if (receiver.JustOneTouch)
                {
                    receiver.Interpreter.OnSingleTap(receiver.Touches[0]);
                    continue;
                }

                var multiGesture = new MultiTapData(receiver.Touches);
                receiver.Interpreter.OnMultiTap(multiGesture);
            }
        }
        
        public void OnRawMultiDrag(object sender, RawMultiDragData e)
        {
            return;
        }

        public void OnRawPinchTwist(object sender, RawTwoFingerDragData e)
        {
            return;
        }
    }
}