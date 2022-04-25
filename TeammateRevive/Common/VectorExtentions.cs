using UnityEngine;
using UnityEngine.UI;

public static class VectorExtentions
{
    public static Vector2 MirrorY(this Vector2 v, RectTransform space, bool negateY = true)
    {
        return new Vector2(v.x, (negateY ? -1 : 1) * (space.rect.height - v.y));
    }

    public static Vector2 MirrorY(this Vector3 v, RectTransform space, bool negateY = true)
    {
        return new Vector2(v.x, (negateY ? -1 : 1) * (space.rect.height - v.y));
    }

    public static Vector2 MirrorX(this Vector2 v, RectTransform space, bool negateX = true)
    {
        return new Vector2((negateX ? -1 : 1) * (space.rect.width - v.x), v.y);
    }

    public static Vector2 MirrorX(this Vector3 v, RectTransform space, bool negateX = true)
    {
        return new Vector2((negateX ? -1 : 1) * (space.rect.width - v.x), v.y);
    }

    public static Vector2 MultiplyComponent (this Vector2 v, Vector2 other)
    {
        return new Vector2(v.x * other.x, v.y * other.y);
    }

    public static Vector3 MultiplyComponent (this Vector3 v, Vector3 other)
    {
        return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
    }

    public static Vector2 DivideComponent (this Vector2 v, Vector2 other)
    {
        return new Vector2(v.x / other.x, v.y / other.y);
    }

    public static Vector3 DivideComponent (this Vector3 v, Vector3 other)
    {
        return new Vector3(v.x / other.x, v.y / other.y, v.z / other.z);
    }

    public static Vector3 SetX(this Vector3 v, float x)
    {
        return new Vector3(x, v.y, v.z);
    }

    public static Vector3 SetY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    public static Vector3 SetZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, z);
    }
}