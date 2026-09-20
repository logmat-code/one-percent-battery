using OnePercentBattery.Core;
using OnePercentBattery.Data;
using UnityEngine;

namespace OnePercentBattery.World
{
    /// <summary>Tracks which cosmetic zone the run is in (Desk, Rooftop, ... Galactic) and tints the camera background to match.</summary>
    public sealed class ZoneManager : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float tintLerpSpeed = 2f;

        public ZoneDefinition CurrentZone { get; private set; }

        private Color _targetColor;

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
            if (targetCamera != null)
            {
                targetCamera.backgroundColor = _targetColor;
            }
            GameEvents.RaiseZoneChanged(CurrentZone);
        }

        private void HandleDistanceChanged(float meters)
        {
            var zone = ZoneCatalog.ForDistance(meters);
            if (zone != CurrentZone)
            {
                CurrentZone = zone;
                _targetColor = zone.BackgroundTint;
                GameEvents.RaiseZoneChanged(zone);
            }
        }

        private void Update()
        {
            if (targetCamera == null) return;
            targetCamera.backgroundColor = Color.Lerp(targetCamera.backgroundColor, _targetColor, Time.deltaTime * tintLerpSpeed);
        }
    }
}
