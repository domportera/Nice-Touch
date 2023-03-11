#define ERROR_CHECK_NICE_TOUCH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GodotExtensions;
using Vector2 = Godot.Vector2;

namespace NiceTouch.GestureGeneration
{
    // todo: multi-finger gestures are not split/optimized by their start time.
    // we can get snappier single-finger gestures and more predictable multi-finger gestures
    // if we determine a touch's start time brethren
    internal partial class GestureGenerator
    {
        class GestureCalculator//Analyzer? nah
        {
            public GestureCalculator(Touch firstTouch)
            {
                AddTouch(firstTouch);
            }

            public void AddTouch(Touch touch)
            {
                _touches.Add(touch);
                touch.Updated += OnTouchUpdated;
            }
            
            public void RemoveTouch(Touch touch, bool considerForGesture)
            {
                touch.Updated -= OnTouchUpdated;
                bool success = _touches.Remove(touch);
                _dragging.Remove(touch);
                
#if ERROR_CHECK_NICE_TOUCH
                if (!success)
                {
                    GDLogger.Error(this, $"Tried to remove a touch this {nameof(GestureCalculator)} didn't have! {touch}");
                    return;
                }
#endif
                int touchCount = _touches.Count;
                if (considerForGesture)
                {
                    ConsiderGesture(touch, touchCount);
                }

                if (touchCount == 0)
                {
                    GestureEnded.Invoke(this, EventArgs.Empty);
                }
            }

            public bool AssessTouchPosition(Vector2 positionCm, out float distanceCm)
            {
                float nearestDistance = GestureSettings.TouchAcceptDistanceCm;
                bool accepted = false;
                
                foreach (Touch touch in _touches)
                {
                    float distance = touch.PositionCm.DistanceTo(positionCm);
                    if (distance > nearestDistance) continue;

                    nearestDistance = distance;
                    accepted = true;
                }

                distanceCm = nearestDistance;
                return accepted;
            }

            void OnTouchUpdated(object sender, EventArgs e)
            {
                Touch touch = (Touch)sender;
                
                if (HandleLongPresses(touch)) return;

                HandleDragging(touch);
            }

            bool _processingMultiLongPress = false;
            bool HandleLongPresses(Touch touch)
            {
                bool isLongPress = touch.TimeAlive >= GestureSettings.LongPressTime && touch.CanBeATap;
                if (!isLongPress) return false;

                _multiLongPressCandidates.Add(touch);
                RemoveTouch(touch, false);
                if (_processingMultiLongPress)
                {
                    return true;
                }
                
                BeginMultiLongPress();
                return true;

            }

            async void BeginMultiLongPress()
            {
                _processingMultiLongPress = true;
                await Task.Delay(GestureSettings.LiftTimeMs);

                if (_multiLongPressCandidates.Count == 1)
                {
                    LongPress.Invoke(this, _multiLongPressCandidates[0]);
                    _multiLongPressCandidates.Clear();
                    return;
                }

                MultiLongPress.Invoke(this, _multiLongPressCandidates);
                _processingMultiLongPress = false;
            }

            void HandleDragging(Touch touch)
            {
                if (!touch.IsDragging)
                {
                    _dragging.Remove(touch);
                    return;
                }
                
                _dragging.Add(touch);
                if (_dragging.Count == 1)
                {
                    SingleDrag.Invoke(this, touch);
                }
                else
                {
                    MultiDrag.Invoke(this, _dragging);
                }
            }

            void ConsiderGesture(Touch touch, int touchCount)
            {
                OneTimeGestureType touchGesture = GetTouchGestureType(touch);
                var singleGesture = new SingleTouchGesture(touchGesture, touch);

                if (touchCount != 0)
                {
                    _multiGestureCandidates.Add(singleGesture);
                    
                    if(!_pendingGesture)
                        BeginMultiGesture();
                    
                    return;
                }

                if (_pendingGesture)
                {
                    _multiGestureCandidates.Add(singleGesture);
                    EndMultiGesture();
                    return;
                }
                
                RaiseSingleGestureEvent(singleGesture);
            }

            async void BeginMultiGesture()
            {
                _pendingGesture = true;
                await Task.Delay(GestureSettings.LiftTimeMs);
                
                if(_pendingGesture)
                    EndMultiGesture();
            }

            void EndMultiGesture()
            {
                _pendingGesture = false;
                #if ERROR_CHECK_NICE_TOUCH
                if (_multiGestureCandidates.Count == 0)
                {
                    GDLogger.Error(this, $"Ended a multi gesture with no touches! wtf?");
                    return;
                }
                #endif
                
                if (_multiGestureCandidates.Count == 1)
                {
                    SingleTouchGesture singleGesture = _multiGestureCandidates[0];
                    RaiseSingleGestureEvent(singleGesture);
                    _multiGestureCandidates.Clear();
                    return;
                }

                Dictionary<OneTimeGestureType, List<Touch>> pendingTouches =
                    new Dictionary<OneTimeGestureType, List<Touch>>();

                int maxTouches = 0;
                foreach (SingleTouchGesture candidate in _multiGestureCandidates)
                {
                    if (!pendingTouches.ContainsKey(candidate.Type))
                    {
                        pendingTouches[candidate.Type] = new List<Touch>();
                    }

                    List<Touch> touchesOfThisGestureType = pendingTouches[candidate.Type];
                    touchesOfThisGestureType.Add(candidate.Touch);

                    int count = touchesOfThisGestureType.Count;
                    if (count > maxTouches)
                        maxTouches = count;
                }

                foreach (KeyValuePair<OneTimeGestureType, List<Touch>> gestureTouches in pendingTouches)
                {
                    List<Touch> touchList = gestureTouches.Value;
                    bool oddMinority = touchList.Count < maxTouches;
                    
                    if (oddMinority)
                    {
                        switch (MultiGestureInterpretationMode)
                        {
                            case MultiGestureInterpretationType.IgnoreOddMenOut:
                            case MultiGestureInterpretationType.IgnoreOddMenOutButSendThemAnyway:
                                continue;
                            case MultiGestureInterpretationType.RaiseAll:
                                RaiseMultiGestureEvent(gestureTouches.Key, touchList);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        continue;
                    }

                    if (MultiGestureInterpretationMode == MultiGestureInterpretationType.IgnoreOddMenOutButSendThemAnyway)
                    {
                        List<Touch> listToSend = _multiGestureCandidates.Select(x => x.Touch).ToList();
                        RaiseMultiGestureEvent(gestureTouches.Key, listToSend);
                        continue;
                    }
                    
                    RaiseMultiGestureEvent(gestureTouches.Key, touchList);
                }
                
                _multiGestureCandidates.Clear();
            }

            void RaiseMultiGestureEvent(OneTimeGestureType type, List<Touch> touches)
            {
                if (touches.Count == 1)
                {
                    RaiseSingleGestureEvent(type, touches[0]);
                    return;
                }
                
                switch (type)
                {
                    case OneTimeGestureType.None:
                        break;
                    case OneTimeGestureType.Swipe:
                        MultiSwipe.Invoke(this, touches);
                        break;
                    case OneTimeGestureType.Tap:
                        // note: can potentially split these touches up by their start time for if they're within the
                        // Lift time or something, thus potentially splitting this into multiple single taps or multi + single taps.
                        // I think this is unnecessary and possibly unintuitive though, given how quick a tap usually is.
                        MultiTap.Invoke(this, touches);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void RaiseSingleGestureEvent(SingleTouchGesture singleGesture)
            {
                RaiseSingleGestureEvent(singleGesture.Type, singleGesture.Touch);
            }

            void RaiseSingleGestureEvent(OneTimeGestureType type, Touch touch)
            {
                switch (type)
                {
                    case OneTimeGestureType.None:
                        break;
                    case OneTimeGestureType.Swipe:
                        SingleSwipe.Invoke(this, touch);
                        break;
                    case OneTimeGestureType.Tap:
                        SingleTap.Invoke(this, touch);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            OneTimeGestureType GetTouchGestureType(Touch touch)
            { 
                if (touch.SpeedCm > GestureSettings.SwipeSpeedThreshold)
                {
                    //SingleSwipe.Invoke(this, touch);
                    return OneTimeGestureType.Swipe;
                }

                if (touch.CanBeATap && touch.TimeAlive < GestureSettings.TapTime)
                {
                    return OneTimeGestureType.Tap;
                }

                return OneTimeGestureType.None;
            }

            
            public event EventHandler<Touch> SingleTap;
            public event EventHandler<Touch> SingleSwipe;
            public event EventHandler<Touch> LongPress;
            public event EventHandler<Touch> SingleDrag;
            public event EventHandler<IReadOnlyList<Touch>> MultiTap;
            public event EventHandler<IReadOnlyList<Touch>> MultiSwipe;
            public event EventHandler<IReadOnlyList<Touch>> MultiLongPress;
            public event EventHandler<IReadOnlyCollection<Touch>> MultiDrag;
            public event EventHandler GestureEnded;
            public IReadOnlyCollection<Touch> DraggingTouches => _dragging;

            readonly List<Touch> _touches = new List<Touch>();
            readonly List<SingleTouchGesture> _multiGestureCandidates = new List<SingleTouchGesture>();
            readonly List<Touch> _multiLongPressCandidates = new List<Touch>();
            readonly HashSet<Touch> _dragging = new HashSet<Touch>();
            bool _pendingGesture;
            
            enum OneTimeGestureType {None, Swipe, Tap}

            struct SingleTouchGesture
            {
                public Touch Touch { get; }
                public OneTimeGestureType Type { get; }

                public SingleTouchGesture(OneTimeGestureType type, Touch touch)
                {
                    Type = type;
                    Touch = touch;
                }
            }
        }
    }
}