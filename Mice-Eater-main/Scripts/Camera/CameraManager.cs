using Godot;
using System;

public partial class CameraManager : Camera3D
{
	const int deoverrideFrames = 2;
	int frames;
	bool overrideCamera;

	public override void _Ready()
	{
		overrideCamera = false;
		frames = 0;
	}

	
	public override void _PhysicsProcess(double delta)
	{
		if (frames < deoverrideFrames)
			frames++;
	}

	public void OverrideCamera(Vector3 cameraPosition, Vector3 playerPosition, bool fixedPosition)
	{
		frames = 0;

		if (!fixedPosition)
			GlobalPosition = cameraPosition + playerPosition;
		else
			GlobalPosition = cameraPosition;

		LookAt(playerPosition);
	}

	public void PerchCamera(Vector3 target, Vector3 offset)
	{
		if (frames >= deoverrideFrames)
		{
			GlobalPosition = target + offset;
			LookAt(target);
		}
	}
}
