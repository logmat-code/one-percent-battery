using UnityEngine;

namespace OnePercentBattery.Data
{
    public enum CosmeticCategory
    {
        Skin,
        Trail,
        Exclusive
    }

    /// <summary>A purchasable cosmetic. Coins-only by design - see Settings: "No in-app purchases".</summary>
    public sealed class CosmeticDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly CosmeticCategory Category;
        public readonly int CostCoins;
        public readonly Color AccentColor;
        public readonly bool IsLimited;

        public CosmeticDefinition(string id, string displayName, CosmeticCategory category, int costCoins, Color accentColor, bool isLimited = false)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            CostCoins = costCoins;
            AccentColor = accentColor;
            IsLimited = isLimited;
        }
    }

    public static class ShopCatalog
    {
        public const string DefaultSkinId = "skin_default";
        public const string NoneTrailId = "trail_none";

        public static readonly CosmeticDefinition[] Skins =
        {
            new CosmeticDefinition(DefaultSkinId, "Default", CosmeticCategory.Skin, 0, new Color(0.68f, 0.92f, 0.36f)),
            new CosmeticDefinition("skin_copper", "Copper Top", CosmeticCategory.Skin, 200, new Color(0.80f, 0.50f, 0.30f)),
            new CosmeticDefinition("skin_retro", "Retro", CosmeticCategory.Skin, 300, new Color(0.85f, 0.75f, 0.55f)),
            new CosmeticDefinition("skin_cute", "Cute", CosmeticCategory.Skin, 600, new Color(0.95f, 0.55f, 0.75f)),
            new CosmeticDefinition("skin_robot", "Robot", CosmeticCategory.Skin, 700, new Color(0.6f, 0.65f, 0.7f)),
            new CosmeticDefinition("skin_cyber", "Cyber", CosmeticCategory.Skin, 800, new Color(0.2f, 0.9f, 0.9f)),
            new CosmeticDefinition("skin_neon", "Neon", CosmeticCategory.Skin, 900, new Color(0.75f, 0.3f, 0.95f)),
            new CosmeticDefinition("skin_lightning", "Lightning", CosmeticCategory.Skin, 1000, new Color(0.95f, 0.85f, 0.2f)),
            new CosmeticDefinition("skin_ghost", "Ghost", CosmeticCategory.Skin, 1100, new Color(0.8f, 0.85f, 0.9f)),
            new CosmeticDefinition("skin_evil", "Evil", CosmeticCategory.Skin, 1300, new Color(0.75f, 0.1f, 0.15f)),
            new CosmeticDefinition("skin_gold", "Gold", CosmeticCategory.Skin, 1600, new Color(0.95f, 0.75f, 0.15f)),
            new CosmeticDefinition("skin_nuclear", "Nuclear", CosmeticCategory.Skin, 1900, new Color(0.65f, 0.95f, 0.15f)),
            new CosmeticDefinition("skin_glitch", "Glitch", CosmeticCategory.Skin, 2200, new Color(0.15f, 0.95f, 0.6f)),
            new CosmeticDefinition("skin_alien", "Alien", CosmeticCategory.Skin, 2500, new Color(0.4f, 0.9f, 0.4f)),
        };

        public static readonly CosmeticDefinition[] Trails =
        {
            new CosmeticDefinition(NoneTrailId, "None", CosmeticCategory.Trail, 0, Color.clear),
            new CosmeticDefinition("trail_smoke", "Smoke", CosmeticCategory.Trail, 300, new Color(0.7f, 0.7f, 0.7f)),
            new CosmeticDefinition("trail_electric", "Electric", CosmeticCategory.Trail, 350, new Color(0.4f, 0.7f, 1f)),
            new CosmeticDefinition("trail_fire", "Fire", CosmeticCategory.Trail, 450, new Color(1f, 0.45f, 0.15f)),
            new CosmeticDefinition("trail_stars", "Stars", CosmeticCategory.Trail, 550, new Color(0.9f, 0.9f, 0.95f)),
            new CosmeticDefinition("trail_pixels", "Pixels", CosmeticCategory.Trail, 600, new Color(0.4f, 0.9f, 0.4f)),
            new CosmeticDefinition("trail_lightning", "Lightning", CosmeticCategory.Trail, 700, new Color(0.95f, 0.85f, 0.2f)),
            new CosmeticDefinition("trail_glitch", "Glitch", CosmeticCategory.Trail, 850, new Color(0.85f, 0.2f, 0.85f)),
            new CosmeticDefinition("trail_plasma", "Plasma", CosmeticCategory.Trail, 950, new Color(0.6f, 0.3f, 0.95f)),
            new CosmeticDefinition("trail_rainbow", "Rainbow", CosmeticCategory.Trail, 1200, new Color(1f, 1f, 1f)),
        };

        /// <summary>Monthly rotating drop. In the source game this rotates on a calendar schedule; here it's a fixed set the caller can re-roll.</summary>
        public static readonly CosmeticDefinition[] Exclusive =
        {
            new CosmeticDefinition("exclusive_sunset", "Sunset", CosmeticCategory.Skin, 1500, new Color(0.95f, 0.5f, 0.3f), isLimited: true),
            new CosmeticDefinition("exclusive_midnight", "Midnight", CosmeticCategory.Skin, 1500, new Color(0.15f, 0.15f, 0.35f), isLimited: true),
            new CosmeticDefinition("exclusive_solar", "Solar", CosmeticCategory.Skin, 1500, new Color(0.95f, 0.8f, 0.2f), isLimited: true),
            new CosmeticDefinition("exclusive_galaxy", "Galaxy", CosmeticCategory.Trail, 1500, new Color(0.5f, 0.3f, 0.9f), isLimited: true),
        };

        public static CosmeticDefinition FindSkin(string id)
        {
            foreach (var skin in Skins)
            {
                if (skin.Id == id) return skin;
            }
            return Skins[0];
        }

        public static CosmeticDefinition FindTrail(string id)
        {
            foreach (var trail in Trails)
            {
                if (trail.Id == id) return trail;
            }
            return Trails[0];
        }
    }
}
