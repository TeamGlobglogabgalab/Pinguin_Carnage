using Godot;
using System;

public partial class CameraTPS : Node3D
{
    private Node3D _camera;
    private float _mouseSensitivity = 0.1f;

    public override void _Ready()
    {
        _camera = GetNode<Node3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion) return;

        var _event = (InputEventMouseMotion)@event;

        RotateY(Mathf.DegToRad(-_event.Relative.X * _mouseSensitivity));

        _camera.RotateX(Mathf.DegToRad(-_event.Relative.Y - _mouseSensitivity));


    }
}
