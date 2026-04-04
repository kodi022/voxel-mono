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

    private bool holdingFire = false;
    private double timeSinceHit = 0d;

    public override void ControllerProcess(double delta, in Player player)
    {
        base.ControllerProcess(delta, player);
    }

    public override void ControllerPhysicsProcess(double delta, in Player player)
    {
        base.ControllerPhysicsProcess(delta, player);

        timeSinceHit += delta;

        if (holdingFire && player.AimHitting)
        {
            // due to float, timeSinceHit becomes 0.09999... instead of 0.1
            if (timeSinceHit > 0.099f)
            {
                timeSinceHit = 0;
                var dmgInfo = new Voxel.Resource.DamageInfo()
                {
                    BlockPosition = player.AimHitBlockPosition,
                    Damage = player.DrillLevel * 0.1d,
                    FaceNormal = player.AimHitNormal,
                    Player = player.Name,
                    HitPosition = player.AimHitPosition,
                    Tool = "drill"
                };
                Chunk.ChunkHitBlock(dmgInfo);
            }
        }

        if (wishVelocity.Y < 0)
        {
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = groundedShape,
                Transform = new Transform3D(Basis.Identity, player.GlobalPosition),
                Exclude = [player.GetRid()]
            };

            var trace = player.GetWorld3D().DirectSpaceState.IntersectShape(query, 1);
            grounded = trace.Count > 0;
        }
        else
        {
            grounded = false;
        }

        Vector3 input = Vector3.Zero;
        input.X += Input.GetAxis("backward", "forward") * 0.8f;
        input.Z += Input.GetAxis("left", "right") * 0.8f;
        wishVelocity += input.Normalized();

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

            // stepup trace
            var vel = new Vector3(player.Velocity.X, 0, player.Velocity.Z).Normalized() * 0.3f;
            var stepCheckPos = player.GlobalPosition + vel;

            var query = new PhysicsRayQueryParameters3D
            {
                From = stepCheckPos + Vector3.Up * 0.6f,
                To = stepCheckPos + Vector3.Up * 0.01f,
                Exclude = [player.GetRid()]
            };
            var trace = player.GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (trace.TryGetValue("position", out Variant value))
            {
                var pos = (Vector3)value;
                GD.Print(pos);
                var difference = pos.Y - stepCheckPos.Y;
                if (difference < 0.51f && difference > 0.01f)
                {
                    player.GlobalPosition += Vector3.Up * difference;
                }
            }
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

        if (@event is InputEventMouseButton buttonEvent)
        {
            if (buttonEvent.ButtonIndex == MouseButton.Left)
            {
                holdingFire = buttonEvent.Pressed;
            }

            // if (!player.AimHitting) return;

            // switch (buttonEvent.ButtonIndex)
            // {
            //     case MouseButton.Left:

            //         break;
            //     case MouseButton.Right:
            //         if (player.FrameTraceResult.ContainsKey("position"))
            //         {
            //             if (buttonEvent.CtrlPressed)
            //             {
            //                 var action = new BlockActionArea()
            //                 {
            //                     Position = BlockVec3.FromVector3(player.AimHitPosition),
            //                     Radius = 20,
            //                     Shape = BlockActionArea.ActionShape.Explosive,
            //                 };
            //                 action.Apply();
            //             }
            //             else
            //             {
            //                 Chunk.ChunkPlaceBlock(player.AimBlockFrontPosition, "base:debugtwoface");
            //             }
            //         }
            //         break;
            // }
        }
    }
}