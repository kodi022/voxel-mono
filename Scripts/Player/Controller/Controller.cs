using Godot;
using System;
using Voxel;

public class Controller
{
    public virtual void ControllerProcess(double delta, in Player player)
    {

    }

    public virtual void ControllerPhysicsProcess(double delta, in Player player)
    {

    }

    public virtual void ControllerInput(InputEvent @event, in Player player)
    {
        if (@event is InputEventMouseMotion motionEvent && player.MouseState == Input.MouseModeEnum.Captured)
        {
            var camRot = Mathf.Clamp(player.Camera3D.Rotation.X - motionEvent.Relative.Y * 0.01f, -1.5f, 1.5f);
            player.Camera3D.Rotation = new Vector3(camRot, 0, 0);
            player.Rotation -= new Vector3(0, motionEvent.Relative.X * 0.01f, 0);
        }
    }
}
