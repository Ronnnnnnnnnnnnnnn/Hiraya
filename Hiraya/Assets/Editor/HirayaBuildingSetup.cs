using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Makes one building prefab for every building plot
    /// in the layout (Assets/Map/HirayaMap.txt): K = park, S = school, H = house.
    ///
    /// Every prefab is built from plain objects you can edit:
    ///   Body      the box (walls)              Roof      a sloped roof on top (a flat slab on the park)
    ///   Front     your 2D picture of the wall (door and windows) covering the whole south face
    ///   Entrance  empty marker + trigger where the door is, ready for the "enterable building" work
    /// The box has a collider, so the player bumps into every side and the top.
    /// A building is only as big as its picture needs, so it sits in the middle of its plot with grass around it.
    /// </summary>
    public static class HirayaBuildingSetup
    {
        public const string BuildingsFolder = "Assets/Prefabs/Buildings";
        public const string TexturesFolder = "Assets/Buildings/Textures";
        public const string MaterialsFolder = "Assets/Buildings/Materials";
        public const string MeshesFolder = "Assets/Buildings/Meshes";

        const string House1Picture = "Assets/2D asset/Gamit/House 1.PNG";
        const string House2Picture = "Assets/2D asset/Gamit/House 2.PNG";
        const string SchoolPicture = "Assets/2D asset/Gamit/School (8).PNG";

        // Sizes in Unity units (the character is 2 tall).
        const float HouseWallHeight = 6.5f;
        const float HouseRoofHeight = 2.4f;
        const float SchoolWallHeight = 7.5f;
        const float SchoolRoofHeight = 3f;
        const float HouseDepthRatio = 0.6f;      // depth = width * this
        const float SchoolDepthRatio = 0.5f;
        const float ParkWidth = 12f;
        const float ParkDepth = 10f;
        const float ParkHeight = 2f;
        const float ParkSlabThickness = 0.3f;
        const float RoofOverhang = 0.5f;
        const float PlotInset = 0.75f;           // a building never gets closer than this to the edge of its plot

        /// <summary>
        /// The camera looks down steeply, so walls look squashed. The wall is made a bit narrower than its picture
        /// (a bit less wide, not as low) so the pictures still look natural from the game camera.
        /// </summary>
        const float PictureStretch = 0.7f;

        public class Plot
        {
            public char kind;                 // 'K' park, 'S' school, 'H' house
            public int row0, col0, row1, col1;
            public string prefabName;
            public Vector3 center;            // world position of the middle of the plot
            public float width, depth;        // free space inside the plot
        }

        // ------------------------------------------------------------------ plots from the layout

        public static string[] ReadLayoutRows()
        {
            var rows = new List<string>();
            foreach (string line in File.ReadAllLines(HirayaMapSetup.LayoutPath))
            {
                string trimmed = line.TrimEnd();
                if (trimmed.Length == 0 || trimmed.StartsWith(";")) continue;
                rows.Add(trimmed);
            }
            return rows.ToArray();
        }

        /// <summary>Finds every park / school / house rectangle in the layout.</summary>
        public static List<Plot> ReadPlots(string[] rows)
        {
            int n = rows.Length;
            var seen = new bool[n, rows[0].Length];
            var plots = new List<Plot>();

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char kind = rows[r][c];
                    if ((kind != 'K' && kind != 'S' && kind != 'H') || seen[r, c]) continue;

                    int r0 = r, r1 = r, c0 = c, c1 = c;
                    var stack = new Stack<Vector2Int>();
                    stack.Push(new Vector2Int(r, c));
                    seen[r, c] = true;
                    while (stack.Count > 0)
                    {
                        Vector2Int p = stack.Pop();
                        r0 = Mathf.Min(r0, p.x); r1 = Mathf.Max(r1, p.x);
                        c0 = Mathf.Min(c0, p.y); c1 = Mathf.Max(c1, p.y);
                        foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        {
                            int rr = p.x + d.x, cc = p.y + d.y;
                            if (rr < 0 || cc < 0 || rr >= n || cc >= rows[rr].Length) continue;
                            if (seen[rr, cc] || rows[rr][cc] != kind) continue;
                            seen[rr, cc] = true;
                            stack.Push(new Vector2Int(rr, cc));
                        }
                    }

                    Vector3 topLeft = HirayaMapSetup.CellToWorld(r0, c0);
                    Vector3 bottomRight = HirayaMapSetup.CellToWorld(r1, c1);
                    plots.Add(new Plot
                    {
                        kind = kind,
                        row0 = r0, col0 = c0, row1 = r1, col1 = c1,
                        center = (topLeft + bottomRight) * 0.5f,
                        width = (c1 - c0 + 1) * HirayaMapSetup.CellSize - 2f * PlotInset,
                        depth = (r1 - r0 + 1) * HirayaMapSetup.CellSize - 2f * PlotInset
                    });
                }
            }

            // Name them: Building_Park, Building_School, Building_House_1 ... (top to bottom, left to right).
            plots.Sort((a, b) => a.row0 != b.row0 ? a.row0.CompareTo(b.row0) : a.col0.CompareTo(b.col0));
            int house = 0;
            foreach (Plot plot in plots)
            {
                if (plot.kind == 'K') plot.prefabName = "Building_Park";
                else if (plot.kind == 'S') plot.prefabName = "Building_School";
                else plot.prefabName = "Building_House_" + (++house);
            }
            return plots;
        }

        // ------------------------------------------------------------------ prefabs

        public static void BuildBuildingPrefabs()
        {
            HirayaCharacterSetup.EnsureFolder(BuildingsFolder);
            HirayaCharacterSetup.EnsureFolder(TexturesFolder);
            HirayaCharacterSetup.EnsureFolder(MaterialsFolder);
            HirayaCharacterSetup.EnsureFolder(MeshesFolder);

            Picture house1 = MakePicture(House1Picture, "House 1");
            Picture house2 = MakePicture(House2Picture, "House 2");
            Picture school = MakePicture(SchoolPicture, "School");
            Mesh roofMesh = GabledRoofMesh();
            AssetDatabase.Refresh();

            // Wall and roof colours are taken from the pictures; change them in the material files if you like.
            var house1Look = new Look("House1", new Color(0.87f, 0.50f, 0.13f), new Color(0.44f, 0.07f, 0.07f), house1);
            var house2Look = new Look("House2", new Color(0.50f, 0.88f, 0.13f), new Color(0.87f, 0.13f, 0.13f), house2);
            var schoolLook = new Look("School", new Color(0.82f, 0.82f, 0.82f), new Color(0.69f, 0.53f, 0.38f), school);
            var parkLook = new Look("Park", new Color(0.94f, 0.94f, 0.56f), new Color(0.94f, 0.75f, 0.58f), null);

            int houseIndex = 0;
            foreach (Plot plot in ReadPlots(ReadLayoutRows()))
            {
                if (plot.kind == 'K') MakeParkPrefab(plot, parkLook);
                else if (plot.kind == 'S') MakeHousePrefab(plot, schoolLook, roofMesh, SchoolWallHeight, SchoolRoofHeight, SchoolDepthRatio);
                else MakeHousePrefab(plot, (++houseIndex) % 2 == 1 ? house1Look : house2Look, roofMesh, HouseWallHeight, HouseRoofHeight, HouseDepthRatio);
            }
            AssetDatabase.SaveAssets();
        }

        class Picture
        {
            public Texture2D texture;
            public float aspect;   // width / height of the wall art
        }

        class Look
        {
            public string name;
            public Color wall, roof;
            public Picture picture;

            public Look(string name, Color wall, Color roof, Picture picture)
            {
                this.name = name;
                this.wall = wall;
                this.roof = roof;
                this.picture = picture;
            }
        }

        // ------------------------------------------------------------------ the pictures

        /// <summary>
        /// The building PNGs have a solid white background and are 64 x 64 "big pixel" art that shows the whole
        /// house, roof included. This makes a small transparent copy of the WALL part only (the roof is built in 3D),
        /// with every big pixel eight texture pixels wide. Your PNG is not touched.
        /// </summary>
        static Picture MakePicture(string sourcePath, string name)
        {
            string targetPath = TexturesFolder + "/" + name + ".png";

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(sourcePath));
            int size = tex.width;
            Color32[] pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);

            const int grid = 64;
            int block = size / grid;
            var art = new Color32[grid * grid];
            for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                    art[y * grid + x] = pixels[(y * block + block / 2) * size + (x * block + block / 2)];

            HirayaSpriteTools.ClearEdgeWhite(art, grid, grid);

            Bounds2 bounds = PaintedBounds(art, grid, 0, grid - 1);

            // Rows count from the bottom (y = 0). Find the black line under the roof edge: the lowest row in the
            // upper part of the picture whose black pixels cover almost the full width. Everything below it is wall.
            int wallTop = bounds.maxY;
            int width = bounds.maxX - bounds.minX + 1;
            int height = bounds.maxY - bounds.minY + 1;
            int searchFloor = bounds.maxY - Mathf.RoundToInt(0.65f * height);
            for (int y = Mathf.Max(bounds.minY, searchFloor); y <= bounds.maxY; y++)
            {
                int black = 0;
                for (int x = bounds.minX; x <= bounds.maxX; x++)
                {
                    Color32 c = art[y * grid + x];
                    if (c.a > 0 && c.r < 24 && c.g < 24 && c.b < 24) black++;
                }
                if (black >= 0.85f * width) { wallTop = y; break; }   // the first one found from the lowest row up
            }
            bounds = PaintedBounds(art, grid, bounds.minY, wallTop);

            const int scale = 8;
            int w = bounds.maxX - bounds.minX + 1;
            int h = bounds.maxY - bounds.minY + 1;
            var output = new Color32[w * scale * h * scale];
            for (int y = 0; y < h * scale; y++)
                for (int x = 0; x < w * scale; x++)
                    output[y * w * scale + x] = art[(bounds.minY + y / scale) * grid + (bounds.minX + x / scale)];

            var outTex = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false);
            outTex.SetPixels32(output);
            File.WriteAllBytes(targetPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(targetPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();

            return new Picture
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath),
                aspect = (float)w / h
            };
        }

        struct Bounds2
        {
            public int minX, maxX, minY, maxY;
        }

        /// <summary>Box around the painted pixels, looking only at rows minRow..maxRow.</summary>
        static Bounds2 PaintedBounds(Color32[] art, int grid, int minRow, int maxRow)
        {
            var b = new Bounds2 { minX = grid, maxX = -1, minY = grid, maxY = -1 };
            for (int y = minRow; y <= maxRow; y++)
            {
                for (int x = 0; x < grid; x++)
                {
                    if (art[y * grid + x].a == 0) continue;
                    b.minX = Mathf.Min(b.minX, x); b.maxX = Mathf.Max(b.maxX, x);
                    b.minY = Mathf.Min(b.minY, y); b.maxY = Mathf.Max(b.maxY, y);
                }
            }
            return b;
        }

        // ------------------------------------------------------------------ materials

        static Material LitMaterial(string name, Color color)
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

        /// <summary>The building picture: unlit, so the pixel art keeps its exact colours; transparent where the art has no pixels.</summary>
        static Material PictureMaterial(string name, Texture2D texture)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = 2450;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ the sloped roof

        /// <summary>
        /// A roof shaped like a tent: width 1, depth 1, height 1, the ridge runs left to right so one slope
        /// faces the camera. It is scaled to each building. Every face has its own vertices (flat shading).
        /// </summary>
        static Mesh GabledRoofMesh()
        {
            Vector3 a = new Vector3(-0.5f, 0f, -0.5f);   // front bottom left
            Vector3 b = new Vector3(0.5f, 0f, -0.5f);    // front bottom right
            Vector3 c = new Vector3(0.5f, 1f, 0f);       // ridge right
            Vector3 d = new Vector3(-0.5f, 1f, 0f);      // ridge left
            Vector3 e = new Vector3(-0.5f, 0f, 0.5f);    // back bottom left
            Vector3 f = new Vector3(0.5f, 0f, 0.5f);     // back bottom right

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                int i = vertices.Count;
                vertices.AddRange(new[] { p0, p1, p2, p3 });
                triangles.AddRange(new[] { i, i + 1, i + 3, i + 1, i + 2, i + 3 });
            }

            void Tri(Vector3 p0, Vector3 p1, Vector3 p2)
            {
                int i = vertices.Count;
                vertices.AddRange(new[] { p0, p1, p2 });
                triangles.AddRange(new[] { i, i + 1, i + 2 });
            }

            Quad(a, d, c, b);   // front slope
            Quad(f, c, d, e);   // back slope
            Tri(a, e, d);       // left end
            Tri(b, c, f);       // right end

            var fresh = new Mesh { name = "GabledRoof" };
            fresh.SetVertices(vertices);
            fresh.SetTriangles(triangles, 0);
            fresh.RecalculateNormals();
            fresh.RecalculateBounds();

            // Keep the same mesh asset (and so its GUID) when the buildings are rebuilt.
            string path = MeshesFolder + "/GabledRoof.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }

        // ------------------------------------------------------------------ building prefabs

        static void MakeHousePrefab(Plot plot, Look look, Mesh roofMesh, float wallHeight, float roofHeight, float depthRatio)
        {
            // The wall is as wide as its picture needs, then it has to fit inside the plot.
            float w = Mathf.Min(plot.width, wallHeight * look.picture.aspect * PictureStretch);
            float d = Mathf.Min(plot.depth, w * depthRatio);

            var root = NewRoot(plot, w, wallHeight + roofHeight, d);
            AddBody(root, w, wallHeight, d, LitMaterial("Building_" + look.name + "_Wall", look.wall));

            var roof = new GameObject("Roof");
            roof.transform.SetParent(root.transform, false);
            roof.transform.localPosition = new Vector3(0f, wallHeight, 0f);
            roof.transform.localScale = new Vector3(w + 2f * RoofOverhang, roofHeight, d + 2f * RoofOverhang);
            roof.AddComponent<MeshFilter>().sharedMesh = roofMesh;
            roof.AddComponent<MeshRenderer>().sharedMaterial = LitMaterial("Building_" + look.name + "_Roof", look.roof);

            // The picture covers the whole south face (the side the camera sees).
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "Front";
            front.transform.SetParent(root.transform, false);
            front.transform.localPosition = new Vector3(0f, wallHeight * 0.5f, -d * 0.5f - 0.02f);
            front.transform.localScale = new Vector3(w, wallHeight, 1f);
            Object.DestroyImmediate(front.GetComponent<MeshCollider>());
            var renderer = front.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = PictureMaterial("Building_" + look.name + "_Picture", look.picture.texture);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AddEntrance(root, d);
            HirayaInteriors.SetUpDoor(root);
            Save(root, plot);
        }

        static void MakeParkPrefab(Plot plot, Look look)
        {
            // The park has no picture yet: a low plain box with a flat top, to be replaced.
            float w = Mathf.Min(plot.width, ParkWidth);
            float d = Mathf.Min(plot.depth, ParkDepth);

            var root = NewRoot(plot, w, ParkHeight + ParkSlabThickness, d);
            AddBody(root, w, ParkHeight, d, LitMaterial("Building_" + look.name + "_Wall", look.wall));

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Roof";
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, ParkHeight + ParkSlabThickness * 0.5f, 0f);
            top.transform.localScale = new Vector3(w + 2f * RoofOverhang, ParkSlabThickness, d + 2f * RoofOverhang);
            Object.DestroyImmediate(top.GetComponent<BoxCollider>());
            top.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial("Building_" + look.name + "_Roof", look.roof);

            AddEntrance(root, d);
            Save(root, plot);
        }

        static GameObject NewRoot(Plot plot, float width, float totalHeight, float depth)
        {
            var root = new GameObject(plot.prefabName);

            // One box collider for the whole building: the player bumps into every side and the top.
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, totalHeight * 0.5f, 0f);
            box.size = new Vector3(width, totalHeight, depth);
            return root;
        }

        static void AddBody(GameObject root, float w, float height, float d, Material wall)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(w, height, d);
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());
            body.GetComponent<MeshRenderer>().sharedMaterial = wall;
        }

        /// <summary>Where the door will be: an empty marker with a trigger in front of the building.</summary>
        static void AddEntrance(GameObject root, float depth)
        {
            var entrance = new GameObject("Entrance");
            entrance.transform.SetParent(root.transform, false);
            entrance.transform.localPosition = new Vector3(0f, 0f, -depth * 0.5f - 1.2f);
            var trigger = entrance.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            trigger.size = new Vector3(4f, 3f, 2.4f);
        }

        static void Save(GameObject root, Plot plot)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            PrefabUtility.SaveAsPrefabAsset(root, BuildingsFolder + "/" + plot.prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
