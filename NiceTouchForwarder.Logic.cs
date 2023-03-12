#define ERROR_CHECK_NICE_TOUCH
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using GodotExtensions;
using NiceTouch.GestureGeneration;
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

            bool hasKey = _claimedTouches.TryGetValue(touch, out HashSet<IGestureInterpreter> claimers);

            if (!hasKey)
            {
                #if ERROR_CHECK_NICE_TOUCH
                GDLogger.Log(this, $"Touch was removed before it was accepted. Ignoring");
                #endif
                return;
            }

            bool hasInterpreterTouchCollection =
                _touchesClaimedByInterpreters.TryGetValue(interpreter, out HashSet<Touch> interpreterTouchCollection);

            if (!hasInterpreterTouchCollection)
            {
                interpreterTouchCollection = new HashSet<Touch>();
                _touchesClaimedByInterpreters.Add(interpreter, interpreterTouchCollection);
            }

            interpreterTouchCollection.Add(touch);
            claimers.Add(interpreter);
            args.Interpreter.OnTouchBegin(thisEvent.Touch);
        }

        async void EndSingleTouch(Touch touch)
        {
            HashSet<IGestureInterpreter> claimers = _claimedTouches[touch];
            foreach (IGestureInterpreter interpreter in claimers)
            {
                interpreter.OnTouchEnd(touch);
            }

            // give ample time for the touch to be processed as a gesture before removing
            await Task.Delay(GestureSettings.LiftTimeMs * 10);
            
            foreach (IGestureInterpreter interpreter in claimers)
            {
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
        void FilterTouches<T>(ref T gesture, out List<IGestureInterpreter> fullReceivers, out List<InterpreterWithTouches> partialRecievers) where T : IMultiFingerGesture
        {
            partialRecievers = GetInterpretersContainingTouchesInGesture(ref gesture);
            fullReceivers = FilterUsersWithTouches(ref partialRecievers, gesture.TouchCount);
        }

        /// <summary>
        /// Returns a list of <see cref="InterpreterWithTouches"/> - the <see cref="IGestureInterpreter"/>s that own a touch
        /// contained in the provided gesture
        /// </summary>
        List<InterpreterWithTouches> GetInterpretersContainingTouchesInGesture<T>(ref T gesture) where T : IMultiFingerGesture // todo: `in` keyword in c# 7
        {
            // todo: caching 
            List<InterpreterWithTouches> usersWithTouches = new List<InterpreterWithTouches>(); 
            foreach (KeyValuePair<IGestureInterpreter, HashSet<Touch>> interpreterWithTouches in _touchesClaimedByInterpreters)
            {
                List<Touch> touchesTheyOwn = new List<Touch>();
                IGestureInterpreter interpreter = interpreterWithTouches.Key;
                HashSet<Touch> touches = interpreterWithTouches.Value;

                foreach (Touch touch in gesture.Touches)
                {
                    if (touches.Contains(touch))
                        touchesTheyOwn.Add(touch);
                }

                if (touchesTheyOwn.Count == 0)
                    continue;

                usersWithTouches.Add(new InterpreterWithTouches(interpreter, touchesTheyOwn));
            }

            return usersWithTouches;
        }

        readonly List<IGestureInterpreter> _fullGestureReceivers = new List<IGestureInterpreter>();
        
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