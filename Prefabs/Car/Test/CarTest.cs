using Godot;
using PinguinCarnage.Constants;
using PinguinCarnage.Extension;
using PinguinCarnage.Prefabs.Car;
using PinguinCarnage.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public float Acceleration = 2f;
    [Export]
    public float TurningForceBase = 30f;
    [Export]
    public float TurningForceDrift = 80f;
    [Export]
    public float AirControlForce = 5f;
    [Export]
    public float FrictionForce = 15f;
    [Export]
    public float JumpForce = 80f;

    public float Speed => Math.Abs(SignedSpeed);
    public float CurrentTorque => _currentTorque;
    public float SignedSpeed => LinearVelocity.Dot(Transform.Basis.X);
    public float SideSpeed => LinearVelocity.Dot(GetSideVector());
    public Vector3 RelativeVectorUp => GlobalTransform.Basis.Y.Normalized();
    public bool IsDrifting { get; private set; } = false;
    public Vector3 FloorNormal { get; private set; } = Vector3.Up;

    private Vector2 InputDirection => InputTools.GetDirections();
    private bool InTheAir => _wheelsOnRoad == 0;
    private bool OnRoad => _wheelsOnRoad > 0;
    private bool AllWheelsOnRoad => _wheelsOnRoad == 4;

    private List<WheelComponent> _wheelsComponents = new();
    private RayCast3D _frontRayCast;
    private RayCast3D _backRayCast;
    private RayCast3D _leftSideRayCast;
    private RayCast3D _rightSideRayCast;
    private RayCast3D _middleRayCast;
    private float _accelerationSign = 0f;
    private float _driftDirection = 0f;
    private float _currentTorque = 0f;
    private float _currentFriction = 0f;
    private int _wheelsOnRoad = 0;
    private Vector3 _initialPosition;

    public override void _Ready()
    {
        _wheelsComponents.Add(new WheelComponent(this, "FrontLeft"));
        _wheelsComponents.Add(new WheelComponent(this, "FrontRight"));
        _wheelsComponents.Add(new WheelComponent(this, "BackLeft"));
        _wheelsComponents.Add(new WheelComponent(this, "BackRight"));

        _frontRayCast = GetNode<RayCast3D>("FrontRayCast");
        _backRayCast = GetNode<RayCast3D>("BackRayCast");

        _leftSideRayCast = GetNode<RayCast3D>("LeftSideRayCast");
        _rightSideRayCast = GetNode<RayCast3D>("RightSideRayCast");
        _middleRayCast = GetNode<RayCast3D>("MiddleRayCast");

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
        //DEBUG JUMP (pour le fun)
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Random random = new Random();
            int randomNumber = random.Next(40) - 20;
            int randomNumber2 = random.Next(20) - 10;
            ApplyForce(new Vector3(0, Mass * 666, 0));
            ApplyForce(new Vector3(0, Mass * 100, 0), GlobalPosition +
                new Vector3((float)randomNumber / 20f, 0, (float)randomNumber2 / 20f));
        }

        CalculateFloorNormal();
        DampTweaks(delta);
        HandleSuspensions(delta);
        ShowParticleDust();
        if (InTheAir)
        {
            HandleAirControl();
            return;
        }

        HandleThrottling();
        HandleCornering();
        HandleJump();
        ApplyTiltTweak();
        AddSideFrictionForce(delta);
        LimitTiltAngle(delta);
    }

    public string GetSuspensionCompressions()
    {
        string compressions = string.Empty;
        foreach (var w in _wheelsComponents)
            compressions += w.PositionString + " : " + w.CurrentDistanceToCollision.ToString() + "\n";
        return compressions;
    }

    private void HandleInputs(double delta)
    {
        //TODO - Ameliorer les condition du code de drift
        //Drift
        if (IsDrifting && (InTheAir || Input.IsActionJustReleased(InputConstant.DRIFT) || Speed < 3f))
        {
            IsDrifting = false;
            _driftDirection = 0f;
        }
        else if (!IsDrifting && OnRoad && Input.IsActionPressed(InputConstant.DRIFT) &&
            InputDirection.X != 0f && Speed >= 3f)
        {
            IsDrifting = true;
            _driftDirection = -InputDirection.X;
        }

        //Throttle - Torque
        _accelerationSign = Input.IsActionPressed(InputConstant.ACCELERATE) ? 1f :
            Input.IsActionPressed(InputConstant.BRAKE) ? -1f : 0f;
        _currentTorque = Mathf.Lerp(_currentTorque, MaxTorque * _accelerationSign, Acceleration * (float)delta);
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
            if (Math.Abs(_accelerationSign) == 0f && Speed < 3f && !IsDrifting)
                LinearDamp = 10f;
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
        foreach (var w in _wheelsComponents) HandleSuspension(w, delta);
    }

    private void HandleSuspension(WheelComponent wheel, double delta)
    {
        RayCast3D suspensionRayCast = wheel.SuspensionRayCast;
        MeshInstance3D wheelMesh = wheel.WheelMesh;
        wheelMesh.GlobalPosition = GetWheelGlobalPosition(suspensionRayCast, wheelMesh, delta);//  suspensionRayCast.Position - (suspensionRayCast.Basis.Y.Normalized() * (SuspensionLength - wheelRadius));
        wheel.CurrentDistanceToCollision = GetSuspensionDistanceToCollision(suspensionRayCast);
        if (!suspensionRayCast.IsColliding()) return;

        Vector3 collisionPoint = suspensionRayCast.GetCollisionPoint();
        if (wheel.CurrentDistanceToCollision > SuspensionLength) return;

        float forceRatio = 1f - (wheel.CurrentDistanceToCollision / SuspensionLength);
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
        float cornerDirection = IsDrifting ? _driftDirection : -InputDirection.X;
        ApplyTorque(new Vector3(0f, cornerDirection * sign, 0f) * Mathf.DegToRad(90) * turningForce);

        //Counter torque when drifting
        if (IsDrifting)
        {
            float driftCounterRatio = (Math.Abs(_driftDirection + InputDirection.X) / 2f); //0-1
            float counterTurningForce = turningForce * driftCounterRatio * 1.25f;
            counterTurningForce *= Math.Abs(SideSpeed) / 15f;
            ApplyTorque(new Vector3(0f, cornerDirection * -sign, 0f) * Mathf.DegToRad(85) * counterTurningForce);
        }
    }

    private void HandleJump()
    {
        if(Input.IsActionJustPressed(InputConstant.JUMP))
        {
            LinearDamp = 0f;
            ApplyCentralImpulse(Vector3.Up * JumpForce);
        }
    }

    private void AddSideFrictionForce(double delta)
    {
        float frictionTarget = IsDrifting ? 0f : FrictionForce;
        //Increase friction at lower speed to limit slow drift
        if (Speed < 8f)
            frictionTarget = !IsDrifting ?
                frictionTarget + (frictionTarget * (8f - Speed)) :
                FrictionForce * (8f - Speed) / 10f;

        _currentFriction = Mathf.Lerp(_currentFriction, frictionTarget, (float)delta * 10f);
        ApplyCentralForce(GetSideVector() * -SideSpeed * Mass * _currentFriction);
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

    private void ShowParticleDust()
    {
        bool directionChanging = Speed > 15f && InputDirection.X != 0f && _accelerationSign != 0f &&
            Mathf.Sign(SideSpeed) != -InputDirection.X && Math.Abs(SideSpeed) < 2f;
        bool torqueChange = Math.Abs(_currentTorque) < MaxTorque / 2f && _accelerationSign != 0f;
        bool accelerating = torqueChange && Mathf.Sign(_currentTorque) == _accelerationSign;
        bool braking = torqueChange && Mathf.Sign(_currentTorque) != _accelerationSign;

        foreach (var w in _wheelsComponents.Where(w => w.ParticleEmitter is not null))
        {
            if (!w.SuspensionRayCast.IsColliding() || InTheAir)
            {
                w.ParticleEmitter.Emitting = false;
                continue;
            }

            bool frontWheel = w.Position == WheelComponent.WheelPosition.Front;
            bool backWheel = w.Position == WheelComponent.WheelPosition.Back;
            bool suspensionCrushed = w.CurrentDistanceToCollision < 0.45f;
            if (IsDrifting || (directionChanging && frontWheel) || (accelerating && backWheel) || braking || suspensionCrushed)
            {
                float wheelRadius = GetWheelRadius(w.WheelMesh);
                Vector3 rayCastNormal = w.SuspensionRayCast.GlobalBasis.Column1.Normalized();
                Vector3 particlePosition = w.WheelMesh.GlobalPosition - rayCastNormal * 
                    wheelRadius/2f + GetForwardVector() * -wheelRadius;
                w.ParticleEmitter.GlobalPosition = particlePosition;
                w.ParticleEmitter.Emitting = true;
            }
            else
                w.ParticleEmitter.Emitting = false;
        }
    }

    private Vector3 GetForwardVector()
    {
        if (!_frontRayCast.IsColliding() && !_backRayCast.IsColliding())
            return Vector3.Zero;
        return _backRayCast.GetCollisionPoint().DirectionTo(_frontRayCast.GetCollisionPoint());
    }

    private Vector3 GetSideVector()
    {
        if (!_leftSideRayCast.IsColliding() && !_rightSideRayCast.IsColliding())
            return Vector3.Zero;
        return _rightSideRayCast.GetCollisionPoint().DirectionTo(_leftSideRayCast.GetCollisionPoint());
    }

    private void ApplyTiltTweak()
    {
        float tilt = Speed < 8 ? TiltRatio * Speed * 0.125f : TiltRatio;
        CenterOfMass = new Vector3(-_accelerationSign * tilt, CenterOfMass.Y, -InputDirection.X * tilt / 2f);
    }

    private Vector3 GetWheelGlobalPosition(RayCast3D suspensionRayCast, MeshInstance3D wheelMesh, double delta)
    {
        float wheelRadius = GetWheelRadius(wheelMesh);
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

    private float GetSuspensionDistanceToCollision(RayCast3D suspensionRayCast)
    {
        if (!suspensionRayCast.IsColliding())
            return SuspensionLength;
        return suspensionRayCast.GlobalPosition.DistanceTo(suspensionRayCast.GetCollisionPoint());
    }

    private float GetWheelRadius(MeshInstance3D wheelMesh)
    {
        if (wheelMesh?.Mesh is not CylinderMesh) return 0f;
        return ((CylinderMesh)wheelMesh.Mesh).TopRadius;
    }
}
