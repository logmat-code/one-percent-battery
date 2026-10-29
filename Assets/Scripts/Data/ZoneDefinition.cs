using UnityEngine;

namespace OnePercentBattery.Data
{
    /// <summary>A cosmetic backdrop stretch of the run. Purely visual/flavor - does not affect difficulty.</summary>
    public sealed class ZoneDefinition
    {
        public readonly string Name;
        public readonly float StartMeters;
        public readonly Color BackgroundTint;
        public readonly Color FogTint;
        public readonly Color SkylineTint;
        /// <summary>Tint (including alpha) of the camera-locked forest treeline layer - alpha 0 hides it entirely. Independent of SkylineTint so the two grounded layers can cross-fade into each other instead of ever both reading as "on" at full strength.</summary>
        public readonly Color ForestTint;
        /// <summary>0-1 opacity of the camera-locked star field - the texture's own stars vary in baked brightness, so a low value here naturally shows only the brightest few rather than a uniformly faint copy of the full field.</summary>
        public readonly float StarAlpha;
        /// <summary>0-1 opacity of the camera-locked nebula cloud - stays 0 through Forest and only ramps up from Stratosphere onward, so Galactic's is a genuine payoff rather than a constant background element.</summary>
        public readonly float NebulaAlpha;
        /// <summary>Zone-themed color for the running track / ground platform.</summary>
        public readonly Color GroundTint;

        public ZoneDefinition(string name, float startMeters, Color backgroundTint, Color fogTint, Color skylineTint, Color forestTint, float starAlpha = 0f, float nebulaAlpha = 0f, Color? groundTint = null)
        {
            Name = name;
            StartMeters = startMeters;
            BackgroundTint = backgroundTint;
            FogTint = fogTint;
            SkylineTint = skylineTint;
            ForestTint = forestTint;
            StarAlpha = starAlpha;
            NebulaAlpha = nebulaAlpha;
            GroundTint = groundTint ?? new Color(0.20f, 0.22f, 0.26f, 1f);
        }
    }

    public static class ZoneCatalog
    {
        // Cadence: every 250m a new zone is reached.
        // 0 - 249m: CITY
        // 250 - 499m: FOREST
        // 500 - 749m: STRATOSPHERE
        // 750 - 999m: ORBIT
        // 1000 - 1249m: GALACTIC
        // 1250m+: Loops back to CITY and cycles continuously.
        public const float ZoneIntervalMeters = 250f;

        public static readonly ZoneDefinition[] Zones =
        {
            new ZoneDefinition("CITY", 0f,
                new Color(0.10f, 0.12f, 0.16f), new Color(0.35f, 0.45f, 0.55f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0.95f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0f, nebulaAlpha: 0f,
                groundTint: new Color(0.18f, 0.20f, 0.24f, 1f)),
            new ZoneDefinition("FOREST", 250f,
                new Color(0.08f, 0.13f, 0.11f), new Color(0.3f, 0.5f, 0.4f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0.92f),
                starAlpha: 0f, nebulaAlpha: 0f,
                groundTint: new Color(0.12f, 0.20f, 0.14f, 1f)),
            new ZoneDefinition("STRATOSPHERE", 500f,
                new Color(0.06f, 0.07f, 0.18f), new Color(0.4f, 0.5f, 0.75f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0.45f, nebulaAlpha: 0.10f,
                groundTint: new Color(0.20f, 0.24f, 0.35f, 1f)),
            new ZoneDefinition("ORBIT", 750f,
                new Color(0.03f, 0.03f, 0.13f), new Color(0.55f, 0.45f, 0.8f),
                skylineTint: new Color(0.13f, 0.11f, 0.16f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0.75f, nebulaAlpha: 0.35f,
                groundTint: new Color(0.15f, 0.18f, 0.28f, 1f)),
            new ZoneDefinition("GALACTIC", 1000f,
                new Color(0.02f, 0.02f, 0.08f), new Color(0.7f, 0.35f, 0.85f),
                skylineTint: new Color(0.14f, 0.11f, 0.15f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 1f, nebulaAlpha: 0.85f,
                groundTint: new Color(0.22f, 0.12f, 0.30f, 1f)),
        };

        public static ZoneDefinition ForDistance(float meters)
        {
            if (meters < 0f) meters = 0f;
            int cycleIndex = Mathf.FloorToInt(meters / ZoneIntervalMeters) % Zones.Length;
            return Zones[cycleIndex];
        }
    }
}
