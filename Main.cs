using Godot;
using System;

public partial class Main : Node3D
{
	private CarTest _car;
	private Label _labelSpeed;

    public override void _Ready()
	{
		_car = GetNode("CarTest") as CarTest;
        _labelSpeed = GetNode("Label") as Label;
    }

	public override void _Process(double delta)
	{
		_labelSpeed.Text = "Speed : " + Math.Round(_car.CarSpeed, 2);
    }
}
