using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Prepares the Fin / Fiona sprites, creates the
    /// CharacterSkin assets, the Player prefab and the on-screen D-pad prefab.
    /// Everything it makes is a normal asset / prefab you can open and edit afterwards.
    /// </summary>
    public static class HirayaCharacterSetup
    {
        public const string FinFolder = "Assets/2D asset/Fin";
        public const string FionaFolder = "Assets/2D asset/Fiona";
        public const string CharactersFolder = "Assets/Characters";
        public const string SpritesFolder = CharactersFolder + "/Sprites";
        public const string UiFolder = "Assets/UI";
        public const string PrefabsFolder = "Assets/Prefabs";

        public const string PlayerPrefabPath = PrefabsFolder + "/Player.prefab";
        public const string DPadPrefabPath = PrefabsFolder + "/DPadCanvas.prefab";

        /// <summary>How tall the character looks in the world, in Unity units.</summary>
        public const float CharacterHeight = 2.0f;

        /// <summary>The source art is 32 x 32 pixel art drawn on a 2048 canvas; the copies are 256 x 256 (8 px per art pixel).</summary>
        const int OutputSize = 256;

        static readonly string[] FinFiles =
        {
            "Character fin front", "Character back", "Character left side", "Character right side",
            "Walk front 1", "Walk front 2", "Walk back 1", "Walk back 2",
            "Walk left 1", "Walk left 2", "Walk right 1", "Walk right 2"
        };

        static readonly string[] FionaFiles =
        {
            "Fiona front", "Fiona back", "Fiona left side", "Fiona right side",
            "Fiona walk front 1", "Fiona walk front 2", "Fiona walk back 1", "Fiona walk back 2",
            "Fiona walk left 1", "Fiona walk left 2", "Fiona walk right 1", "Fiona walk right 2"
        };

        // ---------------------------------------------------------------- helpers

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------------------------------------------------------------- sprites + skins

        public static void SetupSkins()
        {
            EnsureFolder(CharactersFolder);
            EnsureFolder(SpritesFolder + "/Fin");
            EnsureFolder(SpritesFolder + "/Fiona");

            // The original PNGs have a solid white background. Make transparent copies (the originals are not touched)
            // and measure the union of all painted pixels so every frame gets the same pivot and scale.
            var jobs = new List<KeyValuePair<string, string>>();
            foreach (var f in FinFiles)
                jobs.Add(new KeyValuePair<string, string>(FinFolder + "/" + f + ".PNG", SpritesFolder + "/Fin/" + f + ".png"));
            foreach (var f in FionaFiles)
                jobs.Add(new KeyValuePair<string, string>(FionaFolder + "/" + f + ".PNG", SpritesFolder + "/Fiona/" + f + ".png"));

            int minY = int.MaxValue, maxY = int.MinValue, sourceSize = 0;
            foreach (var job in jobs)
            {
                MakeCleanCopy(job.Key, job.Value, out int y0, out int y1, out sourceSize);
                minY = Mathf.Min(minY, y0);
                maxY = Mathf.Max(maxY, y1);
            }
            AssetDatabase.Refresh();

            float artHeightInCopy = (maxY - minY + 1) * (OutputSize / (float)sourceSize);
            float pixelsPerUnit = artHeightInCopy / CharacterHeight;
            float pivotY = (float)minY / sourceSize; // the soles of the feet

            foreach (var job in jobs) ConfigureSprite(job.Value, pixelsPerUnit, pivotY);
            AssetDatabase.Refresh();

            MakeSkin("Fin", SpritesFolder + "/Fin", FinFiles);
            MakeSkin("Fiona", SpritesFolder + "/Fiona", FionaFiles);
            AssetDatabase.SaveAssets();
        }

        static void MakeCleanCopy(string sourcePath, string targetPath, out int minY, out int maxY, out int sourceSize)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(sourcePath));
            int w = tex.width;
            int h = tex.height;
            sourceSize = h;
            Color32[] pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);

            HirayaSpriteTools.ClearEdgeWhite(pixels, w, h);

            minY = int.MaxValue;
            maxY = int.MinValue;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a > 8)
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        break;
                    }
                }
            }
            if (minY == int.MaxValue) { minY = 0; maxY = h - 1; }

            // Nearest-neighbour shrink: every art pixel is a flat block, so this keeps the edges perfectly crisp.
            int factor = Mathf.Max(1, w / OutputSize);
            var small = new Texture2D(OutputSize, OutputSize, TextureFormat.RGBA32, false);
            var smallPixels = new Color32[OutputSize * OutputSize];
            for (int y = 0; y < OutputSize; y++)
                for (int x = 0; x < OutputSize; x++)
                    smallPixels[y * OutputSize + x] = pixels[(y * factor + factor / 2) * w + (x * factor + factor / 2)];
            small.SetPixels32(smallPixels);
            File.WriteAllBytes(targetPath, small.EncodeToPNG());
            Object.DestroyImmediate(small);
        }

        static void ConfigureSprite(string path, float pixelsPerUnit, float pivotY)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning("Hiraya: missing sprite file " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;          // keep the pixel art crisp
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, pivotY);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void MakeSkin(string skinName, string folder, string[] files)
        {
            string assetPath = CharactersFolder + "/" + skinName + ".asset";
            var skin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(assetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<CharacterSkin>();
                AssetDatabase.CreateAsset(skin, assetPath);
            }

            Sprite S(int i) => AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + files[i] + ".png");

            skin.displayName = skinName;
            skin.front = S(0); skin.back = S(1);
            skin.walkFront1 = S(4); skin.walkFront2 = S(5);
            skin.walkBack1 = S(6); skin.walkBack2 = S(7);

            // The artwork files are named from the character's own point of view: the "left side" and
            // "walk left" pictures actually look toward the RIGHT of the screen and the "right" ones look LEFT.
            // They are swapped here so that pressing Left shows a character that looks (and walks) left.
            skin.left = S(3); skin.right = S(2);
            skin.walkLeft1 = S(10); skin.walkLeft2 = S(11);
            skin.walkRight1 = S(8); skin.walkRight2 = S(9);
            EditorUtility.SetDirty(skin);
        }

        // ---------------------------------------------------------------- generated little textures

        static Sprite SaveSprite(string path, Texture2D tex, float pixelsPerUnit)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite BlobShadowSprite()
        {
            const string path = CharactersFolder + "/BlobShadow.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * 0.55f;
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            return SaveSprite(path, tex, size);
        }

        static Sprite CircleSprite()
        {
            const string path = UiFolder + "/DPadCircle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) * size * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return SaveSprite(path, tex, 100);
        }

        static Sprite ArrowSprite()
        {
            const string path = UiFolder + "/DPadArrow.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // upward pointing triangle
                    float fy = (y + 0.5f) / size;
                    float fx = (x + 0.5f) / size;
                    float top = 0.82f, bottom = 0.22f;
                    float halfWidth = Mathf.Lerp(0.30f, 0f, Mathf.InverseLerp(bottom, top, fy));
                    bool inside = fy >= bottom && fy <= top && Mathf.Abs(fx - 0.5f) <= halfWidth;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            return SaveSprite(path, tex, 100);
        }

        // ---------------------------------------------------------------- Player prefab

        public static GameObject CreatePlayerPrefab()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(CharactersFolder);

            var fin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(CharactersFolder + "/Fin.asset");
            var fiona = AssetDatabase.LoadAssetAtPath<CharacterSkin>(CharactersFolder + "/Fiona.asset");
            if (fin == null || fiona == null)
            {
                SetupSkins();
                fin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(CharactersFolder + "/Fin.asset");
                fiona = AssetDatabase.LoadAssetAtPath<CharacterSkin>(CharactersFolder + "/Fiona.asset");
            }

            var root = new GameObject("Player");
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = 1.0f;
            capsule.center = new Vector3(0f, 0.5f, 0f);
            capsule.sharedMaterial = SlipperyMaterial();

            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlaceStart>();   // starts in front of the building chosen on the world map

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root.transform, false);
            var spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = fin.front;
            spriteGo.AddComponent<BillboardToCamera>();

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowGo.transform.localScale = new Vector3(1.2f, 0.7f, 1f);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = BlobShadowSprite();
            shadow.sortingOrder = -1;

            var animator = root.AddComponent<CharacterSpriteAnimator>();
            var so = new SerializedObject(animator);
            so.FindProperty("player").objectReferenceValue = controller;
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("fin").objectReferenceValue = fin;
            so.FindProperty("fiona").objectReferenceValue = fiona;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static PhysicsMaterial SlipperyMaterial()
        {
            const string path = CharactersFolder + "/PlayerSlippery.asset";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat != null) return mat;
            mat = new PhysicsMaterial("PlayerSlippery")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------------------------------------------------------------- D-pad prefab

        public static GameObject CreateDPadPrefab()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(UiFolder);
            Sprite circle = CircleSprite();
            Sprite arrow = ArrowSprite();

            var canvasGo = new GameObject("DPadCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeArea));   // away from a camera notch
            safe.transform.SetParent(canvasGo.transform, false);
            var safeRect = (RectTransform)safe.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;

            var pad = new GameObject("DPad", typeof(RectTransform));
            pad.transform.SetParent(safe.transform, false);
            var padRect = (RectTransform)pad.transform;
            padRect.anchorMin = padRect.anchorMax = Vector2.zero;
            padRect.pivot = Vector2.zero;
            padRect.anchoredPosition = new Vector2(70f, 70f);
            padRect.sizeDelta = new Vector2(420f, 420f);

            MakeArrowButton(pad.transform, "Up", Facing.Up, new Vector2(210f, 350f), 0f, circle, arrow);
            MakeArrowButton(pad.transform, "Down", Facing.Down, new Vector2(210f, 70f), 180f, circle, arrow);
            MakeArrowButton(pad.transform, "Left", Facing.Left, new Vector2(70f, 210f), 90f, circle, arrow);
            MakeArrowButton(pad.transform, "Right", Facing.Right, new Vector2(350f, 210f), -90f, circle, arrow);

            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, DPadPrefabPath);
            Object.DestroyImmediate(canvasGo);
            return prefab;
        }

        static void MakeArrowButton(Transform parent, string name, Facing facing, Vector2 position, float arrowRotation, Sprite circle, Sprite arrow)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140f, 140f);

            var image = go.AddComponent<Image>();
            image.sprite = circle;
            image.color = new Color(1f, 1f, 1f, 0.55f);

            var button = go.AddComponent<DPadButton>();
            var so = new SerializedObject(button);
            so.FindProperty("direction").enumValueIndex = (int)facing;
            so.ApplyModifiedPropertiesWithoutUndo();

            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRect = (RectTransform)arrowGo.transform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(80f, 80f);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, arrowRotation);
            var arrowImage = arrowGo.AddComponent<Image>();
            arrowImage.sprite = arrow;
            arrowImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            arrowImage.raycastTarget = false;
        }
    }
}
