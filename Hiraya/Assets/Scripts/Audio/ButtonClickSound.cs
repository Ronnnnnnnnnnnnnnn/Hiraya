using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// Put on a Button or a Toggle (a switch): tapping it plays the button click of the AudioManager.
    /// The editor tools add it to every button and switch of the screens they build. A button that cannot be pressed
    /// (not interactable) stays silent. Remove the component from a button that should be silent.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
    {
        Selectable selectable;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable == null) selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;
            AudioManager.PlayClick();
        }
    }
}
