using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Keeps a 2D sprite standing upright and facing the camera (the 2.5D look).
    /// The sprite is also moved a little toward the camera along the line of sight so it looks exactly the same
    /// on screen, but its tilted head does not sink into fences, walls and buildings right behind it.
    /// </summary>
    public class BillboardToCamera : MonoBehaviour
    {
        [Tooltip("Perspective camera: how far the sprite is moved toward the camera. It is shrunk by the same amount so it keeps its on-screen size.")]
        [SerializeField] float perspectiveBias = 3.2f;

        [Tooltip("Orthographic camera: how far the sprite is moved toward the camera (the screen position does not change).")]
        [SerializeField] float orthographicBias = 2.4f;

        Camera cam;
        Transform anchor;

        void Awake()
        {
            anchor = transform.parent;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 pivot = anchor != null ? anchor.position : transform.position;

            if (cam.orthographic)
            {
                // All view rays are parallel, so sliding along -forward changes neither position nor size on screen.
                transform.position = pivot - cam.transform.forward * orthographicBias;
                transform.localScale = Vector3.one;
            }
            else
            {
                Vector3 toCamera = cam.transform.position - pivot;
                float distance = toCamera.magnitude;
                if (distance < 0.01f) return;

                float bias = Mathf.Min(perspectiveBias, distance * 0.5f);
                transform.position = pivot + toCamera / distance * bias;
                transform.localScale = Vector3.one * ((distance - bias) / distance);
            }

            transform.rotation = cam.transform.rotation;
        }
    }
}
