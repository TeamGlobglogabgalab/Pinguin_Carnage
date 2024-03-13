using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinguinCarnage.Prefabs.Vehicles.Wheels;

public partial class WheelComponent : WheelBase
{
    public float CurrentDistanceToCollision { get; set; } = 0f;
    public RayCast3D SuspensionRayCast { get; private set; }
    public bool ShowParticles => ParticleEmitter is not null;

    public WheelComponent(Node node, WheelBase wheelBase)
    {
        this.SuspensionLength = wheelBase.SuspensionLength;
        this.SuspensionForce = wheelBase.SuspensionForce;
        this.WheelRadius = wheelBase.WheelRadius;
        this.SteeringWheel = wheelBase.SteeringWheel;
        this.DriveWheel = wheelBase.DriveWheel;
        this.WheelMesh = wheelBase.WheelMesh;
        this.ParticleEmitter = wheelBase.ParticleEmitter;

        SuspensionRayCast = new RayCast3D()
        {
            Position = wheelBase.Position,
            CollideWithAreas = false,
            CollideWithBodies = true,
            Enabled = true,
            ExcludeParent = true,
            TargetPosition = Vector3.Down * (SuspensionLength + 0.2f)
        };
        node.AddChild(SuspensionRayCast);
    }

    public static implicit operator Variant(WheelComponent component)
    {
        return new Variant();
    }
}
