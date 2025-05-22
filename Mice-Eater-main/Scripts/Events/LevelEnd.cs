using Godot;
using System;

public partial class LevelEnd : Area3D
{
	[Signal] public delegate void PlayerReachedEndEventHandler();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PlayerReachedEnd += GetNode<GameManager>("/root/" + GetTree().Root.GetChild(0).Name + "/Game Manager").WinCondition;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (HasOverlappingBodies())
			EmitSignal(SignalName.PlayerReachedEnd);
	}
}
