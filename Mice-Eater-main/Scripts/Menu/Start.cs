using Godot;
using System;

public partial class Start : Button
{
	[Export] string path = "res://Scenes/level_v2_1.tscn";
	[Export] SpotLight3D spotLight3D;
	[Export] bool newMenu = false;
	[Export] float swaySpeedH;
	[Export] float swayRadiusH;
	[Export] float swaySpeedV;
	[Export] float swayRadiusV;
	float time;

    public void StartGame()
	{
		GetTree().ChangeSceneToFile(path);
	}

    public override void _Ready()
    {
		time = 0f;
    }

    public override void _Process(double delta)
    {
       if (newMenu)
	   {
			time += (float) delta;
			spotLight3D.Position = new Vector3(Mathf.Sin(time * swaySpeedH) * swayRadiusH, Mathf.Sin(time * swaySpeedV) * swayRadiusV, spotLight3D.Position.Z);
	   }
    }
}
