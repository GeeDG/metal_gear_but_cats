using Godot;
using System;

public partial class CameraOverrideArea : Area3D
{
	[Export] public Vector3 cameraPosition;
	[Export] public bool fixedPosition;
	[Export] public bool overridePlayerInput;
	[Signal] public delegate void NewCameraAngleEventHandler(Vector3 cameraPosition, Vector3 playerPosition, bool fixedPosition);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CameraManager playerCamera = GetNode<CameraManager>("/root/" + GetTree().Root.GetChild(0).Name + "/Game Manager/Player Camera");
		NewCameraAngle += playerCamera.OverrideCamera;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		foreach (Node3D node in GetOverlappingBodies())
		{
			if (node.Name == "Player")
			{
				EmitSignal(SignalName.NewCameraAngle, cameraPosition, node.GlobalPosition, fixedPosition);
			}
		}
	}
}
