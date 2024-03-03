using Godot;
using PinguinCarnage.Constants;
using PinguinCarnage.Extension;
using PinguinCarnage.Tools;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using static Godot.Image;

namespace PinguinCarnage.Pefabs.Car.Test;

public partial class CarTest : RigidBody3D
{
    [Export]
    public float SuspensionLength = 0.6f;
    [Export]
    public float WheelGap = 0f;
    [Export]
    public float SuspensionForce = 180f;
    [Export]
    public float LinearBaseDamp = 2f;
    [Export]
    public float AngularBaseDamp = 9f;
    [Export]
    public float AngularAirDamp = 3f;
    [Export]
    public float TiltAngleMax = 50f;
    [Export]
    public float TiltRatio = 0.4f;
    [Export]
    public float MaxTorque = 360f;
    [Export]
    public float Acceleration = 2.5f;
    [Export]
    public float TurningForceBase = 30f;
    [Export]
    public float TurningForceDrift = 80f;
    [Export]
    public float AirControlForce = 10f;
    [Export]
    public float FrictionForce = 15f;
    [Export]
    public float JumpForce = 15f;

    public float Speed => Math.Abs(SignedSpeed);
    public float SignedSpeed => LinearVelocity.Dot(Transform.Basis.X);
    public float SideSpeed => LinearVelocity.Dot(GetSideVector());
    public Vector3 RelativeVectorUp => GlobalTransform.Basis.Y.Normalized();
    public bool IsDrifting { get; private set; } = false;
    public Vector3 FloorNormal { get; private set; } = Vector3.Up;

    private Vector2 InputDirection => InputTools.GetDirections();
    private bool InTheAir => _wheelsOnRoad == 0;
    private bool OnRoad => _wheelsOnRoad > 0;

    private Dictionary<RayCast3D, MeshInstance3D> _wheels = new();
    private RayCast3D _frontRayCast;
    private RayCast3D _backRayCast;
    private RayCast3D _leftSideRayCast;
    private RayCast3D _rightSideRayCast;
    private RayCast3D _middleRayCast;
    private float _currentTorque = 0;
    private int _wheelsOnRoad = 0;
    private Vector3 _initialPosition;

    public override void _Ready()
    {
        _wheels.Add((RayCast3D)GetNode("FrontLeftRayCast"), (MeshInstance3D)GetNode("Wheels/FrontLeftWheel"));
        _wheels.Add((RayCast3D)GetNode("BackLeftRayCast"), (MeshInstance3D)GetNode("Wheels/BackLeftWheel"));
        _wheels.Add((RayCast3D)GetNode("FrontRightRayCast"), (MeshInstance3D)GetNode("Wheels/FrontRightWheel"));
        _wheels.Add((RayCast3D)GetNode("BackRightRayCast"), (MeshInstance3D)GetNode("Wheels/BackRightWheel"));

        _frontRayCast = (RayCast3D)GetNode("FrontRayCast");
        _backRayCast = (RayCast3D)GetNode("BackRayCast");

        _leftSideRayCast = (RayCast3D)GetNode("LeftSideRayCast");
        _rightSideRayCast = (RayCast3D)GetNode("RightSideRayCast");
        _middleRayCast = (RayCast3D)GetNode("MiddleRayCast");

        _initialPosition = this.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        //DEBUG - Reset
        if (Input.IsActionJustPressed("cmd_reset")) //R key
        {
            this.GlobalPosition = _initialPosition;
            LinearVelocity = Vector3.Zero;
            Rotation = Vector3.Zero;
        }

        HandleInputs(delta);
    }

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
        CalculateFloorNormal();
        DampTweaks(delta);
        HandleSuspensions(delta);
        if (InTheAir)
        {
            HandleAirControl();
            return;
        }

        HandleThrottling();
        HandleCornering();
        HandleJump();
        ApplyTiltTweak();
        AddSideFrictionForce();
        LimitTiltAngle(delta);
    }

    private void HandleInputs(double delta)
    {
        //Space key
        IsDrifting = OnRoad && Input.IsActionPressed("cmd_drift") &&
            Speed > 3f && Math.Abs(InputDirection.X) > 0f;

        //Throttle - Torque
        float forwardDirection = InputDirection.Y;
        _currentTorque = Mathf.Lerp(_currentTorque, MaxTorque * forwardDirection, Acceleration * (float)delta);
    }

    private void CalculateFloorNormal()
    {
        FloorNormal = Vector3.Up;
        if (_middleRayCast.IsColliding()) FloorNormal = _middleRayCast.GetCollisionNormal();
    }

    private void DampTweaks(double delta)
    {
        if(InTheAir) CenterOfMass = new Vector3(0f, CenterOfMass.Y, 0f);

        if(OnRoad)
        {
            //Slow the car quickly at low speed if not drifting or throttling
            if (Math.Abs(InputDirection.Y) == 0f && Speed < 3f && !IsDrifting)
                LinearDamp = 20f;
            else
                //Linear damp increase with speed
                LinearDamp = LinearBaseDamp * Math.Max(1f, Speed / 10f);
        }
        else
            LinearDamp = Mathf.Lerp(LinearDamp, 0f, (float)delta * 20f);

        AngularDamp = OnRoad ? AngularBaseDamp : AngularAirDamp;
    }

    private void HandleSuspensions(double delta)
    {
        _wheelsOnRoad = 0;
        foreach (var w in _wheels) HandleSuspension(w.Key, w.Value, delta);
    }

    private void HandleSuspension(RayCast3D suspensionRayCast, MeshInstance3D wheelMesh, double delta)
    {
        wheelMesh.GlobalPosition = GetWheelPosition(suspensionRayCast, wheelMesh, delta);//  suspensionRayCast.Position - (suspensionRayCast.Basis.Y.Normalized() * (SuspensionLength - wheelRadius));
        if (!suspensionRayCast.IsColliding()) return;

        Vector3 collisionPoint = suspensionRayCast.GetCollisionPoint();
        float distance = suspensionRayCast.GlobalPosition.DistanceTo(collisionPoint);
        if (distance > SuspensionLength) return;

        float forceRatio = 1f - (distance / SuspensionLength);
        Vector3 direction = collisionPoint.DirectionTo(suspensionRayCast.GlobalPosition).Normalized();
        ApplyForce(direction * forceRatio * SuspensionForce * Math.Max(1f, Speed / 5f), suspensionRayCast.GlobalPosition - GlobalPosition);
        _wheelsOnRoad++;
    }

    private void HandleAirControl()
    {
        if (!InTheAir) return;
        Vector3 sideRotation = _backRayCast.GlobalPosition.DirectionTo(_frontRayCast.GlobalPosition);
        Vector3 frontRotation = _leftSideRayCast.GlobalPosition.DirectionTo(_rightSideRayCast.GlobalPosition);
        ApplyTorque(sideRotation * InputDirection.X * AirControlForce);
        ApplyTorque(frontRotation * InputDirection.Y * AirControlForce);
    }

    private void HandleThrottling()
    {
        //Throttle
        Vector3 forwardVector = GetForwardVector().Normalized();
        ApplyCentralForce(forwardVector * _currentTorque);
    }

    private void HandleCornering()
    {
        float turningForce = IsDrifting ? TurningForceDrift : TurningForceBase;
        turningForce = Speed < 8f ? turningForce * Speed * 0.125f : turningForce;
        int sign = SignedSpeed > 0f ? 1 : -1;
        ApplyTorque(new Vector3(0f, InputDirection.X * sign, 0f) * Mathf.DegToRad(90) * turningForce);
    }

    private void HandleJump()
    {
        if(Input.IsActionJustPressed(InputConstant.JUMP))
            ApplyCentralImpulse(Vector3.Up * JumpForce);
    }

    private void AddSideFrictionForce()
    {
        float friction = IsDrifting ? 0f : FrictionForce;
        //Increase friction at lower speed to limit slow drift
        if (Speed < 8f)
            friction = !IsDrifting ? 
                friction + (friction * (8f - Speed)) :
                FrictionForce * (8f - Speed) / 10f;

        ApplyCentralForce(GetSideVector() * -SideSpeed * Mass * friction);
    }

    private void LimitTiltAngle(double delta)
    {
        float angleRelativeToSlop = Mathf.RadToDeg(RelativeVectorUp.AngleTo(FloorNormal));
        float absoluteAngleDelta = Mathf.RadToDeg(GetSideVector().SignedAngleTo(Vector3.Up, GetForwardVector())) - 90f;
        if (OnRoad && IsDrifting && angleRelativeToSlop > TiltAngleMax)
        {
            int sign = Mathf.Sign(RotationDegrees.X);
            float maxAngle = sign * TiltAngleMax - absoluteAngleDelta - 10f * sign;
            RotationDegrees = new Vector3(Mathf.Lerp(RotationDegrees.X, maxAngle, (float)delta * 15f),
                RotationDegrees.Y, RotationDegrees.Z);
        }
    }

    private Vector3 GetForwardVector()
    {
        if (!_frontRayCast.IsColliding() && !_backRayCast.IsColliding())
            return Vector3.Zero;
        return _frontRayCast.GetCollisionPoint().DirectionTo(_backRayCast.GetCollisionPoint());
    }

    private Vector3 GetSideVector()
    {
        if (!_leftSideRayCast.IsColliding() && !_rightSideRayCast.IsColliding())
            return Vector3.Zero;
        return _leftSideRayCast.GetCollisionPoint().DirectionTo(_rightSideRayCast.GetCollisionPoint());
    }

    private void ApplyTiltTweak()
    {
        float tilt = Speed < 8 ? TiltRatio * Speed * 0.125f : TiltRatio;
        CenterOfMass = new Vector3(-InputDirection.Y * tilt, CenterOfMass.Y, InputDirection.X * tilt / 2f);
    }

    private Vector3 GetWheelPosition(RayCast3D suspensionRayCast, MeshInstance3D wheelMesh, double delta)
    {
        float wheelRadius = ((CylinderMesh)wheelMesh.Mesh).TopRadius;
        Vector3 rayCastNormal = suspensionRayCast.GlobalBasis.Column1.Normalized();
        //Vector3 rayCastNormal = suspensionRayCast.Basis.Y.Normalized();;

        Vector3 rayOrigin = suspensionRayCast.GlobalPosition;
        Vector3 rayEnd = rayOrigin + new Vector3(0, -SuspensionLength, 0);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(new PhysicsRayQueryParameters3D()
        {
            CollideWithBodies = true,
            CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid>(new List<Rid>() { GetRid() }), // Exclude self
            From = rayOrigin,
            To = rayEnd
        });

        Vector3 target;
        if (result.Count > 0 && suspensionRayCast.IsColliding())
        {
            Vector3 hitPosition = (Vector3)result["position"];
            float distance = suspensionRayCast.GlobalPosition.DistanceTo(hitPosition);
            target = suspensionRayCast.GlobalPosition - rayCastNormal * (distance - wheelRadius);
        }
        else
            target = suspensionRayCast.GlobalPosition - rayCastNormal * (SuspensionLength - wheelRadius);

        return GodotMath.Lerp(wheelMesh.GlobalPosition, target, (float)delta * 20f);
    }
}
