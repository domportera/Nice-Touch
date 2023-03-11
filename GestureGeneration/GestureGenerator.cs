#define ERROR_CHECK_NICE_TOUCH

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotExtensions;
using NiceTouch.GestureReceiving;

namespace NiceTouch.GestureGeneration
{
    public static class GestureSettings
    {
        public const int LiftTimeMs = 150;
        public const float SwipeSpeedThreshold = 1f;
        public const float TouchAcceptDistanceCm = 7f;
        public const double TapTime = 0.4d;
        public const double LongPressTime = 0.6d;
        
        const float Pi = (float)Math.PI;
        
        // the amount of imperfection of drag directions considered to be dragging in the same direction, thus forming multi-drags
        public const float DragDirectionThreshold = Pi / 8f;

        // the amount of imperfection allowed in what is considered a pinch or twist
        public const float OppositeAngleThreshold = Pi / 10f;
        
        // touches going in this direction should be growing closer/separating by at least their total movement deltas * PinchDirectionPrecision. Or else it's a twist
        public const float PinchDirectionPrecision = 0.8f; 
    }
    
    internal partial class GestureGenerator
    {
        public const float Pi = (float)Math.PI;

        readonly Dictionary<Touch, GestureCalculator> _calculatorsByTouch = new Dictionary<Touch, GestureCalculator>();
        readonly HashSet<GestureCalculator> _gestureCalculators = new HashSet<GestureCalculator>();
        readonly List<GestureCalculator> _recycledGestureCalculators = new List<GestureCalculator>();
        readonly HashSet<Touch> _longPresses = new HashSet<Touch>();
        
        const MultiGestureInterpretationType MultiGestureInterpretationMode = MultiGestureInterpretationType.RaiseAll;
        enum MultiGestureInterpretationType {IgnoreOddMenOut, IgnoreOddMenOutButSendThemAnyway, RaiseAll}

        public event EventHandler<TouchData> Touch;
        public event EventHandler<Touch> Tap;
        public event EventHandler<Touch> Drag;
        public event EventHandler<Touch> LongPress;
        public event EventHandler<Touch> Swipe;
        public event EventHandler<MultiDragData> MultiDrag;
        public event EventHandler<RawMultiDragData> RawMultiDrag;
        public event EventHandler<RawTwoFingerDragData> RawPinchTwist;
        public event EventHandler<MultiLongPressData> MultiLongPress;
        public event EventHandler<MultiSwipeData> MultiSwipe;
        public event EventHandler<MultiTapData> MultiTap;
        public event EventHandler<PinchData> Pinch;
        public event EventHandler<TwistData> Twist;


        public GestureGenerator(NiceTouch touchProvider, IGestureReceiver receiver)
        {
            if (GestureSettings.TapTime > GestureSettings.LongPressTime)
            {
                throw new ArgumentOutOfRangeException(nameof(GestureSettings.TapTime), GestureSettings.TapTime,
                    $"Value must be less than that of {nameof(GestureSettings.LongPressTime)}");
            }
            
            touchProvider.TouchAdded += OnTouchAdded;
            touchProvider.TouchRemoved += OnTouchRemoved;
            touchProvider.AfterInput += OnInput;

            Touch += receiver.OnSingleTouch;
            Tap += receiver.OnSingleTap;
            Drag += receiver.OnSingleDrag;
            LongPress += receiver.OnSingleLongPress;
            Swipe += receiver.OnSingleSwipe;
            MultiDrag += receiver.OnMultiDrag;
            RawMultiDrag += receiver.OnRawMultiDrag;
            RawPinchTwist += receiver.OnRawPinchTwist;
            MultiLongPress += receiver.OnMultiLongPress;
            MultiSwipe += receiver.OnMultiSwipe;
            MultiTap += receiver.OnMultiTap;
            Pinch += receiver.OnPinch;
            Twist += receiver.OnTwist;
        }

        void OnInput(object sender, EventArgs e)
        {
            foreach (GestureCalculator g in _gestureCalculators)
            {
                int dragCount = g.DraggingTouches.Count;
                if (dragCount== 0) continue;
                if (dragCount == 1)
                {
                    Touch touch = g.DraggingTouches.First();
                    Drag.Invoke(this, touch);
                    continue;
                }

                var draggingTouches = g.DraggingTouches.ToList();
                RawMultiDrag.Invoke(this, new RawMultiDragData(draggingTouches));
                // copy list since DraggingTouches is a property with a backing non-readonly list
                InterpretMultiDrag(draggingTouches);
            }
        }

        // todo: what if something starts dragging and stops? should this distinction be made at all?
        // should stationary touches be considered for pinches/twists?
        void InterpretMultiDrag(IReadOnlyList<Touch> draggingTouches)
        {
            List<Touch> touchesToConsiderForGestures = draggingTouches.ToList();
            HashSet<Touch> multiDragTouches = new HashSet<Touch>();
            for (int i = 0; i < touchesToConsiderForGestures.Count; i++)
            {
                Touch primaryTouch = touchesToConsiderForGestures[i];
                
                for (int j = i + 1; j < touchesToConsiderForGestures.Count; j++)
                {
                    Touch secondaryTouch = touchesToConsiderForGestures[j];
                    RawTwoFingerDragData data = InterpretDragRelationship(primaryTouch, secondaryTouch);

                    bool moveToNextPrimaryTouch = false;
                    switch (data.Relationship)
                    {
                        case DragRelationshipType.None:
                            break;
                        case DragRelationshipType.Identical:
                            multiDragTouches.Add(primaryTouch);
                            multiDragTouches.Add(secondaryTouch);
                            touchesToConsiderForGestures.Remove(secondaryTouch);
                            j--; // decrement iterator to account for removed touch
                            break;
                        case DragRelationshipType.Pinch:
                            touchesToConsiderForGestures.Remove(secondaryTouch);
                            moveToNextPrimaryTouch = true;
                            RawPinchTwist.Invoke(this, data);
                            Pinch.Invoke(this, new PinchData(ref data));
                            break;
                        case DragRelationshipType.Twist:
                            touchesToConsiderForGestures.Remove(secondaryTouch);
                            moveToNextPrimaryTouch = true;
                            RawPinchTwist.Invoke(this, data);
                            Twist.Invoke(this, new TwistData(ref data));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (moveToNextPrimaryTouch) break;
                }


                if (multiDragTouches.Count == 0) continue;
#if ERROR_CHECK_NICE_TOUCH
                if (multiDragTouches.Count == 1)
                {
                    GDLogger.Error(this, $"We got multi-drags with a count of 1 - this should be impossible");
                    continue;
                }
#endif
                Touch[] copiedDragList = multiDragTouches.ToArray();
                MultiDrag.Invoke(this, new MultiDragData(copiedDragList));
                multiDragTouches.Clear();
            }

            // todo: raise SingleDrag events for these unused touches
            foreach (Touch touch in touchesToConsiderForGestures)
            {
                Drag.Invoke(this, touch);
            }
        }

        static RawTwoFingerDragData InterpretDragRelationship(Touch firstTouch, Touch secondTouch)
        {
            Vector2 firstPosition = firstTouch.Position;
            Vector2 secondPosition = secondTouch.Position;
            Vector2 firstPositionPrevious = firstTouch.PreviousPosition;
            Vector2 secondPositionPrevious = secondTouch.PreviousPosition;
            
            Vector2 previousCenter = (secondPositionPrevious + firstPositionPrevious) / 2f;
            Vector2 center = (firstPosition + secondPosition) / 2f;
            Vector2 centerDelta = center - previousCenter;

            Vector2 firstPositionRelativeToCenter = firstPosition - center;
            Vector2 firstPositionPreviousRelativeToCenter = firstPositionPrevious - previousCenter;
            
            float twistRadians = DetermineTwistAngle();

            float separationAmount = firstPosition.DistanceTo(secondPosition)
                                     - firstPositionPrevious.DistanceTo(secondPositionPrevious);
            
            DragRelationshipType relationship = DetermineDragRelationshipType();


            return new RawTwoFingerDragData(firstTouch, secondTouch, separationAmount,
                                            twistRadians, center, centerDelta, relationship);

            DragRelationshipType DetermineDragRelationshipType()
            {
                float difference = Mathf.Abs(firstTouch.DirectionRadians - secondTouch.DirectionRadians);
                bool identical = EqualDirection(difference, tolerance: GestureSettings.DragDirectionThreshold);
                bool opposite = OppositeDirection(difference, tolerance: GestureSettings.OppositeAngleThreshold);
                
                var dragRelationship = DragRelationshipType.None;
                if (identical)
                {
                    dragRelationship = DragRelationshipType.Identical;
                }
                else if (opposite)
                {
                    bool isPinch = IsPinch(firstTouch, secondTouch);
                    dragRelationship = isPinch ? DragRelationshipType.Pinch : DragRelationshipType.Twist;
                }

                return dragRelationship;
            }

            float DetermineTwistAngle()
            {
                // since we're using the center point, we only need to calculate the twist amount
                // using one of the fingers, as their relative twist delta would be the same
                float angle = Mathf.Atan2(firstPositionRelativeToCenter.y,
                    firstPositionRelativeToCenter.x);
                float previousAngle = Mathf.Atan2(firstPositionPreviousRelativeToCenter.y,
                    firstPositionPreviousRelativeToCenter.x);

                float twistAngle = angle - previousAngle;
                return twistAngle;
            }
            
            bool EqualDirection(float angleDifference, float tolerance)
            {
                return angleDifference < tolerance;
            }

            bool OppositeDirection(float angleDifference, float tolerance)
            {
                float toleranceHalved = tolerance / 2f;
                return angleDifference < Pi + toleranceHalved 
                       && angleDifference > Pi - toleranceHalved;
            }
            
            bool IsPinch(Touch touch1, Touch touch2)
            {
                // if they're moving apart or closer to eachother at nearly the same amount as the total of their speeds,
                // then we can assume it is a pinching action.
                bool isPinch = Mathf.Abs(separationAmount) > (touch1.Speed + touch2.Speed) * GestureSettings.PinchDirectionPrecision;
                return isPinch;
            }
        }

        void OnTouchRemoved(object sender, Touch touch)
        {
            bool alreadyRemovedFromGesture = _longPresses.Remove(touch);
            var gesture = _calculatorsByTouch[touch];
            _calculatorsByTouch.Remove(touch);

            if (alreadyRemovedFromGesture)
                return;

            gesture.RemoveTouch(touch, true);
            var touchData = new TouchData(touch, false);
            Touch.Invoke(this, touchData);
        }

        void OnTouchAdded(object sender, Touch touch)
        {
            var touchData = new TouchData(touch, true);
            Touch.Invoke(this, touchData);
            GestureCalculator gesture = GetGesture(touch);

            if (gesture is null)
            {
                GestureCalculator nextCalculator;
                int calculatorsAvailable = _recycledGestureCalculators.Count;
                if (calculatorsAvailable > 0)
                {
                    nextCalculator = _recycledGestureCalculators.Last();
                    _recycledGestureCalculators.RemoveAt(calculatorsAvailable - 1);
                }
                else
                {
                    nextCalculator = CreateNewGestureCalculator(touch);
                }
                
                _gestureCalculators.Add(nextCalculator);
            }
            else
            {
                gesture.AddTouch(touch);
            }
            
            _calculatorsByTouch.Add(touch, gesture);
        }

        GestureCalculator CreateNewGestureCalculator(Touch touch)
        {
            var newGesture = new GestureCalculator(touch);
            newGesture.LongPress += OnLongPress;
            newGesture.GestureEnded += OnGestureEnded;
            newGesture.SingleTap += OnSingleTap;
            newGesture.SingleSwipe += OnSingleSwipe;
            newGesture.SingleDrag += OnSingleDrag;
            newGesture.MultiTap += OnMultiTap;
            newGesture.MultiSwipe += OnMultiSwipe;
            newGesture.MultiLongPress += OnMultiLongPress;
            newGesture.MultiDrag += OnMultiDrag;
            return newGesture;
        }

        void OnMultiLongPress(object sender, IReadOnlyList<Touch> e)
        {
            var data = new MultiLongPressData(e);
            MultiLongPress.Invoke(this, data);
        }

        void OnMultiDrag(object sender, IReadOnlyCollection<Touch> e)
        {
            // leaves this for the gesture interpretation above
            return;
        }

        void OnMultiSwipe(object sender, IReadOnlyList<Touch> e)
        {
            var data = new MultiSwipeData(e);
            MultiSwipe.Invoke(this, data);
        }

        void OnMultiTap(object sender, IReadOnlyList<Touch> e)
        {
            var data = new MultiTapData(e);
            MultiTap.Invoke(this, data);
        }

        void OnSingleDrag(object sender, Touch e)
        {
            Drag.Invoke(this, e);
        }

        void OnSingleSwipe(object sender, Touch e)
        {
            Swipe.Invoke(this, e);
        }

        void OnSingleTap(object sender, Touch e)
        {
            Tap.Invoke(this, e);
        }

        void OnLongPress(object sender, Touch e)
        {
            _longPresses.Add(e);
            LongPress.Invoke(this, e);
        }

        void OnGestureEnded(object sender, EventArgs e)
        {
            GestureCalculator calculator = (GestureCalculator)sender;
            _gestureCalculators.Remove(calculator);
            _recycledGestureCalculators.Add(calculator);
        }

        GestureCalculator GetGesture(Touch touch)
        {
            Vector2 positionCm = touch.PositionCm;

            GestureCalculator nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (GestureCalculator gesture in _gestureCalculators)
            {
                bool accepted = gesture.AssessTouchPosition(positionCm, out float distanceCm);
                if (!accepted) continue;
                if (distanceCm >= nearestDistance) continue;
                
                nearest = gesture;
                nearestDistance = distanceCm;
            }

            return nearest;
        }
    }
    
    public enum DragRelationshipType {None, Identical, Pinch, Twist}
}