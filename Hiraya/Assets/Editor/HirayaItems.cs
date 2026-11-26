using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Makes one prefab for every item that can be found:
    /// Assets/Prefabs/Items/Item_Chair, Item_Bed, ... Drag them into a playing scene (or into a room under "Interiors").
    /// Each prefab has a WordTarget with the Filipino word, its recording, the picture, the clue and two wrong answers,
    /// a collider the character bumps into, and your 3D model when there is one (desk, chalk, eraser, bottle) or your
    /// picture standing up facing the camera.
    /// The pictures are copied smaller into Assets/UI/Items (your originals are not touched).
    /// Prefabs that already exist are left as they are, so your changes to them are kept.
    /// </summary>
    public static class HirayaItems
    {
        public const string PrefabFolder = "Assets/Prefabs/Items";
        public const string SpriteFolder = "Assets/UI/Items";
        const string PictureFolder = "Assets/2D asset/Gamit";
        const string SoundFolder = "Assets/Sound/Interactable(Sound)";
        const string ModelFolder = "Assets/3D asset";
        const string MaterialFolder = "Assets/Map/Materials";

        public class Item
        {
            public string name;          // prefab name part
            public string word;          // Filipino
            public string english;       // meaning
            public string place;         // paaralan / tahanan
            public string picture;       // file in 2D asset/Gamit
            public string sound;         // file in Sound/Interactable(Sound), null = no recording
            public string model;         // file in 3D asset, null = use the picture
            public float height;         // size in the world (the character is 2 tall)
            public string hint, hintEnglish;
            public string category, categoryEnglish;
            public Color modelColor;      // one plain colour; leave it out to keep the model's own materials
            public float yaw;             // turned so its front faces the camera
            public float width;           // size by width instead of height (long low things)
            public bool lieDown;          // a model that was made standing up (the pencil)
            public float thicken = 1f;    // fatter, so a thin model can be seen

            public Item Pose(float yaw = 0f, float width = 0f, bool lieDown = false, float thicken = 1f)
            {
                this.yaw = yaw;
                this.width = width;
                this.lieDown = lieDown;
                this.thicken = thicken;
                return this;
            }
        }

        static Item New(string name, string word, string english, string place, string picture, string sound, float height,
                        string category, string categoryEnglish, string hint, string hintEnglish, string model = null, Color modelColor = default)
        {
            return new Item
            {
                name = name, word = word, english = english, place = place, picture = picture, sound = sound, height = height,
                category = category, categoryEnglish = categoryEnglish, hint = hint, hintEnglish = hintEnglish, model = model, modelColor = modelColor
            };
        }

        /// <summary>Every item of the word list, school first (7 words) and then home (4 words).</summary>
        public static readonly Item[] All =
        {
            New("Chair", "upuan", "chair", "paaralan", "chair.png", "Chair.mp3", 1.6f, "kagamitan", "furniture", "Inuupuan", "You sit on it", "chair.fbx").Pose(yaw: 125f),
            New("Desk", "hapag-sulatan", "desk", "paaralan", "desk.png", "Desk.mp3", 1.3f, "gamit sa paaralan", "school thing", "Sinusulatan ng mag-aaral", "You write on it at school", "desk.fbx", new Color(0.72f, 0.48f, 0.26f)),
            New("Blackboard", "pisara", "blackboard", "paaralan", "blackboard.png", "Chalkboard or blackboard.mp3", 2.2f, "gamit sa paaralan", "school thing", "Sinusulatan ng guro", "The teacher writes on it", "blackboard.fbx").Pose(yaw: 90f, width: 3.2f),
            New("Chalk", "tisa", "chalk", "paaralan", "chalk.png", "Chalk.mp3", 0.9f, "gamit sa paaralan", "school thing", "Pansulat sa pisara", "You write on the blackboard with it", "chalk.fbx", new Color(0.96f, 0.96f, 0.94f)),
            New("Eraser", "pambura", "eraser", "paaralan", "eraser.png", "Eraser.mp3", 0.22f, "gamit sa paaralan", "school thing", "Pambura ng sulat", "It rubs out writing", "eraser.fbx", new Color(0.93f, 0.55f, 0.60f)),
            New("Pencil", "lapis", "pencil", "paaralan", "pencil.png", "Pencil.mp3", 0.9f, "gamit sa paaralan", "school thing", "Panulat", "You write with it", "pencil.fbx").Pose(yaw: 40f, width: 1.4f, lieDown: true, thicken: 3f),
            New("Bottle", "bote", "bottle", "paaralan", "waterbottle.png", "Bottle.mp3", 1.0f, "gamit", "thing", "Lalagyan ng tubig", "It holds water", "bottle water.fbx", new Color(0.45f, 0.72f, 0.95f)),
            New("Bed", "kama", "bed", "tahanan", "bed.png", "Bed.mp3", 1.4f, "kagamitan", "furniture", "Tinutulugan", "You sleep on it", "bed.fbx").Pose(width: 3f),
            New("Table", "hapag", "table", "tahanan", "table.png", "Desk or table.mp3", 1.3f, "kagamitan", "furniture", "Kainan ng pamilya", "The family eats on it", "table.fbx").Pose(yaw: 90f, width: 2.1f),
            New("TV", "telebisyon", "TV", "tahanan", "TV.png", "TV.mp3", 1.4f, "kasangkapan", "appliance", "Pinapanood", "You watch it", "tv.fbx").Pose(yaw: -90f, width: 2.2f),
            New("Wardrobe", "aparador", "wardrobe", "tahanan", "wardrobe.png", "Wardrobe or closet.mp3", 2.6f, "kagamitan", "furniture", "Taguan ng damit", "Clothes are kept in it", "wardrobe.fbx").Pose(yaw: 90f, width: 2.1f)
        };

        /// <summary>
        /// The texts for 6 years old and up: word, description, example, example in English, fill in the blank,
        /// the blank in English, and two words that do not fit the blank. The sentences are mine: have a Filipino or
        /// preschool teacher check them, and edit them in Assets/Data/WordCatalog.asset.
        /// </summary>
        public static readonly string[][] Sentences =
        {
            new[] { "upuan", "A chair is a seat with a back, made for one person.",
                    "Umupo si Ana sa upuan.", "Ana sat on the chair.",
                    "Pagod ako, kaya umupo ako sa ___.", "I was tired, so I sat on the ___.", "pisara", "tisa" },
            new[] { "hapag-sulatan", "A school desk is a small table where a pupil reads and writes.",
                    "Malinis ang hapag-sulatan ni Ben.", "Ben's desk is clean.",
                    "Inilagay ko ang aklat sa ibabaw ng aking ___.", "I put the book on top of my ___.", "tisa", "lapis" },
            new[] { "pisara", "A blackboard is a big dark board that the teacher writes on with chalk.",
                    "Sumulat ang guro sa pisara.", "The teacher wrote on the blackboard.",
                    "Basahin natin ang nakasulat sa ___.", "Let us read what is written on the ___.", "bote", "upuan" },
            new[] { "tisa", "Chalk is a small stick used to write on a blackboard.",
                    "Puti ang tisa ng guro.", "The teacher's chalk is white.",
                    "Sumulat si Marco sa pisara gamit ang ___.", "Marco wrote on the blackboard with ___.", "bote", "upuan" },
            new[] { "pambura", "An eraser removes writing from paper or from the board.",
                    "May pambura sa loob ng bag ko.", "There is an eraser inside my bag.",
                    "Mali ang isinulat ko, kaya ginamit ko ang ___.", "I wrote something wrong, so I used the ___.", "bote", "upuan" },
            new[] { "lapis", "A pencil is a thin stick used for writing and drawing.",
                    "Gumuhit ako ng bulaklak gamit ang lapis.", "I drew a flower with a pencil.",
                    "Tinasahan ko ang aking ___ para makasulat.", "I sharpened my ___ so I could write.", "pisara", "bote" },
            new[] { "bote", "A bottle is a container that holds water or other drinks.",
                    "Puno ng tubig ang bote.", "The bottle is full of water.",
                    "Nauhaw ako, kaya uminom ako ng tubig mula sa ___.", "I was thirsty, so I drank water from the ___.", "upuan", "tisa" },
            new[] { "kama", "A bed is a piece of furniture that people sleep on.",
                    "Malambot ang aking kama.", "My bed is soft.",
                    "Gabi na, kaya matutulog na ako sa ___.", "It is night, so I will sleep on the ___.", "telebisyon", "aparador" },
            new[] { "hapag", "A table has a flat top and legs. We put food and other things on it.",
                    "Nasa hapag ang pagkain.", "The food is on the table.",
                    "Sabay-sabay kaming kumain sa ___.", "We ate together at the ___.", "aparador", "kama" },
            new[] { "telebisyon", "A television shows moving pictures with sound.",
                    "Nanonood kami ng telebisyon tuwing gabi.", "We watch television every night.",
                    "Binuksan ni Tatay ang ___ para manood ng balita.", "Father turned on the ___ to watch the news.", "kama", "hapag" },
            new[] { "aparador", "A wardrobe is a tall cabinet where clothes are kept.",
                    "Nasa loob ng aparador ang damit ko.", "My clothes are inside the wardrobe.",
                    "Tiniklop ko ang damit at inilagay sa ___.", "I folded the clothes and put them in the ___.", "telebisyon", "hapag" }
        };

        public static void BuildAll()
        {
            HirayaCharacterSetup.EnsureFolder(PrefabFolder);
            HirayaCharacterSetup.EnsureFolder(SpriteFolder);
            var sprites = new Dictionary<string, Sprite>();
            foreach (Item item in All) sprites[item.name] = MakeSprite(item);
            foreach (Item item in All) MakePrefab(item, sprites);
            AssetDatabase.SaveAssets();
        }

        public static Sprite SpriteOf(Item item) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + item.name + ".png");

        public static AudioClip SoundOf(Item item) =>
            item.sound == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "/" + item.sound);

        // ------------------------------------------------------------------ pictures

        /// <summary>A copy of the picture cut to its drawing and made at most 512 pixels, as a sprite standing on its bottom edge.</summary>
        static Sprite MakeSprite(Item item)
        {
            string target = SpriteFolder + "/" + item.name + ".png";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(PictureFolder + "/" + item.picture));
            int w = source.width, h = source.height;
            Color32[] pixels = source.GetPixels32();
            Object.DestroyImmediate(source);

            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (pixels[y * w + x].a > 20)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            float scale = Mathf.Min(1f, 512f / Mathf.Max(cw, ch));
            int ow = Mathf.Max(1, Mathf.RoundToInt(cw * scale)), oh = Mathf.Max(1, Mathf.RoundToInt(ch * scale));
            var output = new Color32[ow * oh];
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                    output[y * ow + x] = pixels[(minY + Mathf.Min(ch - 1, (int)((y + 0.5f) / scale))) * w + minX + Mathf.Min(cw - 1, (int)((x + 0.5f) / scale))];

            var made = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
            made.SetPixels32(output);
            File.WriteAllBytes(target, made.EncodeToPNG());
            Object.DestroyImmediate(made);

            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = oh / item.height;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        // ------------------------------------------------------------------ prefabs

        static void MakePrefab(Item item, Dictionary<string, Sprite> sprites)
        {
            string path = PrefabFolder + "/Item_" + item.name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;   // keep the user's version

            var root = new GameObject("Item_" + item.name);
            Vector3 size = item.model != null ? AddModel(root.transform, item) : AddPicture(root.transform, sprites[item.name]);

            var box = root.AddComponent<BoxCollider>();
            FitCollider(box, size);

            var target = root.AddComponent<WordTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("word").stringValue = item.word;
            so.FindProperty("place").stringValue = item.place;
            so.FindProperty("sound").objectReferenceValue = SoundOf(item);
            so.FindProperty("picture").objectReferenceValue = sprites[item.name];
            so.FindProperty("category").stringValue = item.category;
            so.FindProperty("categoryEnglish").stringValue = item.categoryEnglish;
            so.FindProperty("hint").stringValue = item.hint;
            so.FindProperty("hintEnglish").stringValue = item.hintEnglish;
            so.FindProperty("interactDistance").floatValue = 1.1f;
            so.FindProperty("area").objectReferenceValue = box;

            // Two wrong answers: the next two other items of the same place.
            var others = new List<Item>();
            int start = System.Array.IndexOf(All, item);
            for (int i = 1; i < All.Length && others.Count < 2; i++)
            {
                Item other = All[(start + i) % All.Length];
                if (other.place == item.place) others.Add(other);
            }
            SerializedProperty wrong = so.FindProperty("wrongAnswers");
            wrong.arraySize = others.Count;
            for (int i = 0; i < others.Count; i++)
            {
                SerializedProperty choice = wrong.GetArrayElementAtIndex(i);
                choice.FindPropertyRelative("word").stringValue = others[i].word;
                choice.FindPropertyRelative("category").stringValue = others[i].category;
                choice.FindPropertyRelative("categoryEnglish").stringValue = others[i].categoryEnglish;
                choice.FindPropertyRelative("picture").objectReferenceValue = sprites[others[i].name];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Vector3 AddPicture(Transform root, Sprite sprite)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            visual.AddComponent<BillboardToCamera>();
            return new Vector3(sprite.bounds.size.x, sprite.bounds.size.y, 0.8f);
        }

        /// <summary>Your 3D model, scaled to the wanted height, standing on the ground, in one plain colour.</summary>
        static Vector3 AddModel(Transform root, Item item)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + item.model);
            if (asset == null) throw new System.IO.FileNotFoundException("no model " + item.model + " in " + ModelFolder);
            var model = new GameObject("Model");
            model.transform.SetParent(root, false);
            var lie = new GameObject("Turn").transform;
            lie.SetParent(model.transform, false);
            var thick = new GameObject("Shape").transform;
            thick.SetParent(lie, false);
            var file = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            file.transform.SetParent(thick, false);
            thick.localScale = new Vector3(item.thicken, 1f, item.thicken);
            if (item.lieDown) lie.localRotation = Quaternion.Euler(0f, 0f, 90f);
            model.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);

            foreach (Camera camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (Light light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;

            Bounds bounds = RendererBounds(model);
            float scale = item.height / Mathf.Max(0.001f, bounds.size.y);
            // A long, flat model (chalk lying down, a wide eraser) is not blown up beyond 2 units across.
            float widest = Mathf.Max(bounds.size.x, bounds.size.z) * scale;
            if (widest > 2f) scale *= 2f / widest;
            if (item.width > 0f) scale = item.width / Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.z));
            model.transform.localScale = Vector3.one * scale;

            bounds = RendererBounds(model);
            Vector3 shift = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            model.transform.localPosition += shift;

            if (item.modelColor.a <= 0f) return RendererBounds(model).size;   // keep the model's own materials
            Material material = PlainMaterial("Item_" + item.name, item.modelColor);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            return RendererBounds(model).size;
        }

        static void FitCollider(BoxCollider box, Vector3 size)
        {
            float height = Mathf.Clamp(size.y, 0.6f, 1.6f);   // not so low that the character climbs over it
            box.size = new Vector3(Mathf.Max(0.6f, size.x * 0.85f), height, Mathf.Clamp(size.z * 0.9f, 0.6f, 2f));
            box.center = new Vector3(0f, height * 0.5f, 0f);
        }

        /// <summary>
        /// Gives item prefabs that already exist their 3D model in place of the flat picture (or of an older model). The rest of
        /// the prefab (word, sound, sentences) stays as it is.
        /// </summary>
        public static void UseModels(params string[] names)
        {
            foreach (Item item in All)
            {
                if (item.model == null || System.Array.IndexOf(names, item.name) < 0) continue;
                string path = PrefabFolder + "/Item_" + item.name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                foreach (string old in new[] { "Visual", "Model" })
                {
                    Transform child = root.transform.Find(old);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
                FitCollider(root.GetComponent<BoxCollider>(), AddModel(root.transform, item));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        static Material PlainMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
