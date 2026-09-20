using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Two-color gradient tint for a UI Graphic (Image/Text) - uGUI has no built-in gradient, and the
    /// source game's premium/ad-reward actions (Revive, Double Coins, Retry, Story) all rely on one.
    /// </summary>
    [AddComponentMenu("UI/Effects/UI Gradient")]
    public sealed class UIGradient : BaseMeshEffect
    {
        public enum Direction { Horizontal, Vertical }

        [SerializeField] private Color colorA = Color.white;
        [SerializeField] private Color colorB = Color.black;
        [SerializeField] private Direction direction = Direction.Horizontal;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            var vertex = default(UIVertex);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minX = Mathf.Min(minX, vertex.position.x);
                maxX = Mathf.Max(maxX, vertex.position.x);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            float width = Mathf.Max(0.0001f, maxX - minX);
            float height = Mathf.Max(0.0001f, maxY - minY);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = direction == Direction.Horizontal
                    ? (vertex.position.x - minX) / width
                    : 1f - (vertex.position.y - minY) / height;
                vertex.color = Color.Lerp(colorA, colorB, t);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
