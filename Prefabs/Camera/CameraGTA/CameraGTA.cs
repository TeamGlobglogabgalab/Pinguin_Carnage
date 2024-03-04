using Godot;
using PinguinCarnage.Extension;
using PinguinCarnage.Pefabs.Car.Test;
using System;

namespace PinguinCarnage.Pefabs.Camera.CameraGTA;

public partial class CameraGTA : Godot.Camera3D
{
	[Export]
	public Node3D ObjectToFollow;

	private Vector3 TargetPosition => ObjectToFollow.GlobalPosition;
	private float _baseDistance;
	private float _baseYGap;
	private float _lerpSpeed = 15f;

    public override void _Ready()
	{
		_baseYGap = this.GlobalPosition.Y - TargetPosition.Y;
        _baseDistance = this.GlobalPosition.DistanceTo(TargetPosition);
    }

	public override void _PhysicsProcess(double delta)
	{
        this.GlobalPosition = new Vector3(GlobalPosition.X, TargetPosition.Y + _baseYGap, GlobalPosition.Z);
        LookAt(TargetPosition);
        if (this.GlobalPosition.DistanceTo(TargetPosition) != _baseDistance)
        {
            var direction = TargetPosition.DirectionTo(this.GlobalPosition).Normalized();
            GlobalPosition = TargetPosition + direction * _baseDistance;
        }

        /*float objectSpeed = ((CarTest)ObjectToFollow).Speed;
        if (this.GlobalPosition.DistanceTo(TargetPosition) != _baseDistance)
        {
            var direction = TargetPosition.DirectionTo(this.GlobalPosition).Normalized();
            Vector3 targetPosition = TargetPosition + direction * _baseDistance;
            float lerpIncrease = ((CarTest)ObjectToFollow).IsDrifting ? objectSpeed / 10f : 1f;
            GlobalPosition = GodotMath.Lerp(GlobalPosition, targetPosition, (float)delta * _lerpSpeed * lerpIncrease);
        }

        Vector3 targetY = new Vector3(GlobalPosition.X, TargetPosition.Y + _baseYGap, GlobalPosition.Z);
        GlobalPosition = GodotMath.Lerp(GlobalPosition, targetY, (float)delta * _lerpSpeed);

		Vector3 targetLook = ObjectToFollow.GlobalPosition + ObjectToFollow.Basis.X.Normalized() * objectSpeed / 5f;
        GlobalTransform = GlobalTransform.InterpolateWith(GlobalTransform.LookingAt(targetLook, Vector3.Up), _lerpSpeed * (float)delta);*/
    }
}
