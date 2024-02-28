using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

public partial class CarTest : RigidBody3D
{
    [Export]
    public float DeadZone = 0.2f;
    [Export]
    public float SuspensionLength = 0.8f;
    [Export]
    public float SuspensionForce = 50;
    [Export]
    public float LinearMaxDamp = 8;
    [Export]
    public float TiltRatio = 0.3f;
    [Export]
    public float MaxTorque = 100f;
    [Export]
    public float MaxTurningForce = 30f;

    private bool OnRoad => _wheelsOnRoad == 4;
    private Dictionary<RayCast3D, MeshInstance3D> _wheels = new();
    private int _wheelsOnRoad = 0;

    public override void _Ready()
	{
        new List<string>(){ "BackLeft", "BackRight", "FrontLeft", "FrontRight"}
            .ForEach(str => _wheels.Add(
                (RayCast3D)GetNode(str + "RayCast"), 
                (MeshInstance3D)GetNode("Wheels/" + str + "Wheel")));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public override void _PhysicsProcess(double delta)
    {
        //DEBUG
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Random random = new Random();
            int randomNumber = random.Next(40) - 20;
            int randomNumber2 = random.Next(20) - 10;
            ApplyForce(new Vector3(0, Mass * 666, 0));
            ApplyForce(new Vector3(0, Mass * 100, 0), GlobalPosition +
                new Vector3((float)randomNumber / 20f, 0, (float)randomNumber2 / 20f));
        }

        _wheelsOnRoad = 0;
        foreach(var w in _wheels) HandleSuspension(w.Key, w.Value);

        LinearDamp = OnRoad ? LinearMaxDamp : 0;
        if (_wheelsOnRoad < 2) return;

        //Throttle
        Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        float forwardDirection = Math.Abs(inputDirection.Y) > DeadZone ?
            Math.Abs(inputDirection.Y) / inputDirection.Y * 1f : 0f;
        Vector3 forwardVector = Transform.Basis.X.Normalized();
        forwardVector.Y = 0f;
        ApplyCentralForce(forwardVector * MaxTorque * forwardDirection);
        CenterOfMass = new Vector3(-forwardDirection * TiltRatio, CenterOfMass.Y, CenterOfMass.Z);

        //Turning
        float angle90Rad = 1.5708f;
        float sideDirection = Math.Abs(inputDirection.X) > DeadZone ?
            Math.Abs(inputDirection.X) / inputDirection.X * 1f : 0f;
        ApplyTorque(new Vector3(0f, -sideDirection, 0f) * angle90Rad * MaxTurningForce);
    }

    private void HandleSuspension(RayCast3D suspensionRayCast, MeshInstance3D wheelMesh)
    {
        Vector3 wheelMaxPosition = GetWheelPosition(SuspensionLength, suspensionRayCast, wheelMesh);
        if (!suspensionRayCast.IsColliding())
        {
            wheelMesh.Position = wheelMaxPosition;
            return;
        }

        Vector3 collisionPoint = suspensionRayCast.GetCollisionPoint();
        float distance = Math.Abs(suspensionRayCast.GlobalPosition.DistanceTo(collisionPoint));
        if (distance > SuspensionLength)
        {
            wheelMesh.Position = wheelMaxPosition;
            return;
        }

        float forceRatio = 1f - (distance / SuspensionLength);
        Vector3 direction = collisionPoint.DirectionTo(suspensionRayCast.GlobalPosition).Normalized();
        ApplyForce(direction * forceRatio * SuspensionForce, suspensionRayCast.GlobalPosition - GlobalPosition);
        wheelMesh.Position = GetWheelPosition(distance, suspensionRayCast, wheelMesh);
        _wheelsOnRoad++;
    }

    private Vector3 GetWheelPosition(float suspensionLength, RayCast3D suspensionRayCast, MeshInstance3D wheelMesh)
    {
        float wheelRadius = ((CylinderMesh)wheelMesh.Mesh).TopRadius;
        return suspensionRayCast.Position - (suspensionRayCast.Basis.Y.Normalized() * (suspensionLength - wheelRadius));
    }
}
