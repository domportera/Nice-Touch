#define ERROR_CHECK_NICE_TOUCH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotExtensions;
using NiceTouch.GestureGeneration;
using static NiceTouch.UnitConstants;
using Environment = System.Environment;

namespace NiceTouch
{
    public static class TouchSettings
    {
        public const double MoveSpeedThresholdMm = 0d;
        public const float DragDistanceThresholdMm = 2f;
    }
    
    public partial class Touch // Nice Touch ™
    {
        const double DragHistoryDuration = 0.3;

        internal Touch(double time, int index, Vector2 position)
        {
            _dpi = DisplayServer.ScreenGetDpi();
            Current = new TouchPositionData(time, position, _dpi);
            _history.Enqueue(Current);
            StartPosition = position;
            Index = index;
            StartTime = time;
            FrameCreated = Engine.GetProcessFrames(); //Godot 4.0+ - GetProcessFrames()
        }

        GestureCalculator _gestureCalculator; 
        readonly Queue<TouchPositionData> _history = new Queue<TouchPositionData>();
        readonly float _dpi;

        public event EventHandler Updated;
        public TouchPositionData Current { get; private set; }
        
        public int Index {get; }
        public double LastUpdateTime => Current.Time;
        public double TimeAlive => LastUpdateTime - StartTime;
        public double StartTime { get; }
        public ulong FrameCreated { get; }
        public bool IsDragging => IsMoving && HasDragged;
        public bool HasDragged => TotalDistanceTraveledMm > TouchSettings.DragDistanceThresholdMm;
        bool IsMoving => SpeedMm > TouchSettings.MoveSpeedThresholdMm;
        
        public Vector2 StartPosition { get; }
        public Vector2 StartPositionInches => StartPosition * _dpi;
        public Vector2 StartPositionCm => StartPositionInches * InchesToCmF;
        public Vector2 StartPositionMm => StartPositionCm * CmToMm;
        
        public Vector2 Position => Current.Position;
        
        public Vector2 PositionCm => Current.PositionCm;
        public Vector2 PositionInches => Current.PositionInches;
        public Vector2 PositionMm => Current.PositionMm;
        public Vector2 PositionDelta => Current.PositionDelta;
        
        public Vector2 PreviousPosition => Current.Position - PositionDelta;
        public Vector2 PreviousPositionInches => PreviousPosition * _dpi;
        public Vector2 PreviousPositionCm => PreviousPositionInches * InchesToCmF;
        public Vector2 PreviousPositionMm => PreviousPositionCm * CmToMm;
        
        public double Speed => Current.Speed;
        public double SpeedCm => Current.SpeedCm;
        public double SpeedMm => Current.SpeedMm;
        public double SpeedInches => Current.SpeedInches;
        
        
        public float TotalDistanceTraveled { get; private set; } = 0f;
        public float TotalDistanceTraveledInches => TotalDistanceTraveled * _dpi;
        public float TotalDistanceTraveledCm => TotalDistanceTraveledInches * InchesToCmF;
        public float TotalDistanceTraveledMm => TotalDistanceTraveledCm * CmToMm;

        public Vector2 PositionSmoothedSimple => (Current.Position + _history.Peek().Position) / 2f;
        public Vector2 DeltaSmoothedSimple => (Current.PositionDelta + _history.Peek().PositionDelta) / 2f;
        public double SpeedSmoothedSimple => (Current.Speed + _history.Peek().Speed) / 2;

        
        const float TapWiggleThresholdMm = 5f;
        public bool CanBeATap => TotalDistanceTraveledMm < TapWiggleThresholdMm;
        public float DirectionDegrees => Current.DirectionDegrees;
        public float DirectionRadians => Current.DirectionRadians;


        public override string ToString()
        {
            return $"({nameof(Touch)}): {Current.ToString()}";
        }

        
        internal void Update(double time, Vector2 position)
        {
            var positionData = new TouchPositionData(time, LastUpdateTime, position, position - Current.Position, _dpi);
            
            TotalDistanceTraveled += positionData.DistanceTraveled;
            Current = positionData;
            
            bool hasHistory = _history.Count > 0;
            TouchPositionData oldest = hasHistory ? _history.Peek() : Current;
            _history.Enqueue(positionData);

            double timeElapsed = time - oldest.Time;
            if (/*hasHistory && */timeElapsed > DragHistoryDuration) // add check if changing data structures for a more advanced "smoothing" system
                _history.Dequeue();
            
#if ERROR_CHECK_NICE_TOUCH
            if (Updated == null)
            {
                GDLogger.Error(this, $"Touch {Index.ToString()} does not have any listeners");
                return;
            }
            
            Updated.Invoke(this, EventArgs.Empty);
#else
            Updated.Invoke(this, EventArgs.Empty);
#endif
        }

        internal void Claim(GestureCalculator gestureCalculator)
        {
            #if ERROR_CHECK_NICE_TOUCH
            if (_gestureCalculator != null)
                throw new Exception($"Touch {Index} already claimed");
            #endif
            _gestureCalculator = gestureCalculator;
        }

        internal bool InConsiderationForMultiGesture => _gestureCalculator != null && _gestureCalculator.IsConsideringForMultiTouch(this);
    }
}