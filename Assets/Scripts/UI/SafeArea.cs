using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Insets this RectTransform to Screen.safeArea so its children clear notches, Dynamic Island,
    /// punch-holes and gesture bars. UI-only: nothing here ever touches a gameplay-world object.
    /// Re-applies whenever the safe area or orientation actually changes (device rotation, foldable
    /// state) rather than every frame, so steady-state cost is one Rect/enum comparison per frame and
    /// no layout rebuild.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;

        private RectTransform _rect;
        private Rect _lastSafeArea;
        private ScreenOrientation _lastOrientation;
        private bool _applied;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            _applied = false;
            Apply();
        }

        private void Update()
        {
            if (!_applied || Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastOrientation = Screen.orientation;
            _applied = true;

            Rect safeArea = _lastSafeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if (!applyLeft) anchorMin.x = 0f;
            if (!applyRight) anchorMax.x = 1f;
            if (!applyBottom) anchorMin.y = 0f;
            if (!applyTop) anchorMax.y = 1f;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
        }
    }
}
