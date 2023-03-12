
namespace NiceTouch.GestureReceiving
{
    /// <summary>
    /// Interface that must be implemented to receive events from InputManager.gd
    /// Once inherited, you will want to autoload your script and modify InputManager.gd to reference that??
    /// </summary>
    public interface IGestureReceiver // TODO: should use ref structs instead (c# 7)
    {
        void OnSingleTouch(object sender, TouchData touchData);

        void OnSingleDrag(object sender, Touch touch);

        void OnSingleLongPress(object sender, Touch touch);

        void OnSingleSwipe(object sender, Touch touch);

        void OnSingleTap(object sender, Touch touch);

        void OnTwist(object sender, TwistData twistData);

        void OnMultiDrag(object sender, MultiDragData gesture);

        void OnMultiLongPress(object sender, MultiLongPressData gesture);

        void OnMultiSwipe(object sender, MultiSwipeData gesture);

        void OnMultiTap(object sender, MultiTapData gesture);

        void OnPinch(object sender, PinchData pinchData);
        void OnRawMultiDrag(object sender, RawMultiDragData e); // todo: is this necessary or useful?
        void OnRawPinchTwist(object sender, RawTwoFingerDragData e); // todo: is this necessary or useful?
    }
}