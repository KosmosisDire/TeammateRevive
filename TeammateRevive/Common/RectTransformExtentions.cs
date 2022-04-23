using UnityEngine;
using UnityEngine.UI;

///<summary>
///collection of methods that simplify the use of RectTransform
///</summary>
public static class RectTransformExtensions
{
    public static Vector2 GetLocalPivotInPixels(this RectTransform rt)
    {
        return new Vector2(rt.pivot.x * rt.rect.width, rt.pivot.y * rt.rect.height);
    }

    public static Vector2 GetLocalPivotFromTopLeftInPixels(this RectTransform rt, bool negateY = false)
    {
        return rt.GetLocalPivotInPixels().MirrorY(rt, negateY);
    }

    ///<summary>
    /// Sets the absolute offset between the parent's top left corner, and it's own top left corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetTopLeftOffset(this RectTransform transform, float x, float y)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(x, y).MirrorY(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += finalOffset;
    }

    ///<summary>
    /// Sets the absolute offset between the parent's top left corner, and it's own top left corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetTopLeftOffset(this RectTransform transform, RectTransform.Axis direction, float distance)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(distance, distance).MirrorY(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += new Vector3(direction == RectTransform.Axis.Horizontal ? finalOffset.x : 0, direction == RectTransform.Axis.Vertical ? finalOffset.y : 0);
    }

    /// <summary>
    /// The absolute offset between the parent's top left corner, and it's own top left corner. Regardless of the pivot or anchors.
    /// </summary>
    public static Vector2 GetTopLeftOffset(this RectTransform transform)
    {
        RectTransform parent = transform.parent as RectTransform;
        Vector3 offset = (Vector3)parent.GetLocalPivotInPixels() + transform.localPosition - (Vector3)transform.GetLocalPivotInPixels().MirrorY(transform) - new Vector3(0, parent.rect.height, 0);
        return offset.MultiplyComponent(new Vector2(1, -1));
    }

    /// <summary>
    /// The absolute offset between the parent's top left corner, and it's own bottom right corner. Regardless of the pivot or anchors.
    /// </summary>
    public static Vector2 GetBottomRightOffset(this RectTransform transform)
    {
        RectTransform parent = transform.parent as RectTransform;
        Vector3 offset = (Vector3)parent.GetLocalPivotInPixels() + transform.localPosition - (Vector3)transform.GetLocalPivotInPixels().MirrorX(transform) - new Vector3(0, parent.rect.height, 0);
        return offset.MultiplyComponent(new Vector2(1, -1));
    }


    ///<summary>
    /// Sets the absolute offset between the parent's bottom right corner, and it's own bottom right corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetBottomRightOffset(this RectTransform transform, float x, float y)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(x, y).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += finalOffset;
    }

    ///<summary>
    /// Sets the absolute offset between the parent's bottom right corner, and it's own bottom right corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetBottomRightOffset(this RectTransform transform, RectTransform.Axis direction, float distance)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(distance, distance).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += new Vector3(direction == RectTransform.Axis.Horizontal ? finalOffset.x : 0, direction == RectTransform.Axis.Vertical ? finalOffset.y : 0);
    }

    /// <summary>
    /// The absolute offset between the parent's top left corner, and it's own top right corner. Regardless of the pivot or anchors.
    /// </summary>
    public static Vector2 GetTopRightOffset(this RectTransform transform)
    {
        RectTransform parent = transform.parent as RectTransform;
        Vector3 offset = (Vector3)parent.GetLocalPivotInPixels() + transform.localPosition - (Vector3)transform.GetLocalPivotInPixels().MirrorX(transform) - new Vector3(parent.rect.width, 0, 0);
        return offset.MultiplyComponent(new Vector2(1, -1));
    }

    /// <summary>
    /// Sets the absolute offset between the parent's top right corner, and it's own top right corner. Regardless of the pivot or anchors. Preserves the width and height.
    /// </summary>
    public static void SetTopRightOffset(this RectTransform transform, float x, float y)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(x, y).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += finalOffset;
    }

    ///<summary>
    /// Sets the absolute offset between the parent's top right corner, and it's own top right corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetTopRightOffset(this RectTransform transform, RectTransform.Axis direction, float distance)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(distance, distance).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += new Vector3(direction == RectTransform.Axis.Horizontal ? finalOffset.x : 0, direction == RectTransform.Axis.Vertical ? finalOffset.y : 0);
    }

    /// <summary>
    /// The absolute offset between the parent's top left corner, and it's own bottom left corner. Regardless of the pivot or anchors.
    /// </summary>
    public static Vector2 GetBottomLeftOffset(this RectTransform transform)
    {
        RectTransform parent = transform.parent as RectTransform;
        Vector3 offset = (Vector3)parent.GetLocalPivotInPixels() + transform.localPosition - (Vector3)transform.GetLocalPivotInPixels().MirrorY(transform) - new Vector3(0, parent.rect.height, 0);
        return offset.MultiplyComponent(new Vector2(1, -1));
    }

    /// <summary>
    /// Sets the absolute offset between the parent's bottom left corner, and it's own bottom left corner. Regardless of the pivot or anchors. Preserves the width and height.
    /// </summary>
    public static void SetBottomLeftOffset(this RectTransform transform, float x, float y)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(x, y).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += finalOffset;
    }

    ///<summary>
    /// Sets the absolute offset between the parent's bottom left corner, and it's own bottom left corner. Regardless of the pivot or anchors. Preserves the width and height.
    ///</summary>
    public static void SetBottomLeftOffset(this RectTransform transform, RectTransform.Axis direction, float distance)
    {
        RectTransform parent = transform.parent as RectTransform;

        Vector3 reorientedTarget = new Vector2(distance, distance).MirrorX(parent, false);
        Vector3 parentPivotToTarget = reorientedTarget - (Vector3)parent.GetLocalPivotInPixels();
        Vector3 localPivotToTarget = parentPivotToTarget - transform.localPosition;
        Vector3 finalOffset = localPivotToTarget + (Vector3)transform.GetLocalPivotFromTopLeftInPixels(true);
        transform.localPosition += new Vector3(direction == RectTransform.Axis.Horizontal ? finalOffset.x : 0, direction == RectTransform.Axis.Vertical ? finalOffset.y : 0);
    }

    public static Vector2 GetWorldSpaceCenter(this RectTransform transform)
    {
        return transform.TransformPoint(transform.rect.center);
    }

    public static Vector2 GetWorldSpaceSize(this RectTransform transform)
    {
        return transform.TransformVector(transform.rect.size);
    }

    public static void SetWidthToAnchor(this RectTransform transform, float width)
    {
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    public static void SetHeightToAnchor(this RectTransform transform, float height)
    {
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public static void SetSizeToAnchor(this RectTransform transform, float width, float height)
    {
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public static void SetWidthToParent(this RectTransform transform, float width)
    {
        transform.anchorMin = new Vector2(0, transform.anchorMin.y);
        transform.anchorMax = new Vector2(1, transform.anchorMax.y);
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    public static void SetHeightToParent(this RectTransform transform, float height)
    {
        transform.anchorMin = new Vector2(transform.anchorMin.x, 0);
        transform.anchorMax = new Vector2(transform.anchorMax.x, 1);
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public static void SetSizeToParent(this RectTransform transform, float width, float height)
    {
        transform.SetWidthToParent(width);
        transform.SetHeightToParent(height);
    }

    public static void SetWidthAbsolute(this RectTransform transform, int width)
    {
        SetWidthToParent(transform, width/transform.GetParentSize().x);
    }

    public static void SetHeightAbsolute(this RectTransform transform, int height)
    {
        SetHeightToParent(transform, height/transform.GetParentSize().y);
    }

    public static void SetSizeAbsolute(this RectTransform transform, int width, int height)
    {
        var size = new Vector2(width, height).DivideComponent(transform.GetParentSize());
        SetSizeToParent(transform, size.x, size.y);
    }

}