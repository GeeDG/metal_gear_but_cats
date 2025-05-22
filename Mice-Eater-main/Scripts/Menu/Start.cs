using Godot;
using System;

public partial class Start : Button
{
    public void StartGame()
	{
		GetTree().ChangeSceneToFile("res://Scenes/test_level_2.tscn");
	}
}
