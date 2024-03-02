using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace PinguinCarnage.Pefabs.Car.Test;

public partial class CarTest : RigidBody3D
{
    [Export]
    public float DeadZone = 0.2f;
    [Export]
    public float SuspensionLength = 0.6f;
    [Export]
    public float SuspensionForce = 180f;
    [Export]
    public float LinearBaseDamp = 2f;
    [Export]
    public float TiltRatio = 0.2f;
    [Export]
    public float MaxTorque = 360f;
    [Export]
    public float Acceleration = 50f;
    [Export]
    public float TurningForceBase = 30f;
    [Export]
    public float TurningForceDrift = 90f;
    [Export]
    public float FrictionForce = 10f;

    public float ForwardSpeed => Math.Abs(LinearVelocity.Dot(Transform.Basis.X));
    public float SideSpeed => LinearVelocity.Dot(GetSideVector());
    public bool IsDrifting { get; private set; }

    private bool OnRoad => _wheelsOnRoad == 4;

    private Dictionary<RayCast3D, MeshInstance3D> _wheels = new();
    private RayCast3D _frontRayCast;
    private RayCast3D _backRayCast;
    private RayCast3D _leftSideRayCast;
    private RayCast3D _rightSideRayCast;
    private float _currentTorque = 0;
    private int _wheelsOnRoad = 0;
    private Vector3 _initialPosition;

    public override void _Ready()
    {
        new List<string>() { "BackLeft", "BackRight", "FrontLeft", "FrontRight" }
            .ForEach(str => _wheels.Add(
                (RayCast3D)GetNode(str + "RayCast"),
                (MeshInstance3D)GetNode("Wheels/" + str + "Wheel")));

        _frontRayCast = (RayCast3D)GetNode("FrontRayCast");
        _backRayCast = (RayCast3D)GetNode("BackRayCast");

        _leftSideRayCast = (RayCast3D)GetNode("LeftSideRayCast");
        _rightSideRayCast = (RayCast3D)GetNode("RightSideRayCast");

        _initialPosition = this.GlobalPosition;
        IsDrifting = false;
    }

    public override void _Process(double delta)
    {
        if(Input.IsActionJustPressed("cmd_reset")) //R key
        {
            this.GlobalPosition = _initialPosition;
            LinearVelocity = Vector3.Zero;
            Rotation = Vector3.Zero;
        }

        //Space key
        Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        IsDrifting = Input.IsActionPressed("cmd_drift") && Math.Abs(inputDirection.X) > DeadZone;

        //Throttle - Torque
        float forwardDirection = Math.Abs(inputDirection.Y) > DeadZone ?
            Math.Abs(inputDirection.Y) / inputDirection.Y * 1f : 0f;
        _currentTorque = Mathf.Lerp(_currentTorque, MaxTorque * forwardDirection, Acceleration * (float)delta);
    }

    public float friction;

    public override void _PhysicsProcess(double delta)
    {
        //DEBUG JUMP
        /*if (Input.IsActionJustPressed("ui_accept"))
        {
            Random random = new Random();
            int randomNumber = random.Next(40) - 20;
            int randomNumber2 = random.Next(20) - 10;
            ApplyForce(new Vector3(0, Mass * 666, 0));
            ApplyForce(new Vector3(0, Mass * 100, 0), GlobalPosition +
                new Vector3((float)randomNumber / 20f, 0, (float)randomNumber2 / 20f));
        }*/

        _wheelsOnRoad = 0;
        foreach (var w in _wheels) HandleSuspension(w.Key, w.Value);
        CenterOfMass = new Vector3(0f, CenterOfMass.Y, 0f);

        //Damp
        Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        LinearDamp = _wheelsOnRoad > 0 ? 
            (Math.Abs(inputDirection.Y) < DeadZone && ForwardSpeed < 3f && !IsDrifting ? 10f : LinearBaseDamp * Math.Max(1f, ForwardSpeed / 10f)) : 
            Mathf.Lerp(LinearDamp, 0f, (float)delta * 20f);
        AngularDamp = _wheelsOnRoad > 0 ? 10f : 1f;
        if (_wheelsOnRoad == 0) return;

        //Tilt adjustment
        float tilt = ForwardSpeed < 8 ? TiltRatio * ForwardSpeed * 0.125f : TiltRatio;

        //Throttle
        float forwardDirection = Math.Abs(inputDirection.Y) > DeadZone ?
            Math.Abs(inputDirection.Y) / inputDirection.Y * 1f : 0f;
        Vector3 forwardVector = GetForwardVector().Normalized();
        ApplyCentralForce(forwardVector * _currentTorque);
        //Front/Back tilt
        CenterOfMass = new Vector3(-forwardDirection * tilt, CenterOfMass.Y, CenterOfMass.Z);

        //Friction force
        friction = FrictionForce;
        //Increase friction at lower speed to limit slow drift
        friction = ForwardSpeed >= 8f ? IsDrifting ? 0f : friction :
            !IsDrifting ? friction + (friction * (8f - ForwardSpeed)) : friction * (8f - ForwardSpeed)/10f;
        ApplyCentralForce(GetSideVector() * -SideSpeed * Mass * friction);

        //Turning
        float turningForce = IsDrifting ? TurningForceDrift : TurningForceBase;
        turningForce = ForwardSpeed < 8f ? turningForce * ForwardSpeed * 0.125f : turningForce;
        float angle90Rad = 1.5708f;
        float sideDirection = Math.Abs(inputDirection.X) > DeadZone ?
            Math.Abs(inputDirection.X) / inputDirection.X * 1f : 0f;
        ApplyTorque(new Vector3(0f, -sideDirection, 0f) * angle90Rad * turningForce);
        //Side tilt
        CenterOfMass = new Vector3(CenterOfMass.X, CenterOfMass.Y, sideDirection * tilt / 2f);
    }

    private void HandleSuspension(RayCast3D suspensionRayCast, MeshInstance3D wheelMesh)
    {
        float wheelRadius = ((CylinderMesh)wheelMesh.Mesh).TopRadius;
        Vector3 wheelMaxPosition = suspensionRayCast.Position - (suspensionRayCast.Basis.Y.Normalized() * (SuspensionLength - wheelRadius));
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
        ApplyForce(direction * forceRatio * SuspensionForce * Math.Max(1f, ForwardSpeed / 5f), suspensionRayCast.GlobalPosition - GlobalPosition);
        wheelMesh.GlobalPosition = GetWheelPosition(distance, suspensionRayCast, wheelMesh);
        _wheelsOnRoad++;
    }

    private Vector3 GetWheelPosition(float suspensionLength, RayCast3D suspensionRayCast, MeshInstance3D wheelMesh)
    {
        float wheelRadius = ((CylinderMesh)wheelMesh.Mesh).TopRadius;
        var v = suspensionRayCast.GlobalPosition - (suspensionRayCast.Basis.Y.Normalized() * (suspensionLength - wheelRadius));

        var c = suspensionRayCast.GetCollisionPoint();
        //var d = c.DirectionTo(suspensionRayCast.GlobalPosition).Normalized();
        v.Y = c.Y + wheelRadius;
        return v;
    }

    private Vector3 GetForwardVector()
    {
        if (!_frontRayCast.IsColliding() && !_backRayCast.IsColliding())
            return Transform.Basis.X;
        return _backRayCast.GetCollisionPoint().DirectionTo(_frontRayCast.GetCollisionPoint());
    }

    private Vector3 GetSideVector()
    {
        if (!_leftSideRayCast.IsColliding() && !_rightSideRayCast.IsColliding())
            return Transform.Basis.Z;
        return _leftSideRayCast.GetCollisionPoint().DirectionTo(_rightSideRayCast.GetCollisionPoint());
    }
}
