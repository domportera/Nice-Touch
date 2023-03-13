#define ERROR_CHECK_NICE_TOUCH
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            // todo : can we reference its gesture calculator to see if it's in consideration instead of just waiting
            // for everything?
            await Task.Delay(GestureSettings.LiftTimeMs * 3);
            
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
        void FilterTouches<T>(ref T gesture, out List<IGestureInterpreter> fullReceivers, out List<InterpreterWithGestureTouches> partialReceivers) where T : IMultiFingerGesture
        {
            partialReceivers = GetInterpretersContainingTouchesInGesture(ref gesture);
            fullReceivers = FilterUsersWithTouches(ref partialReceivers, gesture.TouchCount);
        }

        /// <summary>
        /// Returns a list of <see cref="InterpreterWithGestureTouches"/> - the <see cref="IGestureInterpreter"/>s
        /// that own a touch contained in the provided gesture
        /// </summary> // todo: `in` keyword in c# 7
        List<InterpreterWithGestureTouches> GetInterpretersContainingTouchesInGesture<T>(ref T gesture) where T : IMultiFingerGesture
        {
            _interpreterWithGestureTouchesPool.RefreshPool();
            
            foreach (KeyValuePair<IGestureInterpreter, HashSet<Touch>> interpreterTouchKvp in _touchesClaimedByInterpreters)
            {
                HashSet<Touch> touches = interpreterTouchKvp.Value;
                InterpreterWithGestureTouches interpreterWithGestureTouches = 
                    _interpreterWithGestureTouchesPool.New(interpreterTouchKvp.Key);

                foreach (Touch touch in gesture.Touches)
                {
                    if (touches.Contains(touch))
                        interpreterWithGestureTouches.Touches.Add(touch);
                }

                if (interpreterWithGestureTouches.Touches.Count == 0)
                    continue;

                _interpreterWithGestureTouchesPool.UsersWithTouches.Add(interpreterWithGestureTouches);
            }

            return _interpreterWithGestureTouchesPool.UsersWithTouches;
        }
        
        
        /// <summary>
        /// Returns a list of <see cref="IGestureInterpreter"/> that should receive the entire gesture.
        /// </summary>
        /// <param name="distributed">All users that contain a touch in the gesture being considered.
        /// Passing by ref only to communicate the fact that this collection will be modified.</param>
        /// <param name="touchCount">The number of touches in the gesture being considered</param>
        List<IGestureInterpreter> FilterUsersWithTouches(ref List<InterpreterWithGestureTouches> distributed, int touchCount)
        {
            _fullGestureReceivers.Clear();
            for(int i = 0; i < distributed.Count; i++)
            {
                InterpreterWithGestureTouches ut = distributed[i];
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
        
        readonly List<IGestureInterpreter> _fullGestureReceivers = new List<IGestureInterpreter>();
    }
}