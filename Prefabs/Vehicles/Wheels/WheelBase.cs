using Godot;
using System;

namespace PinguinCarnage.Prefabs.Vehicles.Wheels;

[Tool]
public partial class WheelBase : Node3D
{
    [Export]
    public float SuspensionLength = 0.6f;
    [Export]
    public float SuspensionForce = 180f;
    [Export]
    public float WheelRadius = 0.2f;
    [Export]
    public bool SteeringWheel = false;
    [Export]
    public bool DriveWheel = false;
    [Export]
    public Node3D WheelMesh;
    [Export]
    public GpuParticles3D ParticleEmitter;
}
