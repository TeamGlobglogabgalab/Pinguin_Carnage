using Godot;
using System;

namespace PinguinCarnage.Prefabs.Vehicles.Wheels;

[Tool]
public partial class WheelBase : Node3D
{
    [Export]
    public float WheelRadius;
    [Export]
    public bool SteeringWheel;
    [Export]
    public bool DriveWheel;
    [Export]
    public bool ShowParticles = true;
}
