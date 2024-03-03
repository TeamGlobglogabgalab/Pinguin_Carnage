using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinguinCarnage.Prefabs.Car;

public class WheelComponent
{
    public RayCast3D SuspensionRayCast { get; set; }
    public MeshInstance3D WheelMesh { get; set; }
    public GpuParticles3D ParticleEmitter { get; set; }

    public WheelComponent(Node tree, string stringPosition)
    {
        SuspensionRayCast = tree.GetNode<RayCast3D>($"{stringPosition}RayCast");
        WheelMesh = tree.GetNode<MeshInstance3D>($"Wheels/{stringPosition}Wheel");
        if (tree.GetNodeOrNull($"Wheels/{stringPosition}ParticleEmitter") != null)
            ParticleEmitter = tree.GetNode<GpuParticles3D>($"Wheels/{stringPosition}ParticleEmitter");
    }
}
