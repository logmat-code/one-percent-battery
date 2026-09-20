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

        public ZoneDefinition(string name, float startMeters, Color backgroundTint, Color fogTint)
        {
            Name = name;
            StartMeters = startMeters;
            BackgroundTint = backgroundTint;
            FogTint = fogTint;
        }
    }

    public static class ZoneCatalog
    {
        public static readonly ZoneDefinition[] Zones =
        {
            new ZoneDefinition("DESK", 0f, new Color(0.09f, 0.12f, 0.12f), new Color(0.35f, 0.55f, 0.35f)),
            new ZoneDefinition("ROOFTOP", 400f, new Color(0.08f, 0.10f, 0.16f), new Color(0.35f, 0.45f, 0.6f)),
            new ZoneDefinition("SKYLINE", 900f, new Color(0.06f, 0.08f, 0.18f), new Color(0.45f, 0.4f, 0.65f)),
            new ZoneDefinition("STRATOSPHERE", 1600f, new Color(0.05f, 0.05f, 0.16f), new Color(0.4f, 0.5f, 0.75f)),
            new ZoneDefinition("ORBIT", 2500f, new Color(0.03f, 0.03f, 0.12f), new Color(0.55f, 0.45f, 0.8f)),
            new ZoneDefinition("GALACTIC", 4000f, new Color(0.02f, 0.02f, 0.09f), new Color(0.7f, 0.35f, 0.85f)),
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
