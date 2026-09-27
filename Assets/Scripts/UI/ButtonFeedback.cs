using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Lightweight press feedback for a UI Button: scales down while held, eases back on release, and
    /// briefly disables the button right after a click so a fast double-tap can't fire it twice. Purely
    /// additive - hooks the Button's existing OnClick alongside whatever listeners it already has,
    /// never replaces them, so no caller's click logic changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float releaseLerpSeconds = 0.12f;
        [SerializeField] private float reactivateAfterClickSeconds = 0.35f;

        private RectTransform _rect;
        private Button _button;
        private Vector3 _baseScale;
        private bool _pressed;
        private float _lerpTimer;
        private Coroutine _reactivateRoutine;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _baseScale = _rect.localScale;
            _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (_button == null || !isActiveAndEnabled) return;
            OnePercentBattery.Audio.AudioManager.Instance?.PlayUiClick();
            if (_reactivateRoutine != null) StopCoroutine(_reactivateRoutine);
            _reactivateRoutine = StartCoroutine(ReactivateAfterDelay());
        }

        private IEnumerator ReactivateAfterDelay()
        {
            _button.interactable = false;
            yield return new WaitForSecondsRealtime(reactivateAfterClickSeconds);
            _button.interactable = true;
            _reactivateRoutine = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            _rect.localScale = _baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            _lerpTimer = releaseLerpSeconds;
        }

        private void Update()
        {
            if (_pressed || _lerpTimer <= 0f) return;

            _lerpTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_lerpTimer / releaseLerpSeconds);
            _rect.localScale = Vector3.Lerp(_baseScale, _baseScale * pressedScale, t);
        }

        // A panel hiding mid-press/mid-cooldown (e.g. Retry ends the run before the reactivate delay
        // elapses) must not leave the button stuck scaled-down or non-interactable next time it shows.
        private void OnDisable()
        {
            _pressed = false;
            _lerpTimer = 0f;
            _rect.localScale = _baseScale;
            if (_reactivateRoutine != null)
            {
                StopCoroutine(_reactivateRoutine);
                _reactivateRoutine = null;
            }
            if (_button != null) _button.interactable = true;
        }
    }
}
