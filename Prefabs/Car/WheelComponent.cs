using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinguinCarnage.Prefabs.Car;

public class WheelComponent
{
    public string PositionString { get; private set; }
    public RayCast3D SuspensionRayCast { get; private set; }
    public MeshInstance3D WheelMesh { get; private set; }
    public GpuParticles3D ParticleEmitter { get; private set; }

    public WheelComponent(Node tree, string positionString)
    {
        PositionString = positionString;
        SuspensionRayCast = tree.GetNode<RayCast3D>($"{positionString}RayCast");
        WheelMesh = tree.GetNode<MeshInstance3D>($"Wheels/{positionString}Wheel");
        if (tree.GetNodeOrNull($"Wheels/{positionString}ParticleEmitter") != null)
            ParticleEmitter = tree.GetNode<GpuParticles3D>($"Wheels/{positionString}ParticleEmitter");
    }
}
