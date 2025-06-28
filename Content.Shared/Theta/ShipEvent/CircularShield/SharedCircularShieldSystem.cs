using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public class SharedCircularShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <summary>
    /// Generates vertices for a circular shield with origin at (0,0)
    /// </summary>
    public Vector2[] GenerateConeVertices(int radius, Angle angle, Angle width, int extraArcPoints = 0)
    {
        // Check if this is a full or almost full circle
        var isFullCircle = width.Degrees >= 359.0f;

        Vector2[] vertices;

        if (isFullCircle)
        {
            // For full circles, we'll still use a triangle fan but with a center point
            // and carefully placed vertices to avoid the visual artifacts
            var totalPoints = Math.Max(30, extraArcPoints);
            vertices = new Vector2[totalPoints + 2]; // +2 for center and closing the loop

            // First vertex is center point
            vertices[0] = Vector2.Zero;

            Angle step = Angle.FromDegrees(360.0f / totalPoints);

            for (var i = 0; i < totalPoints; i++)
            {
                Angle currentAngle = step * i;
                vertices[i + 1] = currentAngle.ToVec() * radius;
            }

            // Close the loop by duplicating the first edge vertex, but with a tiny offset
            // to prevent perfect overlap and shader artifacts
            vertices[totalPoints + 1] = vertices[1] * 0.999f;
        }
        else
        {
            // Original partial cone/arc implementation
            //central point + two edge points + extra arc points + central point again since this is used for drawing and input must be looped
            vertices = new Vector2[4 + extraArcPoints];
            vertices[0] = new Vector2(0, 0);

            var start = angle - width / 2;
            Angle step = width / (2 + extraArcPoints);

            for (var i = 1; i < 3 + extraArcPoints; i++)
                vertices[i] = (start + step * (i - 1)).ToVec() * radius;

            vertices[^1] = vertices[0];
        }

        return vertices;
    }

    /// <summary>
    /// Generates vertices for a cone shape with origin at the specified offset
    /// </summary>
    public Vector2[] GenerateConeVerticesWithOffset(int radius, Angle angle, Angle width, Vector2 centerOffset, int extraArcPoints = 0)
    {
        var vertices = GenerateConeVertices(radius, angle, width, extraArcPoints);

        // Apply offset to all vertices
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] += centerOffset;

        return vertices;
    }

    public bool EntityInShield(Entity<CircularShieldComponent> shield, EntityUid otherUid, SharedTransformSystem? transformSystem = null)
    //Lua fix
    {
        if (!TryComp(shield, out TransformComponent? shieldXform))
            return false;

        var gridUid = shieldXform.ParentUid;

        Vector2 center;
        Angle referenceRot;

        if (!TerminatingOrDeleted(gridUid) &&
            TryComp(gridUid, out TransformComponent? gridXform))
        {
            var gridPos = _transformSystem.GetWorldPosition(gridXform);
            referenceRot = _transformSystem.GetWorldRotation(gridXform);

            var offset = Vector2.Zero;
            if (TryComp(gridUid, out PhysicsComponent? physics))
                offset = gridXform.WorldRotation.RotateVec(physics.LocalCenter);

            center = gridPos + offset;
        }
        else
        {
            center = _transformSystem.GetWorldPosition(shield);
            referenceRot = _transformSystem.GetWorldRotation(shield);
        }

        var otherPos = _transformSystem.GetWorldPosition(otherUid);
        var delta = otherPos - center;

        var relativeAngle = ThetaHelpers.AngNormal(new Angle(delta) - referenceRot);
        var shieldStart = ThetaHelpers.AngNormal(shield.Comp.Angle - shield.Comp.Width / 2);

        return ThetaHelpers.AngInSector(relativeAngle, shieldStart, shield.Comp.Width) &&
               delta.Length() < shield.Comp.Radius + 0.1f;
    }
    //Lua fix

    public void DoShutdownEffects(Entity<CircularShieldComponent> shield)
    {
        foreach (var effect in shield.Comp.Effects)
            effect.OnShieldShutdown(shield);

        Dirty(shield);
    }

    public void DoEnterEffects(Entity<CircularShieldComponent> shield, EntityUid otherEntity)
    {
        foreach (var effect in shield.Comp.Effects)
            effect.OnShieldEnter(otherEntity, shield);

        Dirty(shield);
    }

    public void DoInitEffects(Entity<CircularShieldComponent> shield)
    {
        foreach (var effect in shield.Comp.Effects)
            effect.OnShieldInit(shield);

        Dirty(shield);
    }

    public void DoShieldUpdateEffects(Entity<CircularShieldComponent> shield, float time)
    {
        foreach (var effect in shield.Comp.Effects)
            effect.OnShieldUpdate(shield, time);

        Dirty(shield);
    }
}
