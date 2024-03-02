using Godot;
using System;

namespace PinguinCarnage.Pefabs.Camera.CameraGTA;

public partial class CameraGTA : Godot.Camera3D
{
	[Export]
	public Node3D ObjectToFollow;

	private Vector3 TargetPosition => ObjectToFollow.GlobalPosition;
	private float _baseDistance;
	private float _baseYGap;

    public override void _Ready()
	{
		_baseYGap = this.GlobalPosition.Y - TargetPosition.Y;
        _baseDistance = this.GlobalPosition.DistanceTo(TargetPosition);
    }

	public override void _Process(double delta)
	{
		this.GlobalPosition = new Vector3(GlobalPosition.X, TargetPosition.Y + _baseYGap, GlobalPosition.Z);
		LookAt(TargetPosition);
		if(this.GlobalPosition.DistanceTo(TargetPosition) != _baseDistance)
		{
			var direction = TargetPosition.DirectionTo(this.GlobalPosition).Normalized();
			GlobalPosition = TargetPosition + direction * _baseDistance;
        }
        /*Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		var direction = new Vector3((float)(inputDirection.X * delta * CameraSpeed), 0f, 
			(float)(inputDirection.Y * delta * CameraSpeed));
		Position += direction;*/
    }
}
