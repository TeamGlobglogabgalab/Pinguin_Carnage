using Godot;
using PinguinCarnage.Pefabs.Car.Test;
using System;

namespace PinguinCarnage.Levels.Test;

public partial class LevelTest : Node3D
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
		_labelSpeed.Text = "Speed : " + Math.Round(_car.Speed, 2) + "\n";
        _labelSpeed.Text += "Torque : " + Math.Round(_car.CurrentTorque, 2) + "\n";
        _labelSpeed.Text += "Side : " + Math.Round(_car.SideSpeed, 2) + "\n";
		_labelSpeed.Text += "Linear Damp : " + Math.Round(_car.LinearDamp, 2) + "\n";
		_labelSpeed.Text += "Floor Normal : " + Math.Round(_car.FloorNormal.X, 2) + "  " + Math.Round(_car.FloorNormal.Y, 2) + "  " + Math.Round(_car.FloorNormal.Z, 2) + "\n";
        _labelSpeed.Text += "Rotation : " + Math.Round(_car.RotationDegrees.X, 2) + "  " + Math.Round(_car.RotationDegrees.Y, 2) + "  " + Math.Round(_car.RotationDegrees.Z, 2) + "\n";
		_labelSpeed.Text += _car.GetSuspensionCompressions();
    }
}
