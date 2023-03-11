using System;
using System.Collections.Generic;
using NiceTouch;
using NiceTouch.GestureReceiving;
using Godot;


public class TouchAction : NiceTouchAction
{
	public Touch Touch { get; }
	public TouchAction(Touch touch) : base(touch.Position)
	{
		Touch = touch;
	}
}

public class TouchBegin : TouchAction
{
	bool _preventPropagation = false;
	public TouchBegin(Touch touch) : base(touch) {}
	public void AcceptGesturesControl<T>(T control, bool disregardMouseFilter, bool acceptFocus = true)
            where T : Control, IGestureInterpreter
        {
            if (!disregardMouseFilter)
            {
                switch (control.MouseFilter)
                {
                    case Control.MouseFilterEnum.Ignore:
                        return;
                    case Control.MouseFilterEnum.Stop:
                        AcceptNode(control);
                        control.AcceptEvent();
                        break;
                    case Control.MouseFilterEnum.Pass:
                        AcceptNode(control);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                AcceptNode(control);
            }
    
            if (acceptFocus && control.FocusMode != Control.FocusModeEnum.None)
            {
                Control.FocusModeEnum focusMode = control.FocusMode;
                control.FocusMode = Control.FocusModeEnum.All;
                control.GrabFocus();
                control.FocusMode = focusMode;
            }
        }
    	
    
        public void AcceptGesturesNode<T>(T node, bool preventPropagation)
            where T : Node, IGestureInterpreter
        {
            if (_preventPropagation) return;
            _preventPropagation = preventPropagation;
            
            if(preventPropagation)
	            node.GetViewport().SetInputAsHandled();
    		
            AcceptNode(node);
        }
    
        void AcceptNode<T>(T node) where T : Node, IGestureInterpreter
        {
            var args = new TouchBeginEventArgs(node);
            Accepted?.Invoke(this, args);
        }
    	
    
        internal event EventHandler<TouchBeginEventArgs> Accepted;
        internal class TouchBeginEventArgs : EventArgs
        {
            public readonly IGestureInterpreter Interpreter;
    
            public TouchBeginEventArgs(IGestureInterpreter interpreter)
            {
                Interpreter = interpreter;
            }
        }
}

public class RawMultiTouch<T> : NiceTouchAction where T : IMultiFingerGesture
{
	protected T Data;
	public IReadOnlyList<Touch> Touches => Data.Touches;
	public int TouchCount => Data.TouchCount;
	public Vector2 Center => Data.Center;
	public Vector2 CenterRelative => Data.CenterDelta;

	protected RawMultiTouch(ref T data) : base(data.Center)// todo: `ref` to `in` in C# 7
	{
		this.Data = data;
	}
}

public class Pinch : RawMultiTouch<PinchData>
{
	public readonly float SeparationAmount;

	public Pinch(ref PinchData data) : base(ref data)
	{
		SeparationAmount = data.SeparationAmount;
	}
}

public class Twist : RawMultiTouch<TwistData>
{
	public float TwistDegrees => Data.TwistDegrees;
	public float TwistRadians => Data.TwistRadians;
	
	public Twist(ref TwistData data) : base(ref data) {}
}

public class MultiTap : RawMultiTouch<MultiTapData>
{
	public MultiTap(ref MultiTapData data) : base(ref data) { }
}

public class MultiLongPress : RawMultiTouch<MultiLongPressData>
{
	public MultiLongPress(ref MultiLongPressData data) : base(ref data) { }
}

public class MultiDrag : RawMultiTouch<MultiDragData>
{
	public float DirectionRadians => Data.DirectionRadians;
	public float DirectionDegrees => Data.DirectionDegrees;
	public MultiDrag(ref MultiDragData data) : base(ref data) { }
}