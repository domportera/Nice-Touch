using System;

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
        
        public const MultiGestureInterpretationType MultiGestureInterpretationMode = MultiGestureInterpretationType.RaiseAll;
        public enum MultiGestureInterpretationType {IgnoreOddMenOut, IgnoreOddMenOutButSendThemAnyway, RaiseAll}
    }
}