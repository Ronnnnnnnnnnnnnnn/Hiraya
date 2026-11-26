using UnityEngine;
using UnityEngine.EventSystems;

namespace Hiraya
{
    /// <summary>Which on-screen D-pad arrows are currently held. Read by PlayerController.</summary>
    public static class DPadState
    {
        static readonly bool[] held = new bool[4];

        public static bool IsHeld(Facing direction) => held[(int)direction];

        public static void Set(Facing direction, bool value) => held[(int)direction] = value;

        public static void Clear()
        {
            for (int i = 0; i < held.Length; i++) held[i] = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Clear();
    }

    /// <summary>One arrow of the on-screen D-pad (Up / Down / Left / Right). Put on a UI Image.</summary>
    public class DPadButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] Facing direction = Facing.Up;

        public void OnPointerDown(PointerEventData eventData) => DPadState.Set(direction, true);

        public void OnPointerUp(PointerEventData eventData) => DPadState.Set(direction, false);

        public void OnPointerExit(PointerEventData eventData) => DPadState.Set(direction, false);

        void OnDisable() => DPadState.Set(direction, false);
    }
}
