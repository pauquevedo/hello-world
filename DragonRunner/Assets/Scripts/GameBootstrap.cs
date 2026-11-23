using UnityEngine;

namespace DragonRunner
{
    /// <summary>
    /// Arranca el juego automáticamente al darle a Play en cualquier escena:
    /// no hace falta montar nada a mano en el Editor.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            new GameObject("DragonRunner").AddComponent<GameManager>();
        }
    }
}
