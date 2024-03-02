using Godot;
using System;

namespace PinguinCarnage.Pefabs.Camera.CameraMouse;

public partial class CameraMouse : Camera3D
{
    [Export]
    private Node3D _objectToFollow;

    public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
        Vector2 mousePosition = GetViewport().GetMousePosition();

        Vector3 rayFromCamera = ProjectRayOrigin(mousePosition);
        Vector3 rayToCamera = ProjectRayNormal(mousePosition);

        Vector3 cameraPosition = GlobalTransform.Origin;

        Vector3 rayDirection = (rayFromCamera + rayToCamera * 1000) - cameraPosition;

        LookAt(cameraPosition + rayDirection, Vector3.Up);
    }
}
