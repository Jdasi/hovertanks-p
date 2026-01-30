using LitJson;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = UnityEngine.Random;

public static class JHelper
{
    public static float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);

        if (dir > 0.0f)
        {
            return 1.0f;
        }
        else if (dir < 0.0f)
        {
            return -1.0f;
        }
        else
        {
            return 0.0f;
        }
    }

    public static bool CanSeeTransform(Vector3 origin, Transform target, LayerMask testLayer, bool drawRay = true)
    {
        Vector3 dir = (target.position - origin).normalized;
        float dist = Vector3.Distance(origin, target.position);

        Physics.Raycast(origin, dir, out var hitInfo, dist, testLayer);
        bool hitTarget = hitInfo.transform == target;

        if (drawRay)
        {
            Debug.DrawRay(origin, dir * dist, hitTarget ? Color.green : Color.red, 1);
        }

        return hitTarget;
    }

    public static bool IsTransformFacing(Transform transform, Vector3 pos, float angle)
    {
        Vector3 dir = (pos - transform.position).normalized;
        float angleBetween = Vector3.Angle(transform.forward, dir);
        return angleBetween < angle;
    }

    public static float RotationToHeading(Transform transform)
    {
        if (transform == null)
        {
            return 0;
        }

        return RotationToHeading(transform.rotation);
    }

    public static float RotationToHeading(Quaternion rotation)
    {
        return rotation.eulerAngles.y;
    }

    public static Quaternion HeadingToRotation(float heading)
    {
        return Quaternion.Euler(0, heading, 0);
    }

    public static Vector3 HeadingToForward(float heading)
    {
        return HeadingToRotation(heading) * Vector3.forward;
    }

    public static Vector3 HeadingToRight(float heading)
    {
        return HeadingToRotation(heading) * Vector3.right;
    }

    public static Vector3 FlatDirection(this Vector3 from, Vector3 to)
    {
        from.y = to.y;
        return (to - from).normalized;
    }

    public static float FlatDistance(this Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
    }

    public static bool IsPawnFacing(this HoverTanks.Entities.IPawn pawn, Vector3 pos, float angle)
    {
        return IsTransformFacing(pawn.SightPoint, pos, angle);
    }

    public static bool SameTeam(TeamId a, TeamId b)
    {
        if (a == TeamId.None || b == TeamId.None)
        {
            return false;
        }

        return a == b;
    }

    public static Vector3 DirectionToVector(this Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Vector3.forward;
            case Direction.Down: return -Vector3.forward;
            case Direction.Left: return -Vector3.right;
            case Direction.Right: return Vector3.right;

            default: return Vector3.zero;
        }
    }

    public static void DrawBounds(Bounds bounds, Color color, float duration = 5)
    {
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        Vector3 backBottomL = new Vector3(center.x - size.x / 2, center.y - size.y / 2, center.z + size.z / 2);
        Vector3 backBottomR = new Vector3(center.x + size.x / 2, center.y - size.y / 2, center.z + size.z / 2);
        Vector3 backTopL = new Vector3(center.x - size.x / 2, center.y + size.y / 2, center.z + size.z / 2);
        Vector3 backTopR = new Vector3(center.x + size.x / 2, center.y + size.y / 2, center.z + size.z / 2);
        Vector3 frontBottomL = new Vector3(center.x - size.x / 2, center.y - size.y / 2, center.z - size.z / 2);
        Vector3 frontBottomR = new Vector3(center.x + size.x / 2, center.y - size.y / 2, center.z - size.z / 2);
        Vector3 frontTopL = new Vector3(center.x - size.x / 2, center.y + size.y / 2, center.z - size.z / 2);
        Vector3 frontTopR = new Vector3(center.x + size.x / 2, center.y + size.y / 2, center.z - size.z / 2);

        Debug.DrawLine(backTopL, backTopR, color, duration);
        Debug.DrawLine(backBottomL, backBottomR, color, duration);
        Debug.DrawLine(backBottomL, backTopL, color, duration);
        Debug.DrawLine(backBottomR, backTopR, color, duration);
        Debug.DrawLine(frontTopL, frontTopR, color, duration);
        Debug.DrawLine(frontBottomL, frontBottomR, color, duration);
        Debug.DrawLine(frontBottomL, frontTopL, color, duration);
        Debug.DrawLine(frontBottomR, frontTopR, color, duration);
        Debug.DrawLine(backTopL, frontTopL, color, duration);
        Debug.DrawLine(backTopR, frontTopR, color, duration);
        Debug.DrawLine(backBottomL, frontBottomL, color, duration);
        Debug.DrawLine(backBottomR, frontBottomR, color, duration);
    }

    public static void BlowIntoPieces(GameObject obj)
    {
        var layer = LayerMask.NameToLayer("Debris");
        var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        int skipChance = meshRenderers.Length > 5 ? 3 : 6;

        foreach (var meshRenderer in meshRenderers)
        {
            if (Random.Range(0, skipChance) == 1)
            {
                continue;
            }

            // ignore if marked to not detatch
            if (meshRenderer.name.EndsWith("_nd"))
            {
                continue;
            }

            var meshObj = meshRenderer.gameObject;
            meshObj.layer = layer;

            var meshCol = meshObj.AddComponent<BoxCollider>();
            var meshRB = meshObj.AddComponent<Rigidbody>();

            meshRB.mass = 1;
            meshRB.AddForce(Vector3.up * Random.Range(3, 5) + new Vector3(Random.Range(-6, 6), 0, Random.Range(-6, 6)), ForceMode.Impulse);
            meshRB.AddTorque(new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10)), ForceMode.Impulse);

            meshObj.transform.SetParent(null);
            DebrisManager.Register(meshObj);
        }
    }

    public static DamageLevel PercentToDamageLevel(float percent)
    {
        int workingPercent = Convert.ToInt32(percent * 100);

        if (workingPercent >= (int)DamageLevel.Critical)
        {
            return DamageLevel.Critical;
        }
        else if (workingPercent >= (int)DamageLevel.Heavy)
        {
            return DamageLevel.Heavy;
        }
        else if (workingPercent >= (int)DamageLevel.Medium)
        {
            return DamageLevel.Medium;
        }
        else if (workingPercent > 0)
        {
            return DamageLevel.Low;
        }

        return DamageLevel.None;
    }

    public static HeatLevel PercentToHeatLevel(float percent)
    {
        int workingPercent = Convert.ToInt32(percent * 100);

        if (workingPercent >= (int)HeatLevel.Overheating)
        {
            return HeatLevel.Overheating;
        }
        else if (workingPercent >= (int)HeatLevel.Critical)
        {
            return HeatLevel.Critical;
        }
        else if (workingPercent >= (int)HeatLevel.High)
        {
            return HeatLevel.High;
        }
        else if (workingPercent >= (int)HeatLevel.Medium)
        {
            return HeatLevel.Medium;
        }

        return HeatLevel.Trivial;
    }

    /// <summary>
    /// Spaces out a "StringLikeThis" into "String Like This".
    /// </summary>
    public static string SpaceOut(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return Regex.Replace(str, "(?<=[a-z])([A-Z])", " $1" );
    }

    public static int FloorToNearest(this int i, int nearest)
    {
        return (int)(i / (float)nearest) * nearest;
    }

    public static Transform FindDescendant(this Transform parent, string name)
    {
        var queue = new Queue<Transform>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var descendant = queue.Dequeue();

            if (descendant.name.Equals(name))
            {
                return descendant;
            }

            foreach (Transform child in descendant)
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }

    public static bool CoinToss()
    {
        return Random.Range(0, 2) == 1;
    }

    public static T SelectRandom<T>(this T[] arr)
    {
        if (arr == null
            || arr.Length == 0)
        {
            return default;
        }

        return arr[Random.Range(0, arr.Length)];
    }

    public static T SelectRandom<T>(this IList<T> list)
    {
        if (list == null
            || list.Count == 0)
        {
            return default;
        }

        return list[Random.Range(0, list.Count)];
    }

    public static Vector3 GetAveragePosition(this Vector3[] positions)
    {
        if (positions == null
            || positions.Length == 0)
        {
            return Vector3.zero;
        }

        var avg = Vector3.zero;

        foreach (var pos in positions)
        {
            avg += pos;
        }

        avg /= positions.Length;
        return avg;
    }

    public static bool TryGetFloorAtPos(Vector3 pos, out Transform surface)
    {
        pos.y = 0;

        if (!Physics.Raycast(pos + Vector3.up, -Vector3.up, out var hitInfo, 2, GameManager.instance.FloorLayer))
        {
            surface = null;
            return false;
        }

        surface = hitInfo.transform;
        return true;
    }
}

public static class JsonHelper
{
    public static bool TryGetValue(this JsonData inData, string key, out string outData)
    {
        outData = null;

        if (inData.Count == 0)
        {
            return false;
        }

        if (!inData.Keys.Contains(key))
        {
            return false;
        }

        outData = inData[key].ToString();
        return true;
    }
}
