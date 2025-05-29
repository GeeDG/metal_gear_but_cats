using Godot;
using System;

public partial class PlayerHealthManager : Node2D
{
	[Export] Label fearLevelTest;
	[Export] Label fearDamageTest;

	[Export] public float damageThreshold;
	[Export] public float guardDPS;
	[Export] public float calmRate;
	float currentDamage;
	public int fearLevel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		currentDamage = 0f;
		fearLevel = 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		fearLevelTest.Text = "Fear: " + fearLevel;
		fearDamageTest.Text = "Damage: " + currentDamage;
	}

    public override void _PhysicsProcess(double delta)
    {
        if (currentDamage > 0f)
			currentDamage -= calmRate * (float) delta;
		if (currentDamage < 0f)
			currentDamage = 0f;
    }

    public void AddDamage()
	{
		currentDamage += guardDPS;

		if (currentDamage >= damageThreshold)
		{
			currentDamage = 0f;
			fearLevel++;
		}
	}

	public void ShowFear(bool show)
	{
		Visible = show;
	}
}
