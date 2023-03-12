using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NiceTouch.GestureGeneration
{
    public static class TouchListUtilityFunctions
    {
        public static Vector2 Centroid(this IReadOnlyCollection<Touch> touches)
        {
            Vector2 totalPosition = Vector2.Zero;
            foreach (Touch touch in touches)
                totalPosition += touch.Position;

            return totalPosition / touches.Count;
        }

        public static Vector2 CentroidDelta(this IReadOnlyCollection<Touch> touches)
        {
            return touches.CentroidDelta(out Vector2 _);
        }
        
        public static Vector2 CentroidDelta(this IReadOnlyCollection<Touch> touches, out Vector2 centroid)
        {
            Vector2 totalPosition = Vector2.Zero;
            
            foreach (Touch touch in touches)
                totalPosition += touch.Position;
            
            Vector2 totalPositionPrevious = totalPosition;
            foreach (Touch touch in touches)
                totalPositionPrevious -= touch.PositionDelta;

            centroid = totalPosition / touches.Count;
            Vector2 centerPrevious = totalPositionPrevious / touches.Count;
            return centroid - centerPrevious;
        }

        public static float AverageDirectionRadians(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.DirectionRadians);
        }

        public static float AverageDirectionDegrees(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.DirectionDegrees);
        }

        public static double AverageSpeed(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.Speed);
        }
        public static double AverageSpeedInches(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.SpeedInches);
        }
        public static double AverageSpeedCm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.SpeedCm);
        }
        public static double AverageSpeedMm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Average(x => x.SpeedMm);
        }
        public static double MaxSpeed(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Max(x => x.Speed);
        }
        public static double MaxSpeedInches(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Max(x => x.SpeedInches);
        }
        public static double MaxSpeedCm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Max(x => x.SpeedCm);
        }
        public static double MaxSpeedMm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Max(x => x.SpeedMm);
        }
        public static double MinSpeed(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Min(x => x.Speed);
        }
        public static double MinSpeedInches(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Min(x => x.SpeedInches);
        }
        public static double MinSpeedCm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Min(x => x.SpeedCm);
        }
        public static double MinSpeedMm(this IReadOnlyCollection<Touch> touches)
        {
            return touches.Min(x => x.SpeedMm);
        }
    }
}