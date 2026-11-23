using UnityEngine;

namespace DragonMasters
{
    public enum SpinMode { Spin, Arrow, Tumble }

    public class WeaponDef
    {
        public string id, sprite, hitFx;
        public float size;      // tamaño en unidades de mundo
        public float damage;    // multiplicador de daño
        public float spin;      // grados/segundo (Spin/Tumble)
        public float baseAngle; // hacia dónde "apunta" el dibujo original (modo Arrow)
        public SpinMode mode;
        public Color tint = Color.white;

        public WeaponDef(string id, string sprite, SpinMode mode, float size, float damage, float spin, string hitFx, float baseAngle = 0)
        {
            this.baseAngle = baseAngle;
            this.id = id; this.sprite = sprite; this.mode = mode; this.size = size;
            this.damage = damage; this.spin = spin; this.hitFx = hitFx;
        }
    }

    public class HeroDef
    {
        public string id, sprite, name, weapon;
        public int price;
        public HeroDef(string id, string sprite, string name, string weapon, int price)
        { this.id = id; this.sprite = sprite; this.name = name; this.weapon = weapon; this.price = price; }
    }

    public class Theme
    {
        public string name;
        public Color skyTop, skyBottom, hills, grass, dirt;
        public bool sea, night;
        public string[] buildings = new string[0];
        public string[] props = new string[0];
        public string[] boats = new string[0];
        public string[] air = new string[0];
        public string[] npcs = new string[0];
    }

    public class EnemyDef
    {
        public string id, sprite, name, weapon;
        public int hp;
        public float height, hover, error, damage, headY = 0.72f;
        public Theme theme;
    }

    /// <summary>Todo el contenido del juego, construido con los sprites de "Brand 01".</summary>
    public static class Catalog
    {
        public static readonly WeaponDef[] Weapons =
        {
            new WeaponDef("sword",   "weapons/sword",              SpinMode.Spin,   1.5f, 1.00f, 900, "effects_vfx/impact"),
            new WeaponDef("axe",     "weapons/axe",                SpinMode.Spin,   1.4f, 1.15f, 800, "effects_vfx/impact"),
            new WeaponDef("hammer",  "weapons/hammer",             SpinMode.Spin,   1.4f, 1.25f, 600, "effects_vfx/star_burst"),
            new WeaponDef("spear",   "weapons/spear",              SpinMode.Arrow,  2.0f, 1.05f, 0,   "effects_vfx/impact", 45),
            new WeaponDef("staff",   "weapons/staff",              SpinMode.Spin,   1.6f, 1.00f, 700, "effects_vfx/magic_swirl"),
            new WeaponDef("shield",  "weapons/shield",             SpinMode.Spin,   1.1f, 0.95f, 1100,"effects_vfx/star_burst"),
            new WeaponDef("potion",  "items_collectibles/potion",  SpinMode.Tumble, 0.9f, 1.00f, 500, "effects_vfx/splash"),
            new WeaponDef("star",    "items_collectibles/star",    SpinMode.Spin,   0.9f, 1.00f, 1000,"effects_vfx/star_burst"),
            new WeaponDef("gem",     "items_collectibles/gem",     SpinMode.Tumble, 0.8f, 1.05f, 600, "effects_vfx/star_burst"),
            new WeaponDef("coins",   "items_collectibles/coin_pile",SpinMode.Tumble,1.0f, 0.95f, 400, "effects_vfx/star_burst"),
            new WeaponDef("key",     "items_collectibles/key",     SpinMode.Spin,   1.0f, 1.00f, 900, "effects_vfx/impact"),
            new WeaponDef("gift",    "items_collectibles/gift",    SpinMode.Tumble, 1.0f, 1.10f, 400, "effects_vfx/confetti"),
            new WeaponDef("lightning","effects_vfx/lightning",     SpinMode.Arrow,  1.3f, 1.10f, 0,   "effects_vfx/lightning", -105),
            new WeaponDef("fire",    "effects_vfx/fire",           SpinMode.Arrow,  1.2f, 1.15f, 0,   "effects_vfx/explosion", -90),
            new WeaponDef("splash",  "effects_vfx/splash",         SpinMode.Tumble, 1.1f, 0.95f, 300, "effects_vfx/splash"),
            new WeaponDef("swirl",   "effects_vfx/magic_swirl",    SpinMode.Spin,   1.1f, 1.05f, 800, "effects_vfx/magic_swirl"),
            new WeaponDef("starburst","effects_vfx/star_burst",    SpinMode.Spin,   1.1f, 1.00f, 900, "effects_vfx/confetti"),
            new WeaponDef("rock",    "environment_props/rocks",    SpinMode.Tumble, 1.1f, 1.20f, 350, "effects_vfx/smoke"),
            new WeaponDef("bush",    "environment_props/flower_bush",SpinMode.Tumble,1.1f,0.95f, 400, "effects_vfx/confetti"),
            new WeaponDef("barrel",  "environment_props/barrel_crate",SpinMode.Tumble,1.2f,1.20f, 300, "effects_vfx/explosion"),
        };

        public static readonly HeroDef[] Heroes =
        {
            new HeroDef("pirate",    "dragon_costumes/pirate",    "Pirata",     "sword",    0),
            new HeroDef("knight",    "dragon_costumes/knight",    "Caballero",  "axe",      0),
            new HeroDef("ninja",     "dragon_costumes/ninja",     "Ninja",      "spear",    0),
            new HeroDef("king",      "dragon_costumes/king",      "Rey",        "hammer",   150),
            new HeroDef("wizard",    "dragon_costumes/wizard",    "Mago",       "staff",    200),
            new HeroDef("chef",      "dragon_costumes/chef",      "Chef",       "potion",   250),
            new HeroDef("aviator",   "dragon_costumes/aviator",   "Aviador",    "shield",   300),
            new HeroDef("cyber",     "dragon_costumes/cyber",     "Cyber",      "lightning",400),
            new HeroDef("astronaut", "dragon_costumes/astronaut", "Astronauta", "star",     450),
            new HeroDef("water",     "dragon_elements/water",     "Agua",       "splash",   500),
            new HeroDef("plant",     "dragon_elements/plant",     "Planta",     "bush",     550),
            new HeroDef("starling",  "dragon_elements/star",      "Estelar",    "starburst",600),
            new HeroDef("ice",       "dragon_elements/ice",       "Hielo",      "gem",      650),
            new HeroDef("stone",     "dragon_elements/stone",     "Piedra",     "rock",     700),
            new HeroDef("thunder",   "dragon_elements/thunder",   "Rayo",       "lightning",800),
            new HeroDef("red",       "dragon_elements/red_adult", "Dragón Rojo","fire",     900),
            new HeroDef("elder",     "dragon_elements/elder",     "Anciano",    "swirl",    1000),
            new HeroDef("baby",      "dragon_elements/baby_egg",  "Cría",       "gift",     1200),
        };

        public static readonly EnemyDef[] Enemies = BuildEnemies();

        public static WeaponDef Weapon(string id)
        {
            foreach (var w in Weapons) if (w.id == id) return w;
            return Weapons[0];
        }

        public static HeroDef Hero(string id)
        {
            foreach (var h in Heroes) if (h.id == id) return h;
            return Heroes[0];
        }

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static EnemyDef[] BuildEnemies()
        {
            var meadow = new Theme
            {
                name = "Prado", skyTop = C("#5BB8F0"), skyBottom = C("#CDEFFF"), hills = C("#8CCB6A"),
                grass = C("#6DBE45"), dirt = C("#8A5A35"),
                buildings = new[] { "buildings/house", "buildings/windmill", "buildings/house" },
                props = new[] { "environment_props/fruit_tree", "environment_props/flower_bush", "environment_props/fence", "environment_props/well" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/farmer", "characters_npcs/adventurer" },
            };
            var forest = new Theme
            {
                name = "Bosque", skyTop = C("#4FA3D9"), skyBottom = C("#D8F3E0"), hills = C("#4E9A5B"),
                grass = C("#4FA040"), dirt = C("#6E4528"),
                buildings = new[] { "buildings/egg_cave", "buildings/house" },
                props = new[] { "environment_props/pine", "environment_props/pine", "environment_props/rocks", "environment_props/flower_bush" },
                npcs = new[] { "characters_npcs/adventurer", "characters_npcs/wizard" },
            };
            var night = new Theme
            {
                name = "Noche encantada", skyTop = C("#141A45"), skyBottom = C("#5A4A8C"), hills = C("#2E3460"),
                grass = C("#3E7A55"), dirt = C("#3B2A3A"), night = true,
                buildings = new[] { "buildings/tower", "buildings/castle" },
                props = new[] { "environment_props/pine", "environment_props/well", "environment_props/fence" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/wizard" },
            };
            var beach = new Theme
            {
                name = "Playa pirata", skyTop = C("#39B5E6"), skyBottom = C("#FFE9B8"), hills = C("#E8C98A"),
                grass = C("#E9CF8E"), dirt = C("#C69A5A"), sea = true,
                buildings = new[] { "buildings/tavern" },
                props = new[] { "environment_props/palm", "environment_props/barrel_crate", "weapons/cannon", "environment_props/palm" },
                boats = new[] { "vehicles_boats/pirate_ship", "vehicles_boats/galleon", "vehicles_boats/rowboat" },
                npcs = new[] { "characters_npcs/pirate" },
            };
            var market = new Theme
            {
                name = "Mercado", skyTop = C("#6EC3F2"), skyBottom = C("#FFF1D6"), hills = C("#A6C878"),
                grass = C("#7CC452"), dirt = C("#9A6B42"),
                buildings = new[] { "buildings/shop", "buildings/tavern", "buildings/house" },
                props = new[] { "environment_props/barrel_crate", "environment_props/fence", "environment_props/fruit_tree", "items_collectibles/chest" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/merchant", "characters_npcs/farmer", "characters_npcs/princess" },
            };
            var workshop = new Theme
            {
                name = "Taller", skyTop = C("#F2A25C"), skyBottom = C("#FFE0B0"), hills = C("#B9825A"),
                grass = C("#8DAF4E"), dirt = C("#7A5236"), sea = true,
                buildings = new[] { "buildings/forge", "buildings/windmill" },
                props = new[] { "environment_props/barrel_crate", "weapons/cannon", "environment_props/rocks" },
                boats = new[] { "vehicles_boats/steamboat", "vehicles_boats/submarine" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/blacksmith", "characters_npcs/aviator" },
            };
            var storm = new Theme
            {
                name = "Costa tormentosa", skyTop = C("#3D4E66"), skyBottom = C("#9DB0C2"), hills = C("#5E7A73"),
                grass = C("#5C9A5A"), dirt = C("#5B4636"), sea = true, night = false,
                buildings = new[] { "buildings/lighthouse" },
                props = new[] { "environment_props/rocks", "environment_props/bridge", "environment_props/pine" },
                boats = new[] { "vehicles_boats/sailboat", "vehicles_boats/raft", "vehicles_boats/drakkar" },
                npcs = new[] { "characters_npcs/aviator" },
            };
            var mountain = new Theme
            {
                name = "Montaña", skyTop = C("#7FB2E0"), skyBottom = C("#EAF2F8"), hills = C("#8D8FA3"),
                grass = C("#7FA864"), dirt = C("#6B6070"),
                buildings = new[] { "buildings/egg_cave", "buildings/tower" },
                props = new[] { "environment_props/rocks", "environment_props/pine", "environment_props/bridge", "environment_props/rocks" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/adventurer", "characters_npcs/blacksmith" },
            };
            var castle = new Theme
            {
                name = "Castillo", skyTop = C("#7A5BC8"), skyBottom = C("#FFC9A8"), hills = C("#8A6FA8"),
                grass = C("#6CB24E"), dirt = C("#6F4A3A"), sea = true,
                buildings = new[] { "buildings/castle", "buildings/tower", "buildings/lighthouse" },
                props = new[] { "environment_props/fence", "environment_props/fruit_tree", "items_collectibles/chest", "environment_props/well" },
                boats = new[] { "vehicles_boats/galleon", "vehicles_boats/drakkar" },
                air = new[] { "vehicles_boats/airship" },
                npcs = new[] { "characters_npcs/knight", "characters_npcs/princess", "characters_npcs/wizard" },
            };

            return new[]
            {
                new EnemyDef { id = "slime",    sprite = "enemies/slime",           name = "Slime",           weapon = "splash",    hp = 80,  height = 1.8f, error = 0.17f, damage = 0.80f, headY = 0.62f, theme = meadow },
                new EnemyDef { id = "mushroom", sprite = "enemies/mushroom",        name = "Seta Gruñona",    weapon = "bush",      hp = 95,  height = 2.4f, error = 0.15f, damage = 0.85f, headY = 0.74f, theme = forest },
                new EnemyDef { id = "ghost",    sprite = "enemies/ghost",           name = "Fantasmita",      weapon = "swirl",     hp = 105, height = 2.2f, hover = 0.6f, error = 0.14f, damage = 0.90f, headY = 0.70f, theme = night },
                new EnemyDef { id = "skeleton", sprite = "enemies/skeleton_pirate", name = "Esqueleto Pirata",weapon = "sword",     hp = 120, height = 2.6f, error = 0.12f, damage = 0.95f, headY = 0.74f, theme = beach },
                new EnemyDef { id = "raccoon",  sprite = "enemies/raccoon",         name = "Mapache Ladrón",  weapon = "coins",     hp = 135, height = 2.4f, error = 0.11f, damage = 1.00f, headY = 0.76f, theme = market },
                new EnemyDef { id = "robot",    sprite = "enemies/robot",           name = "Robo-Tuerca",     weapon = "key",       hp = 150, height = 2.7f, error = 0.10f, damage = 1.05f, headY = 0.74f, theme = workshop },
                new EnemyDef { id = "cloud",    sprite = "enemies/storm_cloud",     name = "Nube Tormenta",   weapon = "lightning", hp = 165, height = 2.0f, hover = 1.2f, error = 0.09f, damage = 1.10f, headY = 0.62f, theme = storm },
                new EnemyDef { id = "golem",    sprite = "enemies/golem",           name = "Gólem",           weapon = "rock",      hp = 190, height = 3.2f, error = 0.08f, damage = 1.15f, headY = 0.80f, theme = mountain },
                new EnemyDef { id = "frog",     sprite = "enemies/frog_king",       name = "Rey Sapo",        weapon = "staff",     hp = 220, height = 3.0f, error = 0.07f, damage = 1.20f, headY = 0.70f, theme = castle },
            };
        }
    }
}
