using UnityEngine;

namespace DragonMasters
{
    /// <summary>Progreso del jugador guardado en PlayerPrefs.</summary>
    public static class Save
    {
        public static int Coins
        {
            get => PlayerPrefs.GetInt("dm_coins", 0);
            set { PlayerPrefs.SetInt("dm_coins", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Nivel actual (0..n). Pasado el último enemigo se vuelve a empezar con más dificultad.</summary>
        public static int Level
        {
            get => PlayerPrefs.GetInt("dm_level", 0);
            set { PlayerPrefs.SetInt("dm_level", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static string Hero
        {
            get => PlayerPrefs.GetString("dm_hero", "pirate");
            set { PlayerPrefs.SetString("dm_hero", value); PlayerPrefs.Save(); }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt("dm_muted", 0) == 1;
            set { PlayerPrefs.SetInt("dm_muted", value ? 1 : 0); PlayerPrefs.Save(); AudioListener.volume = value ? 0 : 1; }
        }

        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt("dm_tutorial", 0) == 1;
            set { PlayerPrefs.SetInt("dm_tutorial", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Owns(HeroDef h) => h.price == 0 || PlayerPrefs.GetInt("dm_own_" + h.id, 0) == 1;

        public static void Unlock(HeroDef h)
        {
            PlayerPrefs.SetInt("dm_own_" + h.id, 1);
            PlayerPrefs.Save();
        }
    }
}
