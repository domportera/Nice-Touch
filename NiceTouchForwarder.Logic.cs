using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using NiceTouch.GestureReceiving;

namespace NiceTouch
{
    public partial class NiceTouchForwarder
    {
        void BeginSingleTouch(Touch touch)
        {
            var args = new TouchBegin(touch);
            args.Accepted += OnSingleTouchAccepted;

            bool recycledAvailable = _recycledInterpreterCollections.Count > 0;

            HashSet<IGestureInterpreter> collection =
                recycledAvailable ? _recycledInterpreterCollections.Pop() : new HashSet<IGestureInterpreter>();

            _claimedTouches.Add(touch, collection);

            Input.ParseInputEvent(args); //unfortunately limited to not triggering _GuiInput /:
        }

        void OnSingleTouchAccepted(object sender, TouchBegin.TouchBeginEventArgs args)
        {
            var thisEvent = (TouchBegin)sender;
            Touch touch = thisEvent.Touch;
            _unclaimedTouches.Remove(touch);

            IGestureInterpreter interpreter = args.Interpreter;

            HashSet<IGestureInterpreter> claimers = _claimedTouches[touch];

            bool hasInterpreterTouchCollection =
                _touchesClaimedByInterpreters.TryGetValue(interpreter, out var interpreterTouchCollection);

            if (!hasInterpreterTouchCollection)
            {
                interpreterTouchCollection = new HashSet<Touch>();
                _touchesClaimedByInterpreters.Add(interpreter, interpreterTouchCollection);
            }

            interpreterTouchCollection.Add(touch);
            claimers.Add(interpreter);
            args.Interpreter.OnTouchBegin(thisEvent.Touch);
        }

        void EndSingleTouch(Touch touch)
        {
            HashSet<IGestureInterpreter> claimers = _claimedTouches[touch];
            foreach (IGestureInterpreter interpreter in claimers)
            {
                interpreter.OnTouchEnd(touch);
                _touchesClaimedByInterpreters[interpreter].Remove(touch);
            }

            claimers.Clear();
            _claimedTouches.Remove(touch);
            _recycledInterpreterCollections.Push(claimers);
            _unclaimedTouches.Add(touch);
        }

        /// <summary>
        /// Outputs lists of <see cref="IGestureInterpreter"/>s that can make use of the full gesture and can make use
        /// of part of it
        /// </summary>
        /// <param name="twisters"></param>
        /// <param name="partialRecievers"></param>
        /// <typeparam name="T"></typeparam>
        void FilterTouches<T>(ref T gesture, out List<IGestureInterpreter> twisters, out List<InterpreterWithTouches> partialRecievers) where T : IMultiFingerGesture
        {
            partialRecievers = DistributeMultiTouch(ref gesture);
            twisters = FilterUsersWithTouches(ref partialRecievers, gesture.TouchCount);
        }

        /// <summary>
        /// Returns a list of <see cref="InterpreterWithTouches"/> - the <see cref="IGestureInterpreter"/>s that own a touch
        /// contained in the provided gesture
        /// </summary>
        List<InterpreterWithTouches> DistributeMultiTouch<T>(ref T gesture) where T : IMultiFingerGesture // todo: `in` keyword in c# 7
        {
            List<InterpreterWithTouches> usersWithTouches = new List<InterpreterWithTouches>(); // todo: cache this list
            foreach (KeyValuePair<IGestureInterpreter, HashSet<Touch>> interpreterWithTouches in
                     _touchesClaimedByInterpreters)
            {
                IGestureInterpreter interpreter = interpreterWithTouches.Key;
                HashSet<Touch> touches = interpreterWithTouches.Value;

                List<Touch> touchesTheyOwn = new List<Touch>();

                foreach (Touch touch in gesture.Touches)
                {
                    if (touches.Contains(touch))
                        touchesTheyOwn.Add(touch);
                }

                usersWithTouches.Add(new InterpreterWithTouches(interpreter, touchesTheyOwn));
            }

            return usersWithTouches;
        }

        List<IGestureInterpreter> _fullGestureReceivers = new List<IGestureInterpreter>();
        
        /// <summary>
        /// Returns a list of <see cref="IGestureInterpreter"/> that should receive the entire gesture.
        /// </summary>
        /// <param name="distributed">All users that contain a touch in the gesture being considered.
        /// Passing by ref only to communicate the fact that this collection will be modified.</param>
        /// <param name="touchCount">The number of touches in the gesture being considered</param>
        List<IGestureInterpreter> FilterUsersWithTouches(ref List<InterpreterWithTouches> distributed, int touchCount)
        {
            _fullGestureReceivers.Clear();
            for(int i = 0; i < distributed.Count; i++)
            {
                InterpreterWithTouches ut = distributed[i];
                Debug.Assert(ut.Touches.Count != 0);
                
                if (ut.Touches.Count != touchCount) continue;
                
                // if we have all the touches, remove from distributed (incomplete) list
                // and add to the full gesture receivers list  
                _fullGestureReceivers.Add(ut.Interpreter);
                distributed.RemoveAt(i);
                i--;
            }

            return _fullGestureReceivers;
        }

        static void HandlePartialDragGestures(List<InterpreterWithTouches> partialReceivers)
        {
            foreach (InterpreterWithTouches ut in partialReceivers)
            {
                if (ut.JustOneTouch)
                {
                    var touch = ut.Touches[0];
                    if (touch.IsDragging)
                        // drag
                        ut.Interpreter.OnSingleDrag(touch);
                    //else
                        //do nothing - i havent moved since last time?
                        //ut.Interpreter.OnSingleDrag(touch);

                    continue;
                }

                //interpret new gestures out of these htings... oh boy
            }
        }
        

        struct InterpreterWithTouches //todo: cache these lists?
        {
            public InterpreterWithTouches(IGestureInterpreter interpreter, List<Touch> touches)
            {
                Interpreter = interpreter;
                Touches = touches;
            }

            public IGestureInterpreter Interpreter { get; }
            public List<Touch> Touches { get; }
            public bool JustOneTouch => Touches.Count == 1;
        }
    }
}