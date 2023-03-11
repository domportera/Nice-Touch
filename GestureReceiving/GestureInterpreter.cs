using System;
using Godot;

namespace NiceTouch.GestureReceiving{
    public class GestureInterpreter : Node, IGestureInterpreter
    {
        [Export] bool _preventPropagation = false;
        [Export] GestureInputMode _inputMode = GestureInputMode.UnhandledInput;
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

        enum GestureInputMode {Input, UnhandledInput}

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (_inputMode != GestureInputMode.Input)
                return;
            
            AcceptGestures(@event);
        }
        
        public override void _UnhandledInput(InputEvent @event)
        {
            base._UnhandledInput(@event);
            
            if (_inputMode != GestureInputMode.UnhandledInput)
                return;
            
            AcceptGestures(@event);
        }

        void AcceptGestures(InputEvent input)
        {
            switch (input)
            {
                case TouchBegin touchBegin:
                    touchBegin.AcceptGesturesNode(this, _preventPropagation);
                    return;
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

        public virtual void OnSingleTap(Touch args)
        {
            SingleTap?.Invoke(this, args);
        }

        public virtual void OnTwist(TwistData args)
        {
            Twist?.Invoke(this, args);
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