using Godot;
using PinguinCarnage.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace PinguinCarnage.Tools;

public static class InputTools
{
    [Range(0, 1)]
    public static float DeadZone { get; set; } = 0.2f;

    public static Vector2 GetDirections()
    {
        Vector2 inputDirection = Input.GetVector(InputConstant.LEFT,
            InputConstant.RIGHT, InputConstant.UP, InputConstant.DOWN);
        float x = Math.Abs(inputDirection.X) > DeadZone ? 1f * Mathf.Sign(inputDirection.X) : 0f;
        float y = Math.Abs(inputDirection.Y) > DeadZone ? 1f * Mathf.Sign(inputDirection.Y) : 0f;
        return new Vector2(x, y);
    }
}
