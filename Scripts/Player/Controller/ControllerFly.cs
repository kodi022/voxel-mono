using Godot;
using System;
using Voxel;
using Voxel.World;

public class ControllerFly : Controller
{
    public override void ControllerProcess(double delta, in Player player)
    {

    }

    public override void ControllerPhysicsProcess(double delta, in Player player)
    {
        var wishVelocity = new Vector3(0, 0, 0);
        wishVelocity.X += Input.GetAxis("backward", "forward");
        wishVelocity.Z += Input.GetAxis("left", "right");
        wishVelocity = wishVelocity.Normalized().Rotated(Vector3.Up, player.Rotation.Y + 1.5707963267948966f);
        player.Velocity = wishVelocity * 15f;

        if (Input.IsKeyPressed(Key.Space))
        {
            player.Velocity += Vector3.Up * 15f;
        }
        if (Input.IsKeyPressed(Key.Shift))
        {
            player.Velocity -= Vector3.Up * 15f;
        }
        player.MoveAndSlide();
    }

    public override void ControllerInput(InputEvent @event, in Player player)
    {
        base.ControllerInput(@event, player);

        if (@event is InputEventMouseButton buttonEvent && buttonEvent.Pressed)
        {
            switch (buttonEvent.ButtonIndex)
            {
                case MouseButton.Left:
                    Chunk.ChunkMineBlock(player.AimBlockPosition);
                    break;
                case MouseButton.Right:
                    if (player.FrameTraceResult.ContainsKey("position"))
                    {
                        Chunk.ChunkPlaceBlock(player.AimBlockFrontPosition, "base:brick");
                    }
                    break;
            }
        }
    }
}