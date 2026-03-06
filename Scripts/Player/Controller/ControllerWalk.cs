using Godot;
using System;
using Voxel;
using Voxel.World;

public class ControllerWalk : Controller
{
    readonly CapsuleShape3D groundedShape = new() { Height = 0.01f, Radius = 0.295f };

    private Vector3 wishVelocity = Vector3.Zero;
    private bool grounded = false;
    private bool groundedLastTick = false;
    private float fallVelocity = 0f;

    public override void ControllerProcess(double delta, in Player player)
    {
        base.ControllerProcess(delta, player);
    }

    public override void ControllerPhysicsProcess(double delta, in Player player)
    {
        base.ControllerPhysicsProcess(@delta, player);

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = groundedShape,
            Transform = new Transform3D(Basis.Identity, player.GlobalPosition),
            Exclude = [player.GetRid()]
        };

        var trace = player.GetWorld3D().DirectSpaceState.IntersectShape(query, 1);
        grounded = trace.Count > 0;

        wishVelocity.X += Input.GetAxis("backward", "forward") * 0.75f;
        wishVelocity.Z += Input.GetAxis("left", "right") * 0.75f;

        if (grounded)
        {
            if (!groundedLastTick) wishVelocity.Y = -4f;
            if (Input.IsActionJustPressed("jump"))
            {
                wishVelocity.Y = 11f;
            }
            fallVelocity = 0f;
            wishVelocity.Y -= 0.1f;
        }
        else
        {
            fallVelocity += 0.008f;
            if (fallVelocity > 1.5f) fallVelocity = 1.5f;
            wishVelocity.Y -= 0.5f + fallVelocity;
        }

        wishVelocity *= new Vector3(0.8f, 0.96f, 0.8f);

        var rotate = wishVelocity.Rotated(Vector3.Up, player.Rotation.Y + 1.5707963267948966f);
        player.Velocity = rotate;
        player.MoveAndSlide();

        groundedLastTick = grounded;
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
                        if (buttonEvent.CtrlPressed)
                        {
                            var action = new BlockActionArea()
                            {
                                Position = player.AimHitPosition,
                                Radius = 20,
                                Shape = BlockActionArea.ActionShape.Explosive,
                            };
                            action.Apply();
                        }
                        else
                        {
                            Chunk.ChunkPlaceBlock(player.AimBlockFrontPosition, "base:brick");
                        }
                    }
                    break;
            }
        }
    }
}