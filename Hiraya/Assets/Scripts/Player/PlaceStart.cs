using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Puts the player in front of the building that was picked on the world map (GameSession.PlaceBuilding).
    /// If nothing was picked, or the building is not in the scene, the player stays where the scene put them.
    /// </summary>
    public class PlaceStart : MonoBehaviour
    {
        [Tooltip("How far in front of the building's Entrance marker the player stands (towards the camera).")]
        [SerializeField] float extraDistance = 1.5f;

        void Awake()
        {
            string buildingName = GameSession.PlaceBuilding;
            if (string.IsNullOrEmpty(buildingName)) return;

            GameObject building = GameObject.Find(buildingName);
            if (building == null) return;

            Transform entrance = building.transform.Find("Entrance");
            Vector3 target = entrance != null ? entrance.position : building.transform.position;
            target += Vector3.back * extraDistance;
            target.y = transform.position.y;

            transform.position = target;
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null) body.position = target;
        }
    }
}
