using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Preview-before-purchase modal for a locked cosmetic (skin, trail, or an Exclusive-tab item -
    /// which are just skins/trails from a curated list, see ShopCatalog.Exclusive), shown inside the
    /// existing Shop panel. One shared modal for all three: a Skin/Exclusive-skin shows a static
    /// tinted preview character (previewBody Image). A Trail/Exclusive-trail instead shows a real,
    /// off-screen preview rig - an actual SpriteRenderer character plus a TrailRenderer whose
    /// settings are copied directly from the real gameplay TrailRenderer - rendered by a dedicated
    /// camera into a RenderTexture and displayed via trailRenderPreview (a RawImage). This is the
    /// genuine cosmetic, not an approximation: the same material/width/lifetime as gameplay, just
    /// with this trail's own color, matching PlayerVisual.ApplyTrail's own start/end color logic.
    /// Purely a visual/read-only look - it never touches SaveData (no equip, no unlock, no spend)
    /// until the player explicitly presses Buy, so Cancel has nothing to "undo": the rest of the UI
    /// (grid checkmarks, equipped skin/trail) was never changed while this was open.
    /// </summary>
    public sealed class SkinPreviewController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image previewBody;
        [SerializeField] private Text nameText;
        [SerializeField] private Text priceText;
        [SerializeField] private Text buyButtonLabel;
        [SerializeField] private GameObject insufficientFeedback;

        [Header("Trail preview (real off-screen rig -> RenderTexture)")]
        [Tooltip("Displays trailPreviewCamera's RenderTexture. Shown instead of previewBody only while previewing a Trail.")]
        [SerializeField] private RawImage trailRenderPreview;
        [Tooltip("Camera framing the off-screen preview rig, culling-masked to the CosmeticPreview layer only. Disabled except while a Trail preview is open, so it costs nothing the rest of the time.")]
        [SerializeField] private Camera trailPreviewCamera;
        [Tooltip("Root of the off-screen preview rig (Body/Eyes/Mouth/TrailAnchor) - animated side to side so its real TrailRenderer actually draws a trail.")]
        [SerializeField] private Transform trailPreviewRig;
        [SerializeField] private SpriteRenderer trailPreviewBody;
        [Tooltip("The preview rig's TrailRenderer - every non-color setting is copied once from the real gameplay TrailRenderer (see PlayerVisual's TrailAnchor) at build time, so it stays accurate if that ever changes.")]
        [SerializeField] private TrailRenderer trailPreviewTrailRenderer;
        [SerializeField] private float trailWorldAmplitude = 1.0f;
        [SerializeField] private float trailSpeed = 1.8f;

        private CosmeticDefinition _item;
        private bool _animatingTrail;
        private float _animTime;
        private float _rigBaseX;
        private bool _rigBaseCaptured;

        private void CaptureRigBaseX()
        {
            if (_rigBaseCaptured || trailPreviewRig == null) return;
            _rigBaseX = trailPreviewRig.position.x;
            _rigBaseCaptured = true;
        }

        public void Show(CosmeticDefinition item)
        {
            CaptureRigBaseX();
            _item = item;
            _animatingTrail = item.Category == CosmeticCategory.Trail;

            if (previewBody != null) previewBody.gameObject.SetActive(!_animatingTrail);
            if (trailRenderPreview != null) trailRenderPreview.gameObject.SetActive(_animatingTrail);

            if (_animatingTrail)
            {
                SetupTrailPreview(item);
            }
            else if (previewBody != null)
            {
                previewBody.color = item.AccentColor.a > 0f ? item.AccentColor : Color.gray;
            }

            nameText.text = item.DisplayName;
            priceText.text = item.CostCoins.ToString();
            if (buyButtonLabel != null) buyButtonLabel.text = "BUY";
            if (insufficientFeedback != null) insufficientFeedback.SetActive(false);
            root.SetActive(true);
        }

        /// <summary>Mirrors PlayerVisual.ApplyTrail's own start/end color logic exactly, so the
        /// preview's color behavior can never drift from what gameplay actually does.</summary>
        private void SetupTrailPreview(CosmeticDefinition trail)
        {
            // A trail doesn't recolor the body - show it in whatever skin is actually equipped, same
            // as gameplay would.
            if (trailPreviewBody != null) trailPreviewBody.color = ShopManager.Instance.EquippedSkin.AccentColor;
            if (trailPreviewCamera != null) trailPreviewCamera.enabled = true;

            if (trailPreviewTrailRenderer != null)
            {
                trailPreviewTrailRenderer.Clear();
                Color startColor = trail.AccentColor;
                Color endColor = startColor;
                endColor.a = 0f;
                trailPreviewTrailRenderer.startColor = startColor;
                trailPreviewTrailRenderer.endColor = endColor;
                trailPreviewTrailRenderer.emitting = true;
            }

            _animTime = 0f;
            if (trailPreviewRig != null)
            {
                var pos = trailPreviewRig.position;
                pos.x = _rigBaseX;
                trailPreviewRig.position = pos;
            }
        }

        private void Update()
        {
            if (!_animatingTrail || trailPreviewRig == null) return;

            _animTime += Time.unscaledDeltaTime;
            var pos = trailPreviewRig.position;
            pos.x = _rigBaseX + trailWorldAmplitude * Mathf.Sin(_animTime * trailSpeed);
            trailPreviewRig.position = pos;
        }

        public void OnBuyClicked()
        {
            if (_item == null) return;

            if (!ShopManager.Instance.TryPurchase(_item))
            {
                if (insufficientFeedback != null) insufficientFeedback.SetActive(true);
                return;
            }

            ShopManager.Instance.Equip(_item);
            Close();
        }

        public void OnCancelClicked() => Close();

        private void Close()
        {
            _item = null;
            if (_animatingTrail)
            {
                if (trailPreviewTrailRenderer != null)
                {
                    trailPreviewTrailRenderer.emitting = false;
                    trailPreviewTrailRenderer.Clear();
                }
                if (trailPreviewCamera != null) trailPreviewCamera.enabled = false;
            }
            _animatingTrail = false;
            root.SetActive(false);
        }
    }
}
