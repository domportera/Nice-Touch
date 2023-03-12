using System.Collections.Generic;
using Godot;
using NiceTouch.GestureGeneration;

namespace NiceTouch
{
    public interface IMultiFingerGesture
    {
        Vector2 Center { get; }
        Vector2 CenterDelta { get; }
        IReadOnlyList<Touch> Touches { get; }
        int TouchCount { get; }
    }

    interface ITwoFingerGesture : IMultiFingerGesture
    {
        RawTwoFingerDragData RawData { get; }
        Touch Touch1 { get; }
        Touch Touch2 { get; }
    }
    
    public struct RawTwoFingerDragData // todo: comprise of other touch type structs?
    {
        public RawTwoFingerDragData(Touch touch1, Touch touch2, float separationAmount,
            float twistRadians, Vector2 centroid, Vector2 centroidDelta,
            DragRelationshipType relationship)
        {
            Touch1 = touch1;
            Touch2 = touch2;
            SeparationAmount = separationAmount;
            Relationship = relationship;
            Centroid = centroid;
            CentroidDelta = centroidDelta;
            TwistRadians = twistRadians;
        }

        public Touch Touch1 { get; }
        public Touch Touch2 { get; }
        public float SeparationAmount { get; }
        public float TwistRadians { get; }
        public float TwistDegrees => Mathf.Rad2Deg(TwistRadians);
        public DragRelationshipType Relationship { get; }

        public Vector2 Centroid { get; }
        public Vector2 CentroidDelta { get; }

    }

    public struct RawMultiDragData : IMultiFingerGesture
    {
        public IReadOnlyList<Touch> Touches { get; }
        public int TouchCount => Touches.Count;
        public Vector2 Center => Touches.Centroid();
        public Vector2 CenterDelta => Touches.CentroidDelta();

        public RawMultiDragData(IReadOnlyList<Touch> touches)
        {
            Touches = touches;
        }
    }

    public struct PinchData : ITwoFingerGesture
    {
        public PinchData(ref RawTwoFingerDragData rawData)
        {
            RawData = rawData;
        }

        public Vector2 Center => RawData.Centroid;
        public Vector2 CenterDelta => RawData.CentroidDelta;

        public RawTwoFingerDragData RawData { get; }
        public float SeparationAmount => RawData.SeparationAmount;
        public Touch Touch1 => RawData.Touch1;
        public Touch Touch2 => RawData.Touch2;
        public IReadOnlyList<Touch> Touches => new[] { Touch1, Touch2 };
        public int TouchCount => 2;
    }

    public struct TwistData : ITwoFingerGesture
    {
        public TwistData(ref RawTwoFingerDragData rawData)
        {
            RawData = rawData;
        }

        public Vector2 Center => RawData.Centroid;
        public Vector2 CenterDelta => RawData.CentroidDelta;
        public RawTwoFingerDragData RawData { get; }
        public Touch Touch1 => RawData.Touch1;
        public Touch Touch2 => RawData.Touch2;
        public IReadOnlyList<Touch> Touches => new[] { Touch1, Touch2 };
        public int TouchCount => 2;
        public float TwistRadians => RawData.TwistRadians;
        public float TwistDegrees => RawData.TwistDegrees;
    }

    public struct MultiDragData : IMultiFingerGesture // todo: make out of multiple SingleDragData ?
    {
        public IReadOnlyList<Touch> Touches { get; }
        public int TouchCount => Touches.Count;
        public float DirectionRadians => Touches.AverageDirectionRadians();
        public float DirectionDegrees => Touches.AverageDirectionDegrees();
        public Vector2 Center => Touches.Centroid();
        public Vector2 CenterDelta => Touches.CentroidDelta();

        public MultiDragData(IReadOnlyList<Touch> touches)
        {
            Touches = touches;
        }
    }

    public struct MultiLongPressData : IMultiFingerGesture
    {
        public MultiLongPressData(IReadOnlyList<Touch> touches)
        {
            Touches = touches;
        }

        public IReadOnlyList<Touch> Touches { get; }
        public int TouchCount => Touches.Count;
        public Vector2 Center => Touches.Centroid();
        public Vector2 CenterDelta => Touches.CentroidDelta();
    }

    public struct MultiSwipeData : IMultiFingerGesture
    {
        public MultiSwipeData(IReadOnlyList<Touch> touches)
        {
            Touches = touches;
            CenterDelta = touches.CentroidDelta(out var centroid);
            Center = centroid;
        }

        public Vector2 Center { get; }
        public Vector2 CenterDelta { get; }
        public IReadOnlyList<Touch> Touches { get; }
        public int TouchCount => Touches.Count;
        public double AverageSpeed => Touches.AverageSpeed();
        public double AverageSpeedInches => Touches.AverageSpeedInches();
        public double AverageSpeedCm => Touches.AverageSpeedCm();
        public double AverageSpeedMm => Touches.AverageSpeedMm();
        public double MaxSpeed => Touches.MaxSpeed();
        public double MaxSpeedInches => Touches.MaxSpeedInches();
        public double MaxSpeedCm => Touches.MaxSpeedCm();
        public double MaxSpeedMm => Touches.MaxSpeedMm();
        
    }
    
    public struct MultiTapData : IMultiFingerGesture // todo: make out of multi SingleTapData ?
    {
        public MultiTapData(IReadOnlyList<Touch> touches)
        {
            Touches = touches;
        }

        public Vector2 CenterDelta => Touches.CentroidDelta();
        public IReadOnlyList<Touch> Touches { get; }
        public int TouchCount => Touches.Count;
        public Vector2 Center => Touches.Centroid();
    }

    public struct TouchData
    {
        public Touch Touch { get; }
        public bool Pressed { get; }
        public TouchData(Touch touch, bool pressed)
        {
            Touch = touch;
            Pressed = pressed;
        }
    }

    #region Unused - unnecessary for now
    public struct SingleTapData
    {
        public SingleTapData(Touch touch)
        {
            Touch = touch;
            Position = touch.Position;
        }

        public Touch Touch { get; }
        public Vector2 Position { get; }
        public double Duration => Touch.TimeAlive;
    }

    public struct LongPressData
    {
        public LongPressData(Touch touch)
        {
            Touch = touch;
            Position = touch.Position;
        }
        
        public Touch Touch { get; }
        public Vector2 Position { get; }
    }

    public struct SingleDragData
    {
        public SingleDragData(Touch touch)
        {
            Touch = touch;
            Position = touch.Position;
            PositionDelta = touch.PositionDelta;
        }

        public Touch Touch { get; }
        public Vector2 Position { get; }
        public Vector2 PositionDelta { get; }
        public double Speed => Touch.Speed;
        public double SpeedInches => Touch.SpeedInches;
        public double SpeedCm => Touch.SpeedCm;
        public double SpeedMm => Touch.SpeedMm;
    }
    #endregion
}