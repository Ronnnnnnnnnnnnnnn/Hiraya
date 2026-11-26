using System.Collections.Generic;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// The inside of a building: a room standing far away from the map. Walking up into the building's door brings the character
    /// here (GameplayDirector), walking down into the exit at the bottom of the room brings them back out, like in old games.
    /// Put things for this building inside the room (as children of it, or just standing in it).
    /// One room belongs to the building with the same Building Name, for example Building_School.
    /// </summary>
    public class Interior : MonoBehaviour
    {
        static readonly List<Interior> all = new List<Interior>();
        public static IReadOnlyList<Interior> All => all;

        [Tooltip("The name of the building object this room is inside of, for example Building_School or Building_House_2.")]
        [SerializeField] string buildingName = "Building_School";
        [Tooltip("Where the character appears when coming in.")]
        [SerializeField] Transform spawnPoint;
        [Tooltip("The exit at the bottom of the room: walking down into it leaves the room.")]
        [SerializeField] BoxCollider exit;
        [Tooltip("Width (x) and depth (z) of the floor.")]
        [SerializeField] Vector2 floorSize = new Vector2(16f, 11f);

        public string BuildingName => buildingName;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public Vector3 ExitPosition => exit != null ? exit.bounds.center : transform.position;
        /// <summary>Where the ring of the HUD goes to show the way out: on the floor just inside the exit.</summary>
        public Vector3 ExitMarkerPosition => new Vector3(ExitPosition.x, transform.position.y, ExitPosition.z + 0.65f);
        public Vector3 Centre => transform.position;

        /// <summary>"school" or "house", for the goal text.</summary>
        public bool IsSchool => buildingName != null && buildingName.Contains("School");

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => all.Clear();

        /// <summary>True if the point is on the floor of this room.</summary>
        public bool Contains(Vector3 point)
        {
            Vector3 local = point - transform.position;
            return Mathf.Abs(local.x) <= floorSize.x * 0.5f + 0.6f && Mathf.Abs(local.z) <= floorSize.y * 0.5f + 0.6f;
        }

        public bool IsAtExit(Vector3 feet) => exit != null && Inside(exit, feet);

        /// <summary>The room a point is in, or null for the outside world.</summary>
        public static Interior At(Vector3 point)
        {
            foreach (Interior room in all)
                if (room.Contains(point)) return room;
            return null;
        }

        public static Interior ForBuilding(string name)
        {
            foreach (Interior room in all)
                if (room.buildingName == name) return room;
            return null;
        }

        /// <summary>Is the character (feet) inside this box, looking only at the ground plane?</summary>
        public static bool Inside(Collider box, Vector3 feet)
        {
            Bounds b = box.bounds;
            return feet.x >= b.min.x && feet.x <= b.max.x && feet.z >= b.min.z && feet.z <= b.max.z;
        }
    }
}
