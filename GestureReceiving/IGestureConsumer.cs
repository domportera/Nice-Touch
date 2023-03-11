namespace NiceTouch.GestureReceiving
{
    public interface IGestureConsumer
    {
        void OnSingleTouch(object sender, Touch e);

        void OnSingleDrag(object sender, Touch e);

        void OnSingleLongPress(object sender, Touch e);

        void OnSingleSwipe(object sender, Touch e);

        void OnSingleTap(object sender, Touch e);

        void OnTwist(object sender, TwistData e);

        void OnMultiDrag(object sender, MultiDragData e);

        void OnMultiLongPress(object sender, MultiLongPressData e);

        void OnMultiSwipe(object sender, MultiSwipeData e);

        void OnMultiTap(object sender, MultiTapData e);

        void OnPinch(object sender, PinchData e);
    }
}