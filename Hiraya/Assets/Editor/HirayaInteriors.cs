using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Makes the buildings enterable:
    ///   1. Every school and house building prefab gets a door (BuildingDoor on its Entrance), placed where the door is drawn.
    ///   2. Two room prefabs (Assets/Prefabs/Interiors: Room_House and Room_School).
    ///   3. In each playing scene an "Interiors" object with one room per building, standing far away from the map.
    /// A room that is already in a scene is never replaced, so what you put in it stays. Move items into a room by dragging
    /// them under it in the Hierarchy (or just placing them on its floor).
    /// </summary>
    public static class HirayaInteriors
    {
        public const string PrefabFolder = "Assets/Prefabs/Interiors";
        const string MaterialFolder = "Assets/Interiors/Materials";

        const float RoomWidth = 16f;
        const float RoomDepth = 11f;
        const float WallHeight = 3.2f;
        const float ExitWidth = 3f;

        public static void BuildAll()
        {
            AddDoors();
            BuildRoomPrefabs();
            AddToScene(HirayaSceneSetup.KinderScenePath);
            AddToScene(HirayaSceneSetup.GradeOneScenePath);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ doors

        public static void AddDoors()
        {
            foreach (HirayaBuildingSetup.Plot plot in HirayaBuildingSetup.ReadPlots(HirayaBuildingSetup.ReadLayoutRows()))
            {
                if (plot.kind == 'K') continue;   // the park has no door
                string path = HirayaBuildingSetup.BuildingsFolder + "/" + plot.prefabName + ".prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                SetUpDoor(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Puts the Entrance where the door is drawn on the front picture and gives it a BuildingDoor.</summary>
        public static void SetUpDoor(GameObject building)
        {
            Transform entrance = building.transform.Find("Entrance");
            Transform front = building.transform.Find("Front");
            if (entrance == null || front == null) return;

            // Where the door is on each picture, as a share of the picture's width (from the left).
            float centre = 0.5f, width = 0.22f;
            var renderer = front.GetComponent<MeshRenderer>();
            string look = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : string.Empty;
            if (look.Contains("House1")) { centre = 0.273f; width = 0.182f; }
            else if (look.Contains("House2")) { centre = 0.30f; width = 0.20f; }

            float buildingWidth = front.localScale.x;
            Vector3 position = entrance.localPosition;
            position.x = (centre - 0.5f) * buildingWidth;
            entrance.localPosition = position;

            // The trigger reaches from just in front of the wall (the character stops there when walking up).
            var trigger = entrance.GetComponent<BoxCollider>();
            if (trigger == null) trigger = entrance.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            float wallZ = front.localPosition.z;
            trigger.size = new Vector3(Mathf.Max(1.8f, width * buildingWidth), 3f, 0.8f);
            trigger.center = new Vector3(0f, 1.5f, (wallZ - 0.4f) - position.z);

            if (entrance.GetComponent<BuildingDoor>() == null) entrance.gameObject.AddComponent<BuildingDoor>();
        }

        // ------------------------------------------------------------------ rooms

        public static void BuildRoomPrefabs()
        {
            HirayaCharacterSetup.EnsureFolder(PrefabFolder);
            HirayaCharacterSetup.EnsureFolder(MaterialFolder);
            BuildRoom("Room_House", Mat("HouseFloor", new Color(0.74f, 0.53f, 0.32f)), Mat("HouseWall", new Color(0.95f, 0.88f, 0.72f)),
                      Mat("HouseMat", new Color(0.70f, 0.25f, 0.20f)), "Building_House_1");
            BuildRoom("Room_School", Mat("SchoolFloor", new Color(0.80f, 0.78f, 0.70f)), Mat("SchoolWall", new Color(0.70f, 0.85f, 0.70f)),
                      Mat("SchoolMat", new Color(0.20f, 0.40f, 0.70f)), "Building_School");
        }

        static void BuildRoom(string name, Material floor, Material wall, Material mat, string buildingName)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;   // keep the user's version

            var root = new GameObject(name);
            var interior = root.AddComponent<Interior>();

            Block(root.transform, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(RoomWidth, 0.5f, RoomDepth), floor, true);
            float halfW = RoomWidth * 0.5f, halfD = RoomDepth * 0.5f;
            Block(root.transform, "WallBack", new Vector3(0f, WallHeight * 0.5f, halfD + 0.25f), new Vector3(RoomWidth + 1f, WallHeight, 0.5f), wall, true);
            Block(root.transform, "WallLeft", new Vector3(-halfW - 0.25f, WallHeight * 0.5f, 0f), new Vector3(0.5f, WallHeight, RoomDepth), wall, true);
            Block(root.transform, "WallRight", new Vector3(halfW + 0.25f, WallHeight * 0.5f, 0f), new Vector3(0.5f, WallHeight, RoomDepth), wall, true);
            // The front wall is low so the camera sees in, with a gap in the middle for the way out.
            float piece = (RoomWidth - ExitWidth) * 0.5f;
            float pieceX = ExitWidth * 0.5f + piece * 0.5f;
            Block(root.transform, "WallFrontLeft", new Vector3(-pieceX, 0.3f, -halfD - 0.25f), new Vector3(piece, 0.6f, 0.5f), wall, true);
            Block(root.transform, "WallFrontRight", new Vector3(pieceX, 0.3f, -halfD - 0.25f), new Vector3(piece, 0.6f, 0.5f), wall, true);
            Block(root.transform, "ExitMat", new Vector3(0f, 0.03f, -halfD + 0.6f), new Vector3(ExitWidth - 0.4f, 0.06f, 1.1f), mat, false);

            // Nothing to see, but the character cannot walk out into the empty space.
            var stop = new GameObject("ExitStop");
            stop.transform.SetParent(root.transform, false);
            stop.transform.localPosition = new Vector3(0f, 1f, -halfD - 0.75f);
            stop.AddComponent<BoxCollider>().size = new Vector3(ExitWidth + 1f, 2f, 0.5f);

            var exit = new GameObject("Exit");
            exit.transform.SetParent(root.transform, false);
            exit.transform.localPosition = new Vector3(0f, 1f, -halfD - 0.05f);
            var exitBox = exit.AddComponent<BoxCollider>();
            exitBox.isTrigger = true;
            exitBox.size = new Vector3(ExitWidth, 2f, 0.9f);

            var spawn = new GameObject("Spawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 0f, -halfD + 1.9f);

            var so = new SerializedObject(interior);
            so.FindProperty("buildingName").stringValue = buildingName;
            so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            so.FindProperty("exit").objectReferenceValue = exitBox;
            so.FindProperty("floorSize").vector2Value = new Vector2(RoomWidth, RoomDepth);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(block.GetComponent<BoxCollider>());
        }

        static Material Mat(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------ scenes

        /// <summary>Adds the "Interiors" object and any room that is missing. Existing rooms are not touched.</summary>
        public static void AddToScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("Interiors");
            if (root == null) root = new GameObject("Interiors");

            var buildings = new List<string>();
            foreach (HirayaBuildingSetup.Plot plot in HirayaBuildingSetup.ReadPlots(HirayaBuildingSetup.ReadLayoutRows()))
                if (plot.kind != 'K') buildings.Add(plot.prefabName);

            var school = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Room_School.prefab");
            var house = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Room_House.prefab");
            for (int i = 0; i < buildings.Count; i++)
            {
                string roomName = "Room_" + buildings[i];
                if (root.transform.Find(roomName) != null) continue;
                var room = (GameObject)PrefabUtility.InstantiatePrefab(buildings[i].Contains("School") ? school : house, root.transform);
                room.name = roomName;
                room.transform.position = new Vector3(300f + (i % 4) * 30f, 0f, (i / 4) * 25f);
                var so = new SerializedObject(room.GetComponent<Interior>());
                so.FindProperty("buildingName").stringValue = buildings[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
