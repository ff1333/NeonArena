using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    [SerializeField] private Vector2 minimum = new Vector2(-20f, -12f);
    [SerializeField] private Vector2 maximum = new Vector2(20f, 12f);

    public Vector2 Minimum => new Vector2(
        Mathf.Min(minimum.x, maximum.x),
        Mathf.Min(minimum.y, maximum.y)
    );

    public Vector2 Maximum => new Vector2(
        Mathf.Max(minimum.x, maximum.x),
        Mathf.Max(minimum.y, maximum.y)
    );

    public Vector2 ClampPoint(Vector2 position, float padding)
    {
        Vector2 min = Minimum + Vector2.one * padding;
        Vector2 max = Maximum - Vector2.one * padding;

        if (min.x >max.x)
        {
            min.x =max.x =(Minimum.x + Maximum.x) * 0.5f;
        }
        if (min.y >max.y)
        {
            min.y =max.y =(Minimum.y + Maximum.y) * 0.5f;
        }
        return new Vector2(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y)
        );
    }

    public float EdgeProximity(Vector2 point)
    {
        Vector2 center = (Minimum + Maximum) * 0.5f;
        Vector2 half = (Maximum - Minimum) * 0.5f;
        if (half.x <= 0.001f || half.y <= 0.001f)
        {
            return 1f;
        }

        float normalizedX = 1f - Mathf.Abs(point.x - center.x) / half.x;
        float normalizedY = 1f - Mathf.Abs(point.y - center.y) / half.y;
        return 1f - Mathf.Clamp01(Mathf.Min(normalizedX, normalizedY));
    }

    public bool Contains(Vector2 point, float padding)
    {
        Vector2 min = Minimum + Vector2.one * padding;
        Vector2 max = Maximum - Vector2.one * padding;

        return point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;
    }

    private void OnDrawGizmosSelected()
    {
       Vector2 size = Maximum - Minimum;
        Vector2 center = (Maximum + Minimum) * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
}
