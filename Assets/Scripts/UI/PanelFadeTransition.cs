using System.Collections;
using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Quick, purely cosmetic fade-in for a panel's Show(). Callers keep calling
    /// gameObject.SetActive(true) exactly as before via Show() - this only eases the alpha from 0 to 1
    /// afterwards, so nothing gameplay-relevant is ever gated on it. Hiding stays a plain, instant
    /// SetActive(false) at every call site (unchanged) so late-arriving callbacks that guard on
    /// root.activeSelf (e.g. a rewarded-ad result after Retry/Home) keep working exactly as before.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelFadeTransition : MonoBehaviour
    {
        [SerializeField] private float fadeSeconds = 0.12f;

        private CanvasGroup _group;
        private Coroutine _routine;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        public void Show()
        {
            gameObject.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            float t = 0f;
            _group.alpha = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            _group.alpha = 1f;
            _routine = null;
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _group.alpha = 1f;
        }
    }
}
