using Godot;
using Voxel;
using Voxel.World;

public class ControllerWalk : Controller
{
    readonly CapsuleShape3D groundedShape = new() { Height = 0.002f, Radius = 0.29f };

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

        if (wishVelocity.Y < 0)
        {
            var trace = player.GetWorld3D().DirectSpaceState.IntersectShape(query, 1);
            grounded = trace.Count > 0;
        }
        else
        {
            grounded = false;
        }

        Vector3 movement = Vector3.Zero;
        movement.X += Input.GetAxis("backward", "forward") * 0.8f;
        movement.Z += Input.GetAxis("left", "right") * 0.8f;
        wishVelocity += movement.Normalized();

        if (grounded)
        {
            if (Input.IsActionJustPressed("jump"))
            {
                wishVelocity.Y = 11f;
            }

            if (!groundedLastTick)
            {
                wishVelocity.Y = -2f;
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

        // idk why it wants rotated
        player.Velocity = wishVelocity.Rotated(Vector3.Up, player.Rotation.Y + 1.5707963267948966f);
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
                                Position = BlockVec3.FromVector3(player.AimHitPosition),
                                Radius = 20,
                                Shape = BlockActionArea.ActionShape.Explosive,
                            };
                            action.Apply();
                        }
                        else
                        {
                            Chunk.ChunkPlaceBlock(player.AimBlockFrontPosition, "base:glass");
                        }
                    }
                    break;
            }
        }
    }
}