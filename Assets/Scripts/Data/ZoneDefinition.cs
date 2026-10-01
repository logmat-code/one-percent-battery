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

        public ZoneDefinition(string name, float startMeters, Color backgroundTint, Color fogTint, Color skylineTint, Color forestTint, float starAlpha = 0f, float nebulaAlpha = 0f)
        {
            Name = name;
            StartMeters = startMeters;
            BackgroundTint = backgroundTint;
            FogTint = fogTint;
            SkylineTint = skylineTint;
            ForestTint = forestTint;
            StarAlpha = starAlpha;
            NebulaAlpha = nebulaAlpha;
        }
    }

    public static class ZoneCatalog
    {
        // StartMeters spaced at an even 400m cadence across the 5 zones (shortened from 500m so later
        // zones are realistically reachable - see RunModeConfig.SpeedPerMeter for the matching difficulty
        // ramp reduction). Only ONE grounded silhouette (Skyline in CITY, Forest in FOREST) is ever meant
        // to be visible at a time - the other's tint alpha is 0, and ZoneManager's existing color lerp
        // naturally cross-fades between them at the boundary rather than ever stacking both permanently.
        // From STRATOSPHERE onward both grounded layers are fully 0 - nothing terrestrial survives into
        // ORBIT/GALACTIC.
        public static readonly ZoneDefinition[] Zones =
        {
            new ZoneDefinition("CITY", 0f,
                new Color(0.10f, 0.12f, 0.16f), new Color(0.35f, 0.45f, 0.55f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0.95f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0f, nebulaAlpha: 0f),
            new ZoneDefinition("FOREST", 400f,
                new Color(0.08f, 0.13f, 0.11f), new Color(0.3f, 0.5f, 0.4f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0.92f),
                starAlpha: 0f, nebulaAlpha: 0f),
            new ZoneDefinition("STRATOSPHERE", 800f,
                new Color(0.06f, 0.07f, 0.18f), new Color(0.4f, 0.5f, 0.75f),
                skylineTint: new Color(0.13f, 0.14f, 0.18f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0.45f, nebulaAlpha: 0.10f),
            new ZoneDefinition("ORBIT", 1200f,
                new Color(0.03f, 0.03f, 0.13f), new Color(0.55f, 0.45f, 0.8f),
                skylineTint: new Color(0.13f, 0.11f, 0.16f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 0.75f, nebulaAlpha: 0.35f),
            new ZoneDefinition("GALACTIC", 1600f,
                new Color(0.02f, 0.02f, 0.08f), new Color(0.7f, 0.35f, 0.85f),
                skylineTint: new Color(0.14f, 0.11f, 0.15f, 0f),
                forestTint: new Color(0.07f, 0.12f, 0.09f, 0f),
                starAlpha: 1f, nebulaAlpha: 0.85f),
        };

        public static ZoneDefinition ForDistance(float meters)
        {
            ZoneDefinition current = Zones[0];
            for (int i = 0; i < Zones.Length; i++)
            {
                if (meters >= Zones[i].StartMeters)
                {
                    current = Zones[i];
                }
            }
            return current;
        }
    }
}
