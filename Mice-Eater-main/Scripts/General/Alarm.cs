using Godot;
using System;

public partial class Alarm : Node
{
	public Callable callable;

    public override void _Ready()
    {
        callable = new Callable(this, MethodName.SendCall);
    }

    public void SendCall(bool state)
	{
		GetTree().CallGroup("Guards", "GlobalChase", state);
	}
}
