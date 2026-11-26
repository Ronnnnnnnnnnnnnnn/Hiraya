using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Settings that are applied once when the game starts, before the first scene.
    /// Phones run Unity games at 30 frames per second unless the game asks for more, which makes the walking look jerky,
    /// so the game asks for 60 (a phone that cannot do it simply runs slower).
    /// </summary>
    public static class GameBoot
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Start()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
