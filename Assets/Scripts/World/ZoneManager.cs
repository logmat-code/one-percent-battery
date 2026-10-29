using OnePercentBattery.Core;
using OnePercentBattery.Data;
using UnityEngine;

namespace OnePercentBattery.World
{
    /// <summary>Tracks which cosmetic zone the run is in (Desk, Rooftop, ... Galactic) and tints the camera
    /// background, the skyline silhouette and the sky backdrop to match.</summary>
    public sealed class ZoneManager : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float tintLerpSpeed = 2f;
        [Tooltip("World/Skyline's tile renderers - tinted per-zone alongside the camera background. Only visible while CurrentZone.SkylineTint's alpha is above 0 (CITY); fades to invisible everywhere else via the same lerp as the tint color itself.")]
        [SerializeField] private SpriteRenderer[] skylineRenderers;
        [Tooltip("World/Forest's tile renderers - same treatment as skylineRenderers, but for the FOREST zone's treeline. Only one of Skyline/Forest is ever meant to be visible at a time; the cross-fade between them at the CITY/FOREST boundary is the only intentional brief overlap.")]
        [SerializeField] private SpriteRenderer[] forestRenderers;
        [Tooltip("The camera-locked sky backdrop sprite. Its sprite fades from opaque (top) to transparent (bottom), so the transparent edge always blends seamlessly into the flat camera color beneath it.")]
        [SerializeField] private SpriteRenderer skyRenderer;
        [Tooltip("A fixed, deliberately lighter reference tone the sky's opaque end blends toward. Every zone's own BackgroundTint is already very dark (near-black in the later zones), so DARKENING it further (an earlier attempt) produced a color indistinguishable from black - blending toward something lighter instead is what actually makes the gradient visible.")]
        [SerializeField] private Color skyAtmosphereTint = new Color(0.28f, 0.30f, 0.38f);
        [Tooltip("How strongly the sky leans toward skyAtmosphereTint vs. the zone's own BackgroundTint - 0 = invisible (same as flat background), 1 = ignores the zone color entirely.")]
        [Range(0f, 1f)]
        [SerializeField] private float skyBlendAmount = 0.55f;

        [Header("Zone progression (stars/nebula)")]
        [Tooltip("Camera-locked, Tiled-mode star field sitting between Sky and Skyline. Its own texture has stars of varying baked brightness, so fading its alpha in per-zone naturally reveals only the brightest few first rather than a uniformly dim copy of the whole field.")]
        [SerializeField] private SpriteRenderer starsRenderer;
        [Tooltip("Camera-locked nebula cloud sitting between the star field and Skyline - stays invisible through the city zones and only becomes prominent from Stratosphere on, so Galactic's is a real late-run payoff.")]
        [SerializeField] private SpriteRenderer nebulaRenderer;
        [Tooltip("Ground tile renderers - tinted per-zone to give each environment a distinct platform feel.")]
        [SerializeField] private SpriteRenderer[] groundRenderers;
        [Header("Zone Floors (City, Forest, Stratosphere, Orbit, Galactic)")]
        [SerializeField] private Sprite[] zoneFloorSprites;

        public ZoneDefinition CurrentZone { get; private set; }

        private Color _targetColor;
        private Color _targetSkyTint;
        private Color _targetSkylineTint;
        private Color _targetForestTint;
        private float _targetStarAlpha;
        private float _targetNebulaAlpha;
        private Color _targetGroundTint;

        private void OnEnable()
        {
            GameEvents.DistanceChanged += HandleDistanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.DistanceChanged -= HandleDistanceChanged;
        }

        public void ResetZone()
        {
            CurrentZone = ZoneCatalog.Zones[0];
            _targetColor = CurrentZone.BackgroundTint;
            _targetSkyTint = SkyTintFor(_targetColor);
            _targetSkylineTint = CurrentZone.SkylineTint;
            _targetForestTint = CurrentZone.ForestTint;
            _targetStarAlpha = CurrentZone.StarAlpha;
            _targetNebulaAlpha = CurrentZone.NebulaAlpha;
            _targetGroundTint = CurrentZone.GroundTint;
            // Hard-set rather than left to Update()'s lerp - a Retry/new run must show the starting
            // zone's look immediately, not fade in from whatever tint the previous run (or a stale
            // Galactic star/nebula/forest layer) ended on.
            ApplyImmediate(_targetColor, _targetSkyTint, _targetSkylineTint, _targetForestTint, _targetStarAlpha, _targetNebulaAlpha, _targetGroundTint);
            GameEvents.RaiseZoneChanged(CurrentZone);
        }

        private void HandleDistanceChanged(float meters)
        {
            var zone = ZoneCatalog.ForDistance(meters);
            if (zone != CurrentZone)
            {
                CurrentZone = zone;
                _targetColor = zone.BackgroundTint;
                _targetSkyTint = SkyTintFor(_targetColor);
                _targetSkylineTint = zone.SkylineTint;
                _targetForestTint = zone.ForestTint;
                _targetStarAlpha = zone.StarAlpha;
                _targetNebulaAlpha = zone.NebulaAlpha;
                _targetGroundTint = zone.GroundTint;
                UpdateGroundSprite(zone);
                GameEvents.RaiseZoneChanged(zone);
            }
        }

        private Color SkyTintFor(Color backgroundColor)
        {
            return new Color(
                Mathf.Lerp(backgroundColor.r, skyAtmosphereTint.r, skyBlendAmount),
                Mathf.Lerp(backgroundColor.g, skyAtmosphereTint.g, skyBlendAmount),
                Mathf.Lerp(backgroundColor.b, skyAtmosphereTint.b, skyBlendAmount),
                1f);
        }

        private void ApplyImmediate(Color backgroundColor, Color skyTint, Color skylineTint, Color forestTint, float starAlpha, float nebulaAlpha, Color groundTint)
        {
            if (targetCamera != null) targetCamera.backgroundColor = backgroundColor;
            if (skyRenderer != null) skyRenderer.color = skyTint;
            if (skylineRenderers != null)
            {
                foreach (var sr in skylineRenderers)
                {
                    if (sr != null) sr.color = skylineTint;
                }
            }
            if (forestRenderers != null)
            {
                foreach (var sr in forestRenderers)
                {
                    if (sr != null) sr.color = forestTint;
                }
            }
            if (starsRenderer != null) starsRenderer.color = new Color(1f, 1f, 1f, starAlpha);
            if (nebulaRenderer != null) nebulaRenderer.color = new Color(1f, 1f, 1f, nebulaAlpha);
            if (groundRenderers != null)
            {
                Sprite sp = (zoneFloorSprites != null && zoneFloorSprites.Length > 0) ? zoneFloorSprites[0] : null;
                foreach (var gr in groundRenderers)
                {
                    if (gr != null)
                    {
                        if (sp != null) gr.sprite = sp;
                        gr.color = groundTint;
                    }
                }
            }
        }

        private void Update()
        {
            if (targetCamera != null)
            {
                targetCamera.backgroundColor = Color.Lerp(targetCamera.backgroundColor, _targetColor, Time.deltaTime * tintLerpSpeed);
            }
            if (skyRenderer != null)
            {
                skyRenderer.color = Color.Lerp(skyRenderer.color, _targetSkyTint, Time.deltaTime * tintLerpSpeed);
            }
            if (skylineRenderers != null)
            {
                foreach (var sr in skylineRenderers)
                {
                    if (sr != null) sr.color = Color.Lerp(sr.color, _targetSkylineTint, Time.deltaTime * tintLerpSpeed);
                }
            }
            if (forestRenderers != null)
            {
                foreach (var sr in forestRenderers)
                {
                    if (sr != null) sr.color = Color.Lerp(sr.color, _targetForestTint, Time.deltaTime * tintLerpSpeed);
                }
            }
            if (starsRenderer != null)
            {
                float a = Mathf.Lerp(starsRenderer.color.a, _targetStarAlpha, Time.deltaTime * tintLerpSpeed);
                starsRenderer.color = new Color(1f, 1f, 1f, a);
            }
            if (nebulaRenderer != null)
            {
                float a = Mathf.Lerp(nebulaRenderer.color.a, _targetNebulaAlpha, Time.deltaTime * tintLerpSpeed);
                nebulaRenderer.color = new Color(1f, 1f, 1f, a);
            }
            if (groundRenderers != null)
            {
                foreach (var gr in groundRenderers)
                {
                    if (gr != null) gr.color = Color.Lerp(gr.color, _targetGroundTint, Time.deltaTime * tintLerpSpeed);
                }
            }
        }

        private void UpdateGroundSprite(ZoneDefinition zone)
        {
            if (groundRenderers == null || zoneFloorSprites == null || zoneFloorSprites.Length == 0) return;
            int zoneIndex = 0;
            for (int i = 0; i < ZoneCatalog.Zones.Length; i++)
            {
                if (ZoneCatalog.Zones[i] == zone)
                {
                    zoneIndex = i;
                    break;
                }
            }
            if (zoneIndex < zoneFloorSprites.Length && zoneFloorSprites[zoneIndex] != null)
            {
                Sprite newFloor = zoneFloorSprites[zoneIndex];
                foreach (var gr in groundRenderers)
                {
                    if (gr != null) gr.sprite = newFloor;
                }
            }
        }
    }
}
