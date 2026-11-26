using System.Collections.Generic;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// The door of a building, on its Entrance object. Walking up into it takes the character into the room (Interior)
    /// with the same Building Name as the building. A building without a room in the scene stays closed.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BuildingDoor : MonoBehaviour
    {
        static readonly List<BuildingDoor> all = new List<BuildingDoor>();
        public static IReadOnlyList<BuildingDoor> All => all;

        BoxCollider trigger;

        public string BuildingName => transform.parent != null ? transform.parent.name : name;

        public Interior Room => Interior.ForBuilding(BuildingName);

        BoxCollider Trigger
        {
            get
            {
                if (trigger == null) trigger = GetComponent<BoxCollider>();
                return trigger;
            }
        }

        /// <summary>Where the ring of the HUD goes when the goal is behind this door.</summary>
        public Vector3 DoorPosition => Trigger.bounds.center;

        /// <summary>Where the character stands after coming out: just in front of the door.</summary>
        public Vector3 OutsidePosition
        {
            get
            {
                Bounds b = Trigger.bounds;
                return new Vector3(b.center.x, 0f, b.min.z - 0.9f);
            }
        }

        public bool IsAtDoor(Vector3 feet) => Interior.Inside(Trigger, feet);

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => all.Clear();

        public static BuildingDoor ForBuilding(string name)
        {
            foreach (BuildingDoor door in all)
                if (door.BuildingName == name) return door;
            return null;
        }
    }
}
