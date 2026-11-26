using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Angled top-down camera that follows the player and stays inside the map.
    /// Works with an orthographic camera (the Hiraya look) or a perspective one.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Where the camera sits relative to the player: straight back along the view direction. Pitch 62, distance 40 gives (0, 35.3, -18.8).")]
        [SerializeField] Vector3 offset = new Vector3(0f, 35.3f, -18.8f);
        [SerializeField] float smoothTime = 0.12f;

        [Header("Orthographic zoom")]
        [Tooltip("The camera's Size is the zoom in landscape. In portrait the view is zoomed out until at least this many units fit left and right of the player.")]
        [SerializeField] float minHalfWidth = 7f;

        [Header("Inside a building")]
        [Tooltip("Moves the room down on the screen so it sits between the HUD at the top and at the bottom.")]
        [SerializeField] float roomViewShift = 0.5f;

        [Header("Keep the view inside the fence")]
        [SerializeField] bool clampToMap = true;
        [SerializeField] Vector2 mapHalfSize = new Vector2(60f, 60f);
        [SerializeField] Vector2 margin = new Vector2(7f, 3.5f);

        Camera cam;
        float baseOrthographicSize;
        Vector3 velocity;
        Vector3? room;
        Color outsideBackground;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null) baseOrthographicSize = cam.orthographicSize;
        }

        void Start()
        {
            if (target != null) transform.position = Desired();
        }

        void LateUpdate()
        {
            if (cam != null && cam.orthographic)
                cam.orthographicSize = Mathf.Max(baseOrthographicSize, minHalfWidth / Mathf.Max(0.1f, cam.aspect));

            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }

        /// <summary>
        /// Inside a building: look at the middle of the room, with black around it, like in old games.
        /// Null = back outside, following the player. The camera jumps there at once.
        /// </summary>
        public void ShowRoom(Vector3? centre)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (centre.HasValue && !room.HasValue && cam != null)
            {
                outsideBackground = cam.backgroundColor;
                cam.backgroundColor = Color.black;
            }
            else if (!centre.HasValue && room.HasValue && cam != null)
            {
                cam.backgroundColor = outsideBackground;
            }
            room = centre;
            velocity = Vector3.zero;
            if (target != null) transform.position = Desired();
        }

        Vector3 Desired()
        {
            if (room.HasValue) return room.Value + offset + Vector3.forward * roomViewShift;
            Vector3 focus = target.position;
            if (clampToMap)
            {
                float limitX = Mathf.Max(0f, mapHalfSize.x - margin.x);
                float limitZ = Mathf.Max(0f, mapHalfSize.y - margin.y);
                focus.x = Mathf.Clamp(focus.x, -limitX, limitX);
                focus.z = Mathf.Clamp(focus.z, -limitZ, limitZ);
            }
            return focus + offset;
        }
    }
}
