using Godot;
using System;

namespace NiceTouch.GestureReceiving
{
	public class ControlGestureInterpreter : Control, IGestureInterpreter
	{
		public event EventHandler<Touch> TouchBegin;
		public event EventHandler<Touch> TouchEnd;
		public event EventHandler<Touch> SingleTap;
		public event EventHandler<Touch> SingleDrag;
		public event EventHandler<Touch> SingleLongPress;
		public event EventHandler<Touch> SingleSwipe;
		public event EventHandler<MultiDragData> MultiDrag;
		public event EventHandler<MultiSwipeData> MultiSwipe;
		public event EventHandler<MultiTapData> MultiTap;
		public event EventHandler<MultiLongPressData> MultiLongPress;
		public event EventHandler<PinchData> Pinch;
		public event EventHandler<TwistData> Twist;

		readonly bool _controlIsThis;
		readonly Control _control;

		protected ControlGestureInterpreter()
		{
			_control = this;
			_controlIsThis = true;
		}

		public ControlGestureInterpreter(Control control, MouseFilterEnum mouseFilter = MouseFilterEnum.Stop)
		{
			_control = control;
			
			if (GetParent() != control)
			{
				_control.AddChild(this);
			}

			// this interpreter will be taking over the mouse interactions
			control.MouseFilter = MouseFilterEnum.Ignore;
			MouseFilter = mouseFilter;

			AnchorLeft = 0;
			AnchorRight = 1;
			AnchorTop = 0;
			AnchorBottom = 1;
			this.RectPosition = Vector2.Zero;
			
			// take their tooltip too
			HintTooltip = control.HintTooltip;
			control.HintTooltip = string.Empty;
			
			
			_controlIsThis = control == this; //always false?
			Name = $"{_control.Name} (Touch)";
		}

		public override void _Input(InputEvent input)
		{
			InterpretTouchActions(input);
		}

		public override void _GuiInput(InputEvent input)
		{
			if (!_controlIsThis) return;
			InterpretTouchActions(input);
		}

		public override void _UnhandledInput(InputEvent input)
		{
			InterpretTouchActions(input);
		}

		void InterpretTouchActions(InputEvent input)
		{
			if (!(input is NiceTouchAction action)) return;
			
			if (action is TouchBegin begin)
			{
				if(_control.GetGlobalRect().HasPoint(begin.Position))
					begin.AcceptGesturesControl(this, true, false);
			}
		}
		
		public virtual void OnTouchBegin(Touch args)
		{
			TouchBegin?.Invoke(this, args);
		}

		public void OnTouchEnd(Touch args)
		{	
			TouchEnd?.Invoke(this, args);
		}

		public virtual void OnSingleTap(Touch args)
		{
			SingleTap?.Invoke(this, args);
		}

		public virtual void OnSingleDrag(Touch args)
		{
			SingleDrag?.Invoke(this, args);
		}

		public virtual void OnSingleLongPress(Touch args)
		{
			SingleLongPress?.Invoke(this, args);
		}
		
		public virtual void OnSingleSwipe(Touch args)
		{
			SingleSwipe?.Invoke(this, args);
		}

		public virtual void OnMultiDrag(MultiDragData args)
		{
			MultiDrag?.Invoke(this, args);
		}

		public virtual void OnMultiLongPress(MultiLongPressData args)
		{
			MultiLongPress?.Invoke(this, args);
		}

		public virtual void OnMultiSwipe(MultiSwipeData args)
		{
			MultiSwipe?.Invoke(this, args);
		}
		
		public virtual void OnMultiTap(MultiTapData args)
		{
			MultiTap?.Invoke(this, args);
		}


		public virtual void OnPinch(PinchData args)
		{
			Pinch?.Invoke(this, args);
		}

		public virtual void OnTwist(TwistData args)
		{
			Twist?.Invoke(this, args);
		}
		
		public void SubscribeToGestures(IGestureConsumer consumer)
		{
			MultiDrag += consumer.OnMultiDrag;
			MultiSwipe += consumer.OnMultiSwipe;
			Pinch += consumer.OnPinch;
			TouchBegin += consumer.OnSingleTouch;
			MultiTap += consumer.OnMultiTap;
			SingleTap += consumer.OnSingleTap;
			SingleDrag += consumer.OnSingleDrag;
			SingleLongPress += consumer.OnSingleLongPress;
			MultiLongPress += consumer.OnMultiLongPress;
			Twist += consumer.OnTwist;
			SingleSwipe += consumer.OnSingleSwipe;
		}
	}
}