using Godot;
using System;

public partial class PlayerHealthManager : Node2D
{
	[Export] Label fearLevelTest;
	[Export] Label fearDamageTest;

	[Export] TextureProgressBar testTextureProgressBar;
	[Export] ProgressBar testProgressBar;

	[Export] public float damageThreshold;
	[Export] public float guardDPS;
	[Export] public float calmRate;
	float currentDamage;
	public int fearLevel;

	bool beingDamaged;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		currentDamage = 0f;
		fearLevel = 0;

		beingDamaged = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		fearLevelTest.Text = "Fear: " + fearLevel + "/5";
		fearDamageTest.Text = "Damage: " + currentDamage;

		testProgressBar.Value = currentDamage * testProgressBar.MaxValue / damageThreshold;

		if (fearLevel > 3)
			fearLevelTest.Set("theme_override_colors/font_color", Colors.Red);
		else if (fearLevel > 2)
			fearLevelTest.Set("theme_override_colors/font_color", Colors.Orange);
		else if (fearLevel >= 0)
			fearLevelTest.Set("theme_override_colors/font_color", Colors.Green);
	}

    public override void _PhysicsProcess(double delta)
    {
		if (beingDamaged)
			beingDamaged = false;
		else
		{
			fearDamageTest.Set("theme_override_colors/font_color", Colors.White);
			StyleBoxFlat box = new();
			box.Set("bg_color", Colors.White);
			testProgressBar.Set("theme_override_styles/fill", box);
		}

        if (currentDamage > 0f)
			currentDamage -= calmRate * (float) delta;
		if (currentDamage < 0f)
			currentDamage = 0f;
    }

    public void AddDamage()
	{
		StyleBoxFlat box = new();
		box.Set("bg_color", Colors.Red);
		testProgressBar.Set("theme_override_styles/fill", box);
		fearDamageTest.Set("theme_override_colors/font_color", Colors.Red);
		beingDamaged = true;

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
