using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Turns the layout text file (Assets/Map/HirayaMap.txt)
    /// into real GameObjects: it makes the Tree / Pine / Rock / Fence / Path prefabs and then ONE
    /// "HirayaMap" prefab that holds every placed prefab as an ordinary child you can see and move
    /// in the Hierarchy. Nothing is spawned by code while the game runs.
    /// Buildings are intentionally left out (cells marked B stay empty grass).
    /// </summary>
    public static class HirayaMapSetup
    {
        public const string LayoutPath = "Assets/Map/HirayaMap.txt";
        public const string EnvironmentFolder = "Assets/Prefabs/Environment";
        public const string MaterialsFolder = "Assets/Map/Materials";
        public const string MapPrefabPath = "Assets/Prefabs/HirayaMap.prefab";

        const string TreeFbx = "Assets/3D asset/tree.fbx";
        const string PineFbx = "Assets/3D asset/pine tree.fbx";
        const string RockFbx = "Assets/3D asset/rocks.fbx";
        const string FenceFbx = "Assets/3D asset/fence.fbx";

        /// <summary>The layout is 80 x 80 cells; one cell is this many Unity units.</summary>
        public const int Cells = 80;
        public const float CellSize = 1.5f;

        public static float MapHalfSize => Cells * CellSize * 0.5f;

        // Target sizes in Unity units.
        const float TreeHeight = 3.8f;
        const float PineHeight = 4.2f;
        const float BoulderWidth = 1.7f;          // how wide a boulder is (before the random size on each one)
        const float BoulderHeightStretch = 2.0f;  // the stones in rocks.fbx are flat, this makes them chunky
        const float FenceHeight = 1.4f;
        const float PathThickness = 0.08f;

        // Where the player starts (row, column in the layout): the big open path area.
        const int SpawnRow = 22;
        const int SpawnColumn = 45;

        /// <summary>Layout cell centre -> world position (row 0 is the north / top edge, +Z).</summary>
        public static Vector3 CellToWorld(int row, int col)
        {
            return new Vector3((col + 0.5f - Cells * 0.5f) * CellSize, 0f, (Cells * 0.5f - (row + 0.5f)) * CellSize);
        }

        public static Vector3 SpawnWorldPosition()
        {
            return CellToWorld(SpawnRow, SpawnColumn) + new Vector3(0f, 0.15f, 0f);
        }

        // ------------------------------------------------------------------ menu entry points

        public static void BuildMap()
        {
            BuildEnvironmentPrefabs();
            HirayaBuildingSetup.BuildBuildingPrefabs();
            BuildMapPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ environment prefabs

        public static void BuildEnvironmentPrefabs()
        {
            HirayaCharacterSetup.EnsureFolder(EnvironmentFolder);
            HirayaCharacterSetup.EnsureFolder(MaterialsFolder);

            // tree.fbx holds two different trees, pine tree.fbx a grid of identical pines,
            // rocks.fbx patches made of hundreds of loose stones (three of them become the boulders),
            // fence.fbx three fence sections.
            MakeModelPrefab("Tree_A", TreeFbx, "pPlatonic20", TreeHeight, true, ColliderKind.Trunk);
            MakeModelPrefab("Tree_B", TreeFbx, "pPlatonic21", TreeHeight, true, ColliderKind.Trunk);
            MakeModelPrefab("PineTree", PineFbx, "polySurface1", PineHeight, true, ColliderKind.Trunk);
            MakeBoulderPrefabs();
            MakeFencePrefab();
            MakePathPrefab();
        }

        enum ColliderKind { None, Trunk, Box }

        class Piece
        {
            public Mesh mesh;
            public Material[] materials;
            public Matrix4x4 toRoot;
        }

        /// <summary>
        /// The FBX files are whole Maya scenes (many copies / variants in one file). This instantiates the
        /// FBX temporarily and returns the mesh, materials and transform of the one renderer called <paramref name="pieceName"/>.
        /// </summary>
        static Piece ReadPiece(string fbxPath, string pieceName)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var temp = (GameObject)Object.Instantiate(fbx);
            try
            {
                MeshRenderer r = null;
                foreach (var candidate in temp.GetComponentsInChildren<MeshRenderer>())
                {
                    if (candidate.name == pieceName) { r = candidate; break; }
                }
                if (r == null) throw new System.Exception("Hiraya: '" + pieceName + "' not found in " + fbxPath);
                var piece = new Piece
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    materials = r.sharedMaterials,
                    toRoot = temp.transform.worldToLocalMatrix * r.transform.localToWorldMatrix
                };
                return piece;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        static GameObject BuildModelObject(string name, Piece piece, float targetSize, bool sizeByHeight, out Bounds bounds, float heightStretch = 1f)
        {
            var root = new GameObject(name);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = piece.mesh;
            var renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = piece.materials;

            model.transform.localPosition = piece.toRoot.GetColumn(3);
            model.transform.localRotation = piece.toRoot.rotation;
            model.transform.localScale = piece.toRoot.lossyScale;

            Bounds first = WorldBounds(piece.mesh, model.transform);
            float current = sizeByHeight ? first.size.y : Mathf.Max(first.size.x, first.size.z);
            float k = targetSize / Mathf.Max(0.0001f, current);
            model.transform.localScale *= k;
            model.transform.localPosition *= k;
            if (!Mathf.Approximately(heightStretch, 1f))
            {
                Vector3 s = model.transform.localScale;
                model.transform.localScale = new Vector3(s.x, s.y * heightStretch, s.z);
            }

            // Stand the model on the ground, centred on the prefab's origin.
            Bounds scaled = WorldBounds(piece.mesh, model.transform);
            model.transform.localPosition -= new Vector3(scaled.center.x, scaled.min.y, scaled.center.z);

            bounds = WorldBounds(piece.mesh, model.transform);
            return root;
        }

        /// <summary>Bounds of a mesh after the given transform (the prefab root sits at the origin, so this is root space).</summary>
        static Bounds WorldBounds(Mesh mesh, Transform transform)
        {
            Bounds local = mesh.bounds;
            Matrix4x4 m = transform.localToWorldMatrix;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        static void MakeModelPrefab(string name, string fbxPath, string pieceName, float targetSize, bool sizeByHeight, ColliderKind collider)
        {
            Piece piece = ReadPiece(fbxPath, pieceName);
            GameObject root = BuildModelObject(name, piece, targetSize, sizeByHeight, out Bounds bounds);

            if (collider == ColliderKind.Trunk)
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = 0.28f;
                capsule.height = Mathf.Min(2.2f, bounds.size.y);
                capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            }
            else if (collider == ColliderKind.Box)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
                box.size = new Vector3(bounds.size.x * 0.8f, bounds.size.y, bounds.size.z * 0.8f);
            }

            MakeStatic(root);
            SavePrefab(root, name);
        }

        // ------------------------------------------------------------------ boulders cut out of rocks.fbx

        public const string MeshesFolder = "Assets/Map/Meshes";

        /// <summary>One loose stone inside a mesh that holds a whole patch of stones.</summary>
        public class Stone
        {
            public readonly List<int> triangles = new List<int>();
            public Bounds bounds;
            public int TriangleCount => triangles.Count / 3;
        }

        /// <summary>
        /// rocks.fbx has no single rocks: every mesh is a patch of hundreds of separate stones.
        /// This finds the separate stones (triangles that touch each other belong to one stone),
        /// biggest first.
        /// </summary>
        public static List<Stone> SplitIntoStones(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;

            var parent = new int[vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }

            void Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);
                if (a != b) parent[a] = b;
            }

            // Vertices stored twice at the same spot still belong to the same stone.
            var firstAt = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertices[i].x * 1e6f),
                    Mathf.RoundToInt(vertices[i].y * 1e6f),
                    Mathf.RoundToInt(vertices[i].z * 1e6f));
                if (firstAt.TryGetValue(key, out int other)) Union(i, other);
                else firstAt[key] = i;
            }
            for (int i = 0; i < indices.Length; i += 3)
            {
                Union(indices[i], indices[i + 1]);
                Union(indices[i], indices[i + 2]);
            }

            var stones = new Dictionary<int, Stone>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                int root = Find(indices[i]);
                if (!stones.TryGetValue(root, out Stone stone))
                {
                    stone = new Stone();
                    stones[root] = stone;
                }
                stone.triangles.Add(indices[i]);
                stone.triangles.Add(indices[i + 1]);
                stone.triangles.Add(indices[i + 2]);
            }

            var result = new List<Stone>(stones.Values);
            foreach (Stone stone in result)
            {
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (int index in stone.triangles)
                {
                    min = Vector3.Min(min, vertices[index]);
                    max = Vector3.Max(max, vertices[index]);
                }
                stone.bounds = new Bounds();
                stone.bounds.SetMinMax(min, max);
            }

            result.Sort((a, b) => Footprint(b).CompareTo(Footprint(a)));
            return result;
        }

        static float Footprint(Stone stone)
        {
            return Mathf.Max(stone.bounds.size.x, stone.bounds.size.z);
        }

        /// <summary>A new mesh that contains only the given stone, centred and standing on y = 0.</summary>
        public static Mesh BuildStoneMesh(Mesh source, Stone stone, string name)
        {
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;

            var map = new Dictionary<int, int>();
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newUvs = new List<Vector2>();
            var newTriangles = new List<int>();

            foreach (int index in stone.triangles)
            {
                if (!map.TryGetValue(index, out int mapped))
                {
                    mapped = newVertices.Count;
                    map[index] = mapped;
                    newVertices.Add(vertices[index]);
                    if (normals.Length == vertices.Length) newNormals.Add(normals[index]);
                    if (uvs.Length == vertices.Length) newUvs.Add(uvs[index]);
                }
                newTriangles.Add(mapped);
            }

            Bounds b = stone.bounds;
            var offset = new Vector3(b.center.x, b.min.y, b.center.z);
            for (int i = 0; i < newVertices.Count; i++) newVertices[i] -= offset;

            var mesh = new Mesh { name = name };
            mesh.SetVertices(newVertices);
            mesh.SetTriangles(newTriangles, 0);
            if (newNormals.Count == newVertices.Count) mesh.SetNormals(newNormals); else mesh.RecalculateNormals();
            if (newUvs.Count == newVertices.Count) mesh.SetUVs(0, newUvs);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void MakeBoulderPrefabs()
        {
            HirayaCharacterSetup.EnsureFolder(MeshesFolder);
            Material gray = GetMaterial("RockGray", new Color(0.62f, 0.62f, 0.60f));
            Material warm = GetMaterial("RockWarm", new Color(0.66f, 0.58f, 0.48f));

            // Three different stones: a chunky 32-triangle one, a rounder 124-triangle one and a 76-triangle one.
            MakeBoulder("Rock_A", "pCube1149", 32, gray);
            MakeBoulder("Rock_B", "pCube1150", 124, warm);
            MakeBoulder("Rock_C", "pCube1150", 76, gray);
        }

        static void MakeBoulder(string prefabName, string patchName, int triangleCount, Material material)
        {
            Piece patch = ReadPiece(RockFbx, patchName);
            Stone stone = SplitIntoStones(patch.mesh).Find(s => s.TriangleCount == triangleCount);
            if (stone == null) throw new System.Exception("Hiraya: no stone with " + triangleCount + " triangles in " + patchName);

            // Keep the same mesh asset (and so its GUID) when the map is rebuilt.
            Mesh fresh = BuildStoneMesh(patch.mesh, stone, prefabName);
            string meshPath = MeshesFolder + "/" + prefabName + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                AssetDatabase.CreateAsset(fresh, meshPath);
                mesh = fresh;
            }
            else
            {
                EditorUtility.CopySerialized(fresh, mesh);
                Object.DestroyImmediate(fresh);
            }

            var piece = new Piece { mesh = mesh, materials = new[] { material }, toRoot = Matrix4x4.identity };
            GameObject root = BuildModelObject(prefabName, piece, BoulderWidth, false, out Bounds bounds, BoulderHeightStretch);

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            box.size = new Vector3(bounds.size.x * 0.85f, bounds.size.y, bounds.size.z * 0.85f);

            MakeStatic(root);
            SavePrefab(root, prefabName);
        }

        /// <summary>Text report of the biggest stones of a patch (used to pick the boulders).</summary>
        public static string DescribeStones(string fbxPath, string pieceName, int count)
        {
            Piece piece = ReadPiece(fbxPath, pieceName);
            List<Stone> stones = SplitIntoStones(piece.mesh);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(pieceName + ": " + stones.Count + " stones, patch bounds " + piece.mesh.bounds.size.ToString("F3"));
            for (int i = 0; i < Mathf.Min(count, stones.Count); i++)
                sb.AppendLine("  #" + i + " tris " + stones[i].TriangleCount + " size " + stones[i].bounds.size.ToString("F4"));
            int mid = stones.Count / 2;
            sb.AppendLine("  median stone #" + mid + " tris " + stones[mid].TriangleCount + " size " + stones[mid].bounds.size.ToString("F4"));
            return sb.ToString();
        }

        static void MakeFencePrefab()
        {
            Piece piece = ReadPiece(FenceFbx, "pCube31");
            GameObject root = BuildModelObject("FenceSegment", piece, FenceHeight, true, out Bounds bounds);

            // A fence piece is long along X.
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            box.size = new Vector3(bounds.size.x, bounds.size.y, Mathf.Max(0.4f, bounds.size.z));

            MakeStatic(root);
            SavePrefab(root, "FenceSegment");
        }

        static void MakePathPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PathSegment";
            // Just the walkable surface: the ground below already has a collider.
            Object.DestroyImmediate(root.GetComponent<BoxCollider>());
            root.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial("Path", new Color(0.90f, 0.82f, 0.58f));
            MakeStatic(root);
            SavePrefab(root, "PathSegment");
        }

        static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, EnvironmentFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        static void MakeStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static Material GetMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ the map prefab

        static GameObject LoadEnvironment(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentFolder + "/" + name + ".prefab");
        }

        static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float yaw, float scale, string label)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = label;
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static void BuildMapPrefab()
        {
            string[] rows = ReadLayout();

            var root = new GameObject("HirayaMap");
            Transform ground = NewGroup(root.transform, "Ground");
            Transform paths = NewGroup(root.transform, "Paths");
            Transform fence = NewGroup(root.transform, "Fence");
            Transform trees = NewGroup(root.transform, "Trees");
            Transform pines = NewGroup(root.transform, "PineTrees");
            Transform rocks = NewGroup(root.transform, "Rocks");

            // Ground: a big flat box with a grass colour that the character can walk on.
            // It reaches 40 units past the fence so the camera never sees empty sky outside the map.
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundGo.name = "GrassGround";
            groundGo.transform.SetParent(ground, false);
            float size = Cells * CellSize + 80f;
            groundGo.transform.localScale = new Vector3(size, 1f, size);
            groundGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundGo.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial("Grass", new Color(0.42f, 0.74f, 0.36f));
            GameObjectUtility.SetStaticEditorFlags(groundGo, StaticEditorFlags.BatchingStatic);

            var treePrefabs = new[] { LoadEnvironment("Tree_A"), LoadEnvironment("Tree_B") };
            var pinePrefab = LoadEnvironment("PineTree");
            var rockPrefabs = new[] { LoadEnvironment("Rock_A"), LoadEnvironment("Rock_B"), LoadEnvironment("Rock_C") };
            var pathPrefab = LoadEnvironment("PathSegment");
            var fencePrefab = LoadEnvironment("FenceSegment");

            var rng = new System.Random(12345);
            var pathRng = new System.Random(777);
            int treeCount = 0, pineCount = 0, rockCount = 0, pathRockCount = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char ch = rows[r][c];
                    Vector3 pos = CellToWorld(r, c);
                    switch (ch)
                    {
                        case 'T':
                            Place(treePrefabs[rng.Next(treePrefabs.Length)], trees, pos, rng.Next(0, 360), 0.9f + (float)rng.NextDouble() * 0.25f, "Tree_" + (++treeCount));
                            break;
                        case 'Y':
                            Place(pinePrefab, pines, pos, rng.Next(0, 360), 0.9f + (float)rng.NextDouble() * 0.25f, "Pine_" + (++pineCount));
                            break;
                        case 'R':
                            Place(rockPrefabs[rng.Next(rockPrefabs.Length)], rocks, pos, rng.Next(0, 360), 0.8f + (float)rng.NextDouble() * 0.5f, "Rock_" + (++rockCount));
                            break;
                        case 'r':
                            // A rock lying on the walk path: sits on top of the path surface. It has its own
                            // random generator so the trees and grass rocks keep exactly the same look.
                            pos.y = PathThickness - 0.005f;
                            Place(rockPrefabs[pathRng.Next(rockPrefabs.Length)], rocks, pos, pathRng.Next(0, 360), 0.9f + (float)pathRng.NextDouble() * 0.4f, "PathRock_" + (++pathRockCount));
                            break;
                    }
                }
            }

            BuildPaths(rows, paths, pathPrefab);
            BuildFence(fence, fencePrefab);
            PlaceBuildings(rows, NewGroup(root.transform, "Buildings"));

            PrefabUtility.SaveAsPrefabAsset(root, MapPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("Hiraya: map prefab built - " + treeCount + " trees, " + pineCount + " pines, " + rockCount + " rocks on grass, " + pathRockCount + " rocks on the path.");
        }

        /// <summary>One building prefab per park / school / house plot of the layout (see HirayaBuildingSetup).</summary>
        static void PlaceBuildings(string[] rows, Transform parent)
        {
            foreach (HirayaBuildingSetup.Plot plot in HirayaBuildingSetup.ReadPlots(rows))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaBuildingSetup.BuildingsFolder + "/" + plot.prefabName + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = plot.prefabName;
                go.transform.localPosition = new Vector3(plot.center.x, 0f, plot.center.z);
            }
        }

        static Transform NewGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static string[] ReadLayout()
        {
            var rows = new List<string>();
            foreach (string line in File.ReadAllLines(LayoutPath))
            {
                string trimmed = line.TrimEnd();
                if (trimmed.Length == 0 || trimmed.StartsWith(";")) continue;
                rows.Add(trimmed);
            }
            return rows.ToArray();
        }

        /// <summary>'=' is walk path; 'r' is a rock lying on the walk path, so the path continues under it.</summary>
        static bool IsPath(char c)
        {
            return c == '=' || c == 'r';
        }

        /// <summary>Merges the path cells into a small number of rectangles, one PathSegment each.</summary>
        static void BuildPaths(string[] rows, Transform parent, GameObject pathPrefab)
        {
            int n = rows.Length;
            var used = new bool[n, rows[0].Length];
            int count = 0;
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (!IsPath(rows[r][c]) || used[r, c]) continue;

                    int width = 1;
                    while (c + width < rows[r].Length && IsPath(rows[r][c + width]) && !used[r, c + width]) width++;

                    int height = 1;
                    bool grow = true;
                    while (grow && r + height < n)
                    {
                        for (int x = c; x < c + width; x++)
                        {
                            if (!IsPath(rows[r + height][x]) || used[r + height, x]) { grow = false; break; }
                        }
                        if (grow) height++;
                    }

                    for (int y = r; y < r + height; y++)
                        for (int x = c; x < c + width; x++)
                            used[y, x] = true;

                    Vector3 topLeft = CellToWorld(r, c);
                    Vector3 bottomRight = CellToWorld(r + height - 1, c + width - 1);
                    Vector3 centre = (topLeft + bottomRight) * 0.5f;

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pathPrefab, parent);
                    go.name = "Path_" + (++count);
                    go.transform.localPosition = new Vector3(centre.x, PathThickness * 0.5f - 0.005f, centre.z);
                    go.transform.localScale = new Vector3(width * CellSize, PathThickness, height * CellSize);
                }
            }
        }

        /// <summary>Fence along the outer edge of the map: pieces are stretched a little so they close each side exactly.</summary>
        static void BuildFence(Transform parent, GameObject fencePrefab)
        {
            float pieceLength = MeasureLength(fencePrefab);
            float sideLength = Cells * CellSize;
            int piecesPerSide = Mathf.Max(1, Mathf.RoundToInt(sideLength / pieceLength));
            float stretch = sideLength / (piecesPerSide * pieceLength);
            float edge = (Cells * 0.5f - 0.5f) * CellSize; // centre of the outer ring of cells

            int count = 0;
            for (int i = 0; i < piecesPerSide; i++)
            {
                float along = -sideLength * 0.5f + (i + 0.5f) * (sideLength / piecesPerSide);
                AddFence(parent, fencePrefab, new Vector3(along, 0f, edge), 0f, stretch, ++count);      // north
                AddFence(parent, fencePrefab, new Vector3(along, 0f, -edge), 0f, stretch, ++count);     // south
                AddFence(parent, fencePrefab, new Vector3(edge, 0f, along), 90f, stretch, ++count);     // east
                AddFence(parent, fencePrefab, new Vector3(-edge, 0f, along), 90f, stretch, ++count);    // west
            }
        }

        static float MeasureLength(GameObject fencePrefab)
        {
            var box = fencePrefab.GetComponent<BoxCollider>();
            return box != null ? box.size.x : 3f;
        }

        static void AddFence(Transform parent, GameObject prefab, Vector3 position, float yaw, float stretch, int index)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "Fence_" + index;
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(stretch, 1f, 1f);
        }
    }
}
