using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Keeps the Home-screen mascot's Body tinted to match the currently equipped skin - reads the same
    /// CosmeticDefinition.AccentColor PlayerVisual uses in gameplay, so there is no separate hardcoded
    /// color table that could fall out of sync with it. A trail-only change never affects this:
    /// EquippedSkin is always the equipped SKIN regardless of what trail is equipped alongside it. Home
    /// intentionally has no glow or InnerCharge (removed per design) - just the mascot shell/face.
    /// </summary>
    public sealed class HomeMascotController : MonoBehaviour
    {
        [SerializeField] private Image bodyImage;

        private void OnEnable()
        {
            // MascotDecoration lives under the Home root, which MainMenuController disables/re-enables
            // on Hide()/Show() - OnEnable already re-fires on every return to Home, so no need to also
            // hook GameEvents.ReturnedToMenu here.
            ShopManager.Instance.CollectionChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            // During Play Mode teardown, ShopManager's own GameObject can already be gone by the time
            // this disables (both are torn down in the same scene-unload pass, in unspecified order) -
            // Instance's FindAnyObjectByType fallback then returns null, and unsubscribing from a null
            // reference threw a NullReferenceException here every time Play Mode stopped.
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.CollectionChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            Color accent = ShopManager.Instance.EquippedSkin.AccentColor;

            if (bodyImage != null)
            {
                // Home always presents the character at its resting/happy state - it never reads a
                // battery tier, so unlike gameplay's PlayerVisual (which swaps in a fixed danger color
                // below 40%), the body always shows the equipped skin's own color directly.
                bodyImage.color = accent;
            }
        }
    }
}
