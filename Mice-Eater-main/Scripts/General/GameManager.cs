using Godot;
using System;

public partial class GameManager : Node
{
	// General
	PlayerHealthManager playerHealthManager;
	// Condition variables
	[Export] public int loseThreshold = 10;
	bool playerHasKey;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		playerHealthManager = GetChild<PlayerHealthManager>(1);
		playerHasKey = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		LoseCondition();
	}

	void LoseCondition()
	{
		if (playerHealthManager.fearLevel >= loseThreshold)
		{
			GD.Print("Lose");
			//TODO: lose
			GetTree().ChangeSceneToFile("res://Scenes/test_menu.tscn");
		}
	}

	public void WinCondition()
	{
		if (playerHasKey)
		{
			GD.Print("Win");
			//TODO: win
			GetTree().ChangeSceneToFile("res://Scenes/test_menu.tscn");
		}
	}

	public void GrantPlayerKey()
	{
		playerHasKey = true;
	}
}
