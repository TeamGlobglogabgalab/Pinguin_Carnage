using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinguinCarnage.Prefabs.Vehicles.Wheels;

public class WheelComponent
{
    public enum WheelPosition
    {
        Front = 0,
        Back = 1,
        Miscellaneous = 2,
    };

    public WheelPosition Position
    {
        get
        {
            return PositionString.ToUpper().StartsWith("FRONT") ?
                WheelPosition.Front :
                PositionString.ToUpper().StartsWith("BACK") ?
                WheelPosition.Back : WheelPosition.Miscellaneous;
        }
    }
    public float CurrentDistanceToCollision { get; set; } = 0f;
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

    public static implicit operator Variant(WheelComponent component)
    {
        return new Variant();
    }
}
