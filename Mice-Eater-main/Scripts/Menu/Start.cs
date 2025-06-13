using Godot;
using System;

public partial class Start : Button
{
	[Export] string path = "res://Scenes/level_v2_1.tscn";

	public void StartGame()
	{
		GetTree().ChangeSceneToFile(path);
	}
}
