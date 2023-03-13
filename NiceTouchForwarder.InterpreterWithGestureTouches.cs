using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NiceTouch.GestureReceiving;

namespace NiceTouch
{
    public partial class NiceTouchForwarder
    {
        class InterpreterWithGestureTouchesPool
        {
            readonly Queue<InterpreterWithGestureTouches> _pool = new Queue<InterpreterWithGestureTouches>(); 
            public readonly List<InterpreterWithGestureTouches> UsersWithTouches = new List<InterpreterWithGestureTouches>(); 
            
            public void RefreshPool()
            {
                foreach (InterpreterWithGestureTouches u in UsersWithTouches)
                {
                    _pool.Enqueue(u);
                }
                
                UsersWithTouches.Clear();
            }
        
            public InterpreterWithGestureTouches New(IGestureInterpreter interpreter)
            {
                InterpreterWithGestureTouches interpreterWithGestureTouches;
                if (_pool.Count == 0)
                {
                    interpreterWithGestureTouches = new InterpreterWithGestureTouches(interpreter, new List<Touch>());
                }
                else
                {
                    interpreterWithGestureTouches = _pool.Dequeue();
                    interpreterWithGestureTouches.Replace(interpreter);
                }

                return interpreterWithGestureTouches;
            }
        }

        class InterpreterWithGestureTouches
        {
            public InterpreterWithGestureTouches(IGestureInterpreter interpreter, List<Touch> touches)
            {
                Interpreter = interpreter;
                Touches = touches;
            }

            public void Replace(IGestureInterpreter interpreter)
            {
                Interpreter = interpreter;
                Touches.Clear();
            }

            public IGestureInterpreter Interpreter { get; private set; }
            public List<Touch> Touches { get; }
            
            // need to duplicate this list as it will be recycled and modified in the InterpreterWithGestureTouches pool
            public List<Touch> TouchListCopy => Touches.ToList();
            public bool JustOneTouch => Touches.Count == 1;
        }
    }
}