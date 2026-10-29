using System;
using OnePercentBattery.Save;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Short, mobile-first visual walkthrough shown to new players on first launch and replayable from
    /// Settings (same controller for both). Each page is a tiny looping demo built from the game's own
    /// sprites/colors (mascot, obstacle, pit, Energy, Shield, Magnet, coins) with one title and one
    /// instruction line. The demos are purely UI-driven (unscaled time) - no gameplay systems are used.
    /// Supports Next / Back / Skip / Finish; completion is persisted via SaveSystem.
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text stepIndicatorText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text headlineText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextButtonLabel;

        [Header("Game sprites reused by the demos")]
        [SerializeField] private Sprite roundedSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite mouthSprite;
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Sprite triangleSprite;

        public event Action TutorialCompleted;

        private const int PageCount = 7;
        private const float DemoW = 680f;
        private const float DemoH = 640f;
        private const float GroundY = 190f;
        private const float MascotSize = 120f;
        private const float MascotX = 190f;
        private const float MascotRestY = GroundY + MascotSize * 1.07f * 0.5f;

        // Colors taken from the game (ShopCatalog default skin, prefabs, ZoneCatalog CITY).
        private static readonly Color CityBg = new Color(0.10f, 0.12f, 0.16f);
        private static readonly Color Skyline = new Color(0.13f, 0.14f, 0.18f);
        private static readonly Color GroundCol = new Color(0.09f, 0.10f, 0.13f);
        private static readonly Color BodyGreen = new Color(0.7f, 0.93f, 0.38f);
        private static readonly Color Rim = new Color(0.7f, 0.72f, 0.76f);
        private static readonly Color Face = new Color(0.07f, 0.11f, 0.12f);
        private static readonly Color EnergyCol = new Color(0.25f, 0.85f, 0.35f);
        private static readonly Color ShieldCol = new Color(0.62f, 0.38f, 0.88f);
        private static readonly Color ShieldRingCol = new Color(0.45f, 0.85f, 1f, 0.55f);
        private static readonly Color MagnetCol = new Color(0.95f, 0.25f, 0.25f);
        private static readonly Color CoinCol = new Color(0.95f, 0.78f, 0.25f);
        private static readonly Color ObstacleCol = new Color(0.9f, 0.35f, 0.35f);

        private static readonly string[] Titles =
        {
            "TAP TO JUMP", "AVOID OBSTACLES", "DON'T FALL", "ENERGY RESTORES BATTERY",
            "SHIELD BLOCKS A HIT", "MAGNET PULLS COINS", "COLLECT COINS FOR COSMETICS"
        };

        private static readonly string[] Lines =
        {
            "HOLD FOR A HIGHER JUMP", "HITS DRAIN YOUR BATTERY", "PITS END THE RUN", "+15% BATTERY",
            "PROTECTS YOU FOR 5 SECONDS", "NEARBY COINS FLY TO YOU", "SPEND THEM IN THE SHOP"
        };

        private int _currentPage;
        private RectTransform _demo;
        private RectTransform _stage;
        private float _t;

        // Per-page demo objects
        private Mascot _mascot;
        private RectTransform _obstacle, _pickup, _warn, _finger, _burst, _shopCard, _barFill;
        private Image _obstacleImg, _pickupImg, _ringImg, _fingerFill, _burstImg, _barImg;
        private Text _label, _floatText, _counter;
        private RectTransform[] _coins;
        private RectTransform[] _dashes;
        private RectTransform _groundL, _groundR;

        private sealed class Mascot
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public RectTransform Charge;
            public Image ChargeImg;
        }

        // ---------------------------------------------------------------- public API (unchanged)

        public void Show(int startPage = 0)
        {
            _currentPage = Mathf.Clamp(startPage, 0, PageCount - 1);
            UpdatePageUI();
            if (root != null) root.SetActive(true);
            else gameObject.SetActive(true);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void OnNextClicked()
        {
            if (_currentPage >= PageCount - 1)
            {
                CompleteTutorial();
            }
            else
            {
                _currentPage++;
                UpdatePageUI();
            }
        }

        public void OnBackClicked()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdatePageUI();
            }
        }

        public void OnSkipClicked()
        {
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            SaveSystem.Data.hasCompletedTutorial = true;
            SaveSystem.Save();
            Close();
            TutorialCompleted?.Invoke();
        }

        private void UpdatePageUI()
        {
            int pageIndex = Mathf.Clamp(_currentPage, 0, PageCount - 1);

            if (stepIndicatorText != null) stepIndicatorText.text = $"{pageIndex + 1} / {PageCount}";
            if (titleText != null)
            {
                titleText.resizeTextForBestFit = true;
                titleText.resizeTextMinSize = 24;
                titleText.resizeTextMaxSize = 38;
                titleText.text = Titles[pageIndex];
            }
            if (headlineText != null) headlineText.text = Lines[pageIndex];
            if (bodyText != null) bodyText.gameObject.SetActive(false); // text-light: visuals carry the lesson

            if (backButton != null) backButton.gameObject.SetActive(pageIndex > 0);
            bool isLastPage = pageIndex >= PageCount - 1;
            if (skipButton != null) skipButton.gameObject.SetActive(!isLastPage);
            if (nextButtonLabel != null) nextButtonLabel.text = isLastPage ? "LET'S RUN!" : "NEXT";

            BuildStage(pageIndex);
        }

        // ---------------------------------------------------------------- demo plumbing

        private void Update()
        {
            if (_stage == null) return;
            _t += Time.unscaledDeltaTime;
            Tick(_currentPage, _t);
        }

        private void EnsureDemo()
        {
            if (_demo != null) return;
            Transform card = titleText != null ? titleText.transform.parent : transform;
            var go = new GameObject("DemoArea", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            _demo = go.GetComponent<RectTransform>();
            _demo.SetParent(card, false);
            _demo.anchorMin = _demo.anchorMax = new Vector2(0.5f, 1f);
            _demo.pivot = new Vector2(0.5f, 1f);
            _demo.anchoredPosition = new Vector2(0f, -200f);
            _demo.sizeDelta = new Vector2(DemoW, DemoH);
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = CityBg;
            img.raycastTarget = false;
        }

        private void BuildStage(int page)
        {
            EnsureDemo();
            if (_stage != null) Destroy(_stage.gameObject);
            var go = new GameObject("Stage", typeof(RectTransform));
            _stage = go.GetComponent<RectTransform>();
            _stage.SetParent(_demo, false);
            _stage.anchorMin = Vector2.zero;
            _stage.anchorMax = Vector2.one;
            _stage.offsetMin = _stage.offsetMax = Vector2.zero;
            _t = 0f;

            _mascot = null; _obstacle = _pickup = _warn = _finger = _burst = _shopCard = _barFill = null;
            _obstacleImg = _pickupImg = _ringImg = _fingerFill = _burstImg = _barImg = null;
            _label = _floatText = _counter = null; _coins = null; _dashes = null; _groundL = _groundR = null;

            // Skyline silhouette (flat blocks, like the City zone backdrop)
            float[] bx = { 20, 120, 230, 330, 450, 560 };
            float[] bw = { 90, 80, 100, 90, 110, 90 };
            float[] bh = { 150, 110, 190, 130, 170, 120 };
            for (int i = 0; i < bx.Length; i++)
                Box(_stage, "Skyline", Skyline, new Vector2(bw[i], bh[i]), new Vector2(bx[i], GroundY + bh[i] * 0.5f));

            bool pit = page == 2;
            if (!pit)
            {
                Box(_stage, "Ground", GroundCol, new Vector2(DemoW, GroundY), new Vector2(DemoW * 0.5f, GroundY * 0.5f));
                _dashes = new RectTransform[8];
                for (int i = 0; i < _dashes.Length; i++)
                    _dashes[i] = Box(_stage, "Dash", Rim * 0.6f, new Vector2(50f, 8f), new Vector2(0f, GroundY - 24f));
            }

            switch (page)
            {
                case 0:
                    _mascot = MakeMascot(_stage, 0.05f);
                    _finger = Pic(_stage, ringSprite, new Color(1f, 1f, 1f, 0.55f), new Vector2(100f, 100f), new Vector2(540f, GroundY + 110f)).rectTransform;
                    _fingerFill = Pic(_finger, circleSprite, new Color(1f, 1f, 1f, 0f), new Vector2(60f, 60f), Vector2.zero);
                    _label = Txt(_stage, "TAP", 34, Color.white, new Vector2(540f, GroundY + 30f));
                    break;
                case 1:
                    _barFill = MakeBar(_stage, out _barImg);
                    _mascot = MakeMascot(_stage, 0.05f);
                    _obstacle = MakeObstacle(_stage, out _obstacleImg);
                    _warn = MakeWarn(_stage);
                    break;
                case 2:
                    _mascot = MakeMascot(_stage, 0.05f);
                    _groundL = Box(_stage, "GroundL", GroundCol, new Vector2(330f, GroundY), new Vector2(165f, GroundY * 0.5f));
                    _groundR = Box(_stage, "GroundR", GroundCol, new Vector2(200f, GroundY), new Vector2(580f, GroundY * 0.5f));
                    _warn = MakeWarn(_stage);
                    break;
                case 3:
                    _barFill = MakeBar(_stage, out _barImg);
                    _mascot = MakeMascot(_stage, 0.15f);
                    _pickup = MakePickup(_stage, EnergyCol, out _pickupImg);
                    _burst = Pic(_stage, glowSprite, new Color(EnergyCol.r, EnergyCol.g, EnergyCol.b, 0f), new Vector2(200f, 200f), Vector2.zero).rectTransform;
                    _burstImg = _burst.GetComponent<Image>();
                    _floatText = Txt(_stage, "+15%", 40, EnergyCol, Vector2.zero);
                    break;
                case 4:
                    _mascot = MakeMascot(_stage, 0.3f);
                    _ringImg = Pic(_mascot.Root, glowSprite, new Color(ShieldRingCol.r, ShieldRingCol.g, ShieldRingCol.b, 0f), Vector2.one * MascotSize * 2.2f, Vector2.zero);
                    _ringImg.transform.SetAsFirstSibling();
                    _pickup = MakePickup(_stage, ShieldCol, out _pickupImg);
                    _obstacle = MakeObstacle(_stage, out _obstacleImg);
                    _obstacle.gameObject.SetActive(false);
                    break;
                case 5:
                    _mascot = MakeMascot(_stage, 0.3f);
                    _ringImg = Pic(_mascot.Root, ringSprite, new Color(MagnetCol.r, MagnetCol.g, MagnetCol.b, 0f), Vector2.one * MascotSize * 2.4f, Vector2.zero);
                    _ringImg.transform.SetAsFirstSibling();
                    _pickup = MakePickup(_stage, MagnetCol, out _pickupImg);
                    MakeCoins(5);
                    MakeCounter();
                    break;
                default:
                    _mascot = MakeMascot(_stage, 0.3f);
                    MakeCoins(4);
                    MakeCounter();
                    _shopCard = MakeShopCard(_stage);
                    break;
            }
        }

        // ---------------------------------------------------------------- per-page animation

        private void Tick(int page, float t)
        {
            switch (page)
            {
                case 0: TickJump(t % 3.4f); break;
                case 1: TickObstacle(t % 3.8f); break;
                case 2: TickPit(t % 3.2f); break;
                case 3: TickEnergy(t % 3.6f); break;
                case 4: TickShield(t % 4.6f); break;
                case 5: TickMagnet(t % 4.4f); break;
                default: TickCoins(t % 4.6f); break;
            }
            if (_dashes != null)
            {
                for (int i = 0; i < _dashes.Length; i++)
                {
                    float x = Mathf.Repeat(i * 100f - t * 260f, 800f) - 60f;
                    _dashes[i].anchoredPosition = new Vector2(x, GroundY - 24f);
                }
            }
        }

        private void TickJump(float t)
        {
            float h = 0f;
            bool pressed = false;
            string label = "TAP";
            if (t >= 0.3f && t < 1.2f)
            {
                float u = (t - 0.3f) / 0.9f;
                h = 70f * 4f * u * (1f - u);
                pressed = t < 0.42f;
            }
            else if (t >= 1.2f)
            {
                label = "HOLD";
                if (t >= 1.7f && t < 2.9f)
                {
                    float u = (t - 1.7f) / 1.2f;
                    h = 195f * 4f * u * (1f - u);
                }
                pressed = t >= 1.7f && t < 2.0f;
            }
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY + h);
            _label.text = label;
            _finger.localScale = Vector3.one * (pressed ? 0.85f : 1f);
            _fingerFill.color = new Color(1f, 1f, 1f, pressed ? 0.9f : 0f);
        }

        private void TickObstacle(float t)
        {
            float x = Mathf.Lerp(640f, MascotX + 95f, Mathf.Clamp01(t / 1.6f));
            bool alive = t < 2.4f;
            _obstacle.gameObject.SetActive(alive);
            _obstacle.anchoredPosition = new Vector2(x, GroundY + 55f);
            _obstacleImg.color = ObstacleCol;
            bool warn = alive && t < 1.6f;
            _warn.gameObject.SetActive(warn);
            _warn.anchoredPosition = new Vector2(x, GroundY + 170f + Mathf.Sin(t * 14f) * 6f);
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY);
            bool hit = t >= 1.6f && t < 2.4f;
            _mascot.Group.alpha = hit && Mathf.FloorToInt(t * 8f) % 2 == 0 ? 0.3f : 1f;
            SetBar(Mathf.Lerp(0.6f, 0.4f, Mathf.Clamp01((t - 1.6f) / 0.4f)));
        }

        private void TickPit(float t)
        {
            float mx = 110f + 190f * Mathf.Min(t, 1.5f) + (t > 1.5f ? 60f * (t - 1.5f) : 0f);
            float y = MascotRestY;
            if (t > 1.5f) y -= 1300f * 0.5f * (t - 1.5f) * (t - 1.5f);
            _mascot.Root.anchoredPosition = new Vector2(mx, y);
            _mascot.Group.alpha = t > 2.7f ? 0f : 1f;
            _warn.anchoredPosition = new Vector2(405f, GroundY + 130f + Mathf.Sin(t * 14f) * 6f);
        }

        private void TickEnergy(float t)
        {
            float x = Mathf.Lerp(650f, MascotX + 100f, Mathf.Clamp01(t / 1.4f));
            bool collected = t >= 1.4f;
            _pickup.gameObject.SetActive(!collected);
            _pickup.anchoredPosition = new Vector2(x, GroundY + 100f + Mathf.Sin(t * 6f) * 10f);
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY);
            float charge = Mathf.Lerp(0.15f, 0.30f, Mathf.Clamp01((t - 1.4f) / 0.8f));
            SetMascotCharge(_mascot, charge);
            SetBar(charge);
            float b = Mathf.Clamp01((t - 1.4f) / 0.5f);
            _burst.anchoredPosition = _mascot.Root.anchoredPosition;
            _burst.localScale = Vector3.one * (0.6f + b * 1.4f);
            _burstImg.color = new Color(EnergyCol.r, EnergyCol.g, EnergyCol.b, collected ? 0.6f * (1f - b) : 0f);
            float f = Mathf.Clamp01((t - 1.4f) / 1.0f);
            _floatText.rectTransform.anchoredPosition = new Vector2(MascotX, MascotRestY + 100f + f * 60f);
            _floatText.color = new Color(EnergyCol.r, EnergyCol.g, EnergyCol.b, collected ? 1f - f : 0f);
        }

        private void TickShield(float t)
        {
            float px = Mathf.Lerp(650f, MascotX + 100f, Mathf.Clamp01(t / 1.2f));
            bool collected = t >= 1.2f;
            _pickup.gameObject.SetActive(!collected);
            _pickup.anchoredPosition = new Vector2(px, GroundY + 100f + Mathf.Sin(t * 6f) * 10f);
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY);
            bool ringOn = t >= 1.2f && t < 4.0f;
            float pulse = t >= 3.0f && t < 3.3f ? 1.15f : 1f;
            _ringImg.color = new Color(ShieldRingCol.r, ShieldRingCol.g, ShieldRingCol.b, ringOn ? ShieldRingCol.a : 0f);
            _ringImg.rectTransform.localScale = Vector3.one * pulse;
            bool obsOn = t >= 1.8f && t < 3.4f;
            _obstacle.gameObject.SetActive(obsOn);
            if (obsOn)
            {
                float ox = Mathf.Lerp(640f, MascotX + 95f, Mathf.Clamp01((t - 1.8f) / 1.2f));
                float fade = t > 3.0f ? 1f - (t - 3.0f) / 0.4f : 1f;
                _obstacle.anchoredPosition = new Vector2(ox, GroundY + 55f);
                _obstacleImg.color = new Color(ObstacleCol.r, ObstacleCol.g, ObstacleCol.b, fade);
            }
        }

        private void TickMagnet(float t)
        {
            float px = Mathf.Lerp(650f, MascotX + 100f, Mathf.Clamp01(t / 1.2f));
            bool collected = t >= 1.2f;
            _pickup.gameObject.SetActive(!collected);
            _pickup.anchoredPosition = new Vector2(px, GroundY + 100f + Mathf.Sin(t * 6f) * 10f);
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY);
            float pulse = 1f + Mathf.Sin(t * 8f) * 0.06f;
            _ringImg.color = new Color(MagnetCol.r, MagnetCol.g, MagnetCol.b, collected ? 0.6f : 0f);
            _ringImg.rectTransform.localScale = Vector3.one * pulse;

            Vector2[] start = { new Vector2(440, GroundY + 130), new Vector2(520, GroundY + 70), new Vector2(570, GroundY + 190), new Vector2(480, GroundY + 230), new Vector2(620, GroundY + 120) };
            int got = 0;
            for (int i = 0; i < _coins.Length; i++)
            {
                float k = (t - 1.4f - i * 0.12f) / 0.7f;
                if (k >= 1f) { _coins[i].gameObject.SetActive(false); got++; continue; }
                _coins[i].gameObject.SetActive(true);
                Vector2 from = start[i] + Vector2.left * Mathf.Min(t, 1.4f) * 30f;
                Vector2 pos = k <= 0f ? from : Vector2.Lerp(from, _mascot.Root.anchoredPosition, k * k);
                _coins[i].anchoredPosition = pos;
            }
            _counter.text = "x " + got;
        }

        private void TickCoins(float t)
        {
            _mascot.Root.anchoredPosition = new Vector2(MascotX, MascotRestY);
            int got = 0;
            for (int i = 0; i < _coins.Length; i++)
            {
                float x = 560f + i * 70f - 200f * t;
                bool taken = x <= MascotX + 40f;
                if (taken) got++;
                _coins[i].gameObject.SetActive(!taken);
                _coins[i].anchoredPosition = new Vector2(x, MascotRestY + Mathf.Sin(t * 8f + i) * 6f);
            }
            _counter.text = "x " + got;
            bool shop = t >= 2.9f;
            _shopCard.gameObject.SetActive(shop);
            _shopCard.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.04f);
        }

        // ---------------------------------------------------------------- builders

        private RectTransform Box(Transform parent, string name, Color c, Vector2 size, Vector2 pos)
        {
            var img = Pic(parent, null, c, size, pos);
            img.name = name;
            return img.rectTransform;
        }

        private Image Pic(Transform parent, Sprite sprite, Color c, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Rect", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            bool inStage = parent == _stage || parent == _demo;
            rt.anchorMin = rt.anchorMax = inStage ? Vector2.zero : new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = c;
            img.raycastTarget = false;
            if (sprite == roundedSprite && sprite != null) img.type = Image.Type.Sliced;
            return img;
        }

        private Text Txt(Transform parent, string s, int size, Color c, Vector2 pos)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, size + 20f);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = headlineText != null ? headlineText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = c;
            t.text = s;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private Mascot MakeMascot(Transform parent, float charge)
        {
            var m = new Mascot();
            var rim = Pic(parent, roundedSprite, Rim, Vector2.one * MascotSize * 1.07f, new Vector2(MascotX, MascotRestY));
            rim.name = "Mascot";
            m.Root = rim.rectTransform;
            m.Group = rim.gameObject.AddComponent<CanvasGroup>();
            var cap = Pic(m.Root, roundedSprite, Rim, new Vector2(MascotSize * 0.38f, MascotSize * 0.15f), new Vector2(0f, MascotSize * 0.56f));
            cap.name = "TerminalCap";
            Pic(m.Root, roundedSprite, BodyGreen, Vector2.one * MascotSize, Vector2.zero).name = "Body";
            float s = MascotSize;
            Pic(m.Root, circleSprite, Face, Vector2.one * s * 0.16f, new Vector2(-s * 0.224f, s * 0.212f));
            Pic(m.Root, circleSprite, Face, Vector2.one * s * 0.16f, new Vector2(s * 0.224f, s * 0.212f));
            Pic(m.Root, mouthSprite, Face, new Vector2(s * 0.5f, s * 0.25f), new Vector2(0f, -s * 0.094f));
            m.ChargeImg = Pic(m.Root, roundedSprite, Color.white, new Vector2(s * 0.3f, 4f), Vector2.zero);
            m.ChargeImg.name = "Charge";
            m.Charge = m.ChargeImg.rectTransform;
            SetMascotCharge(m, charge);
            return m;
        }

        private static void SetMascotCharge(Mascot m, float frac)
        {
            // Mirrors PlayerVisual.UpdateInnerCharge: bottom-up capsule, never fully empty.
            float chamber = MascotSize * 0.6f;
            float h = Mathf.Max(0.02f, Mathf.Clamp01(frac)) * chamber;
            m.Charge.sizeDelta = new Vector2(MascotSize * 0.3f, Mathf.Max(h, 4f));
            m.Charge.anchoredPosition = new Vector2(0f, -MascotSize * 0.3f - 0f + Mathf.Max(h, 4f) * 0.5f - 2f);
            Color tier = frac < 0.4f ? new Color(0.9f, 0.25f, 0.25f) : BodyGreen;
            Color c = Color.Lerp(tier, Color.white, 0.4f);
            c.a = 0.95f;
            m.ChargeImg.color = c;
        }

        private RectTransform MakeObstacle(Transform parent, out Image img)
        {
            img = Pic(parent, roundedSprite, ObstacleCol, new Vector2(70f, 110f), new Vector2(640f, GroundY + 55f));
            img.name = "Obstacle";
            return img.rectTransform;
        }

        private RectTransform MakePickup(Transform parent, Color c, out Image img)
        {
            img = Pic(parent, circleSprite, c, new Vector2(72f, 72f), new Vector2(650f, GroundY + 100f));
            img.name = "Pickup";
            return img.rectTransform;
        }

        private RectTransform MakeWarn(Transform parent)
        {
            var tri = Pic(parent, triangleSprite, new Color(0.95f, 0.78f, 0.25f), new Vector2(64f, 56f), new Vector2(640f, GroundY + 170f));
            tri.name = "Warn";
            var bang = Txt(tri.rectTransform, "!", 34, Face, new Vector2(32f, 20f));
            bang.rectTransform.anchorMin = bang.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bang.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            return tri.rectTransform;
        }

        private RectTransform MakeBar(Transform parent, out Image fillImg)
        {
            var track = Pic(parent, roundedSprite, new Color(0.04f, 0.06f, 0.07f), new Vector2(280f, 34f), new Vector2(200f, DemoH - 50f));
            fillImg = Pic(track.transform, roundedSprite, BodyGreen, new Vector2(272f, 26f), Vector2.zero);
            var rt = fillImg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(4f, 0f);
            return rt;
        }

        private void SetBar(float frac)
        {
            if (_barFill == null) return;
            frac = Mathf.Clamp01(frac);
            _barFill.sizeDelta = new Vector2(Mathf.Max(8f, 272f * frac), 26f);
            _barImg.color = frac < 0.4f ? new Color(0.9f, 0.25f, 0.25f) : BodyGreen;
        }

        private void MakeCoins(int n)
        {
            _coins = new RectTransform[n];
            for (int i = 0; i < n; i++)
            {
                var c = Pic(_stage, circleSprite, CoinCol, new Vector2(44f, 44f), new Vector2(600f, GroundY + 100f));
                c.name = "Coin";
                _coins[i] = c.rectTransform;
            }
        }

        private void MakeCounter()
        {
            Pic(_stage, circleSprite, CoinCol, new Vector2(36f, 36f), new Vector2(DemoW - 130f, DemoH - 45f)).name = "CoinIcon";
            _counter = Txt(_stage, "x 0", 36, Color.white, new Vector2(DemoW - 60f, DemoH - 45f));
            _counter.rectTransform.sizeDelta = new Vector2(100f, 56f);
        }

        private RectTransform MakeShopCard(Transform parent)
        {
            var card = Pic(parent, roundedSprite, new Color(0.08f, 0.11f, 0.115f), new Vector2(260f, 130f), new Vector2(500f, 450f));
            card.name = "ShopCue";
            var title = Txt(card.rectTransform, "SHOP", 30, CoinCol, Vector2.zero);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            Color[] skins = { new Color(0.80f, 0.50f, 0.30f), new Color(0.2f, 0.9f, 0.9f), new Color(0.75f, 0.3f, 0.95f) };
            for (int i = 0; i < skins.Length; i++)
                Pic(card.rectTransform, roundedSprite, skins[i], new Vector2(44f, 44f), new Vector2((i - 1) * 64f, -26f));
            card.gameObject.SetActive(false);
            return card.rectTransform;
        }
    }
}
