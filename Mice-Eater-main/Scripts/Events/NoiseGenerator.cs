using Godot;
using System;

public partial class NoiseGenerator : Area3D
{
	bool bodyInside;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		bodyInside = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (IsTriggered())
		{
			GetTree().CallGroup("Guards", "AddSuspicionPoint", GlobalPosition);
		}
	}

	bool IsTriggered()
	{
		bool bodyPresent = HasOverlappingBodies();

		if (!bodyInside && bodyPresent != bodyInside)
		{
			bodyInside = true;
			return true;
		}

		bodyInside = bodyPresent;
		return false;
	}
}
