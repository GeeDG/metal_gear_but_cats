using Godot;
using System;

public partial class GameManager : Node
{
	// General
	PlayerHealthManager playerHealthManager;
	Node2D loseScreen;
	Node2D winScreen;
	// Condition variables
	[Export] public int loseThreshold = 10;
	bool playerHasKey;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetTree().Paused = false;

		playerHealthManager = GetChild<PlayerHealthManager>(1);
		playerHasKey = false;

		playerHealthManager.Visible = true;

		loseScreen = GetChild<Node2D>(2);
		loseScreen.Visible = false;
		winScreen = GetChild<Node2D>(3);
		winScreen.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		LoseCondition();

		if (Input.IsActionJustPressed("Soft Reset"))
			SoftReset();
		else if (Input.IsActionJustPressed("Hard Reset"))
			HardReset();
	}

	public void HardReset()
	{
		GetTree().ChangeSceneToFile("res://Scenes/test_menu.tscn");
	}

	public void SoftReset()
	{
		GetTree().ReloadCurrentScene();
	}

	void LoseCondition()
	{
		if (playerHealthManager.fearLevel >= loseThreshold)
		{
			playerHealthManager.Visible = false;
			GetTree().Paused = true;
			loseScreen.Visible = true;
		}
	}

	public void WinCondition()
	{
		if (playerHasKey)
		{
			playerHealthManager.Visible = false;
			GetTree().Paused = true;
			winScreen.Visible = true;
		}
	}

	public void GrantPlayerKey()
	{
		playerHasKey = true;
	}
}
