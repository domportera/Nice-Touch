using Godot;

public abstract class NiceTouchAction : InputEventAction
{
    public Vector2 Position { get; }
	
    public NiceTouchAction(Vector2 position)
    {
        Action = GetType().Name;
        Pressed = true;
        Position = position;
		
        if (!InputMap.HasAction(Action))
        {
            InputMap.AddAction(Action);
            GD.Print(this, $"ADDED: {Action}");
        }
    }
}