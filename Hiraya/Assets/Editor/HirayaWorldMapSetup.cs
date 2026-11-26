using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Hiraya.EditorTools.HirayaTitleSetup;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Builds the world map screen (wireframe 02) as the WorldMapPanel inside the TitleScreen prefab.
    /// Left: a plan view of your real map (a top-down picture of the HirayaMap prefab) with a marker on every built place.
    /// Right: the "Mga Lugar" list, star progress, Salita Book and the Pahinga / Parent buttons. Bottom: the next place and play.
    /// Everything is a normal UI object; the plan picture and icons are saved in Assets/UI/WorldMap.
    /// </summary>
    internal static class HirayaWorldMapSetup
    {
        const string FolderPath = "Assets/UI/WorldMap";
        const string PlanPath = FolderPath + "/PlanView.png";

        const float PlanWorldHalf = 62f;     // the plan picture shows 124 x 124 world units
        const float PlanUiSize = 660f;       // and is drawn this big inside the map panel

        static readonly Color PanelGreen = new Color(0.90f, 0.92f, 0.87f, 1f);
        static readonly Color CardBackground = new Color(0.98f, 0.98f, 0.96f, 1f);
        static readonly Color IconCircle = new Color(0.93f, 0.93f, 0.91f, 1f);

        class Icons
        {
            public Sprite house, stall, school, garden, mountain, book, star, chevron;
        }

        class PlaceSpec
        {
            public string name;
            public string building;      // building object in the playing scene ("" = not built yet)
            public Sprite icon;
        }

        // ------------------------------------------------------------------ the panel

        public static WorldMapController BuildPanel(Transform root, Sprites sprites, Sprite boyPortrait, Sprite girlPortrait, out RectTransform panel)
        {
            HirayaCharacterSetup.EnsureFolder(FolderPath);
            Icons icons = MakeIcons();
            Sprite plan = RenderPlanView();

            panel = NewStretched("WorldMapPanel", root);
            var controller = panel.gameObject.AddComponent<WorldMapController>();

            // The five places, in the same order as WorldMapController.places and the cards of the list.
            var specs = new[]
            {
                new PlaceSpec { name = "Tahanan", building = "Building_House_2", icon = icons.house },
                new PlaceSpec { name = "Palengke", building = "", icon = icons.stall },
                new PlaceSpec { name = "Paaralan", building = "Building_School", icon = icons.school },
                new PlaceSpec { name = "Hardin", building = "Building_Park", icon = icons.garden },
                new PlaceSpec { name = "Bundok", building = "", icon = icons.mountain }
            };

            // ---------------------------------------------------------------- top bar
            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);

            RectTransform profile = Card(panel, "ProfilePill", new Vector2(160f, 48f), new Vector2(240f, 92f), 46f, Color.white, LineGray, sprites.rounded);
            RoundedAt(profile, "PortraitCircle", new Vector2(12f, 12f), new Vector2(68f, 68f), IconCircle, sprites.rounded);
            Image portrait = IconAt(profile, "Portrait", new Vector2(12f, 8f), new Vector2(68f, 76f), boyPortrait, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.rectTransform.sizeDelta = new Vector2(100f, 100f);
            portrait.rectTransform.anchoredPosition = new Vector2(46f, -46f);
            TextMeshProUGUI profileName = TextAt(profile, "Name", "Boy", 34f, Ink, FontStyles.Bold, new Vector2(100f, 24f), new Vector2(124f, 44f), TextAlignmentOptions.MidlineLeft);

            RectTransform starPill = Card(panel, "StarPill", new Vector2(416f, 48f), new Vector2(180f, 92f), 46f, Color.white, LineGray, sprites.rounded);
            IconAt(starPill, "StarIcon", new Vector2(26f, 26f), new Vector2(40f, 40f), icons.star, Color.white).raycastTarget = false;
            TextMeshProUGUI starTotal = TextAt(starPill, "Count", "0", 42f, Ink, FontStyles.Bold, new Vector2(78f, 20f), new Vector2(80f, 52f), TextAlignmentOptions.MidlineLeft);

            Image settingsIcon;
            Button settingsButton = AddRoundButton(panel, "SettingsButton", new Vector2(1f, 1f), new Vector2(-40f, -48f), sprites.rounded, sprites.settings, out settingsIcon);
            ((RectTransform)settingsButton.transform).pivot = new Vector2(1f, 1f);

            // ---------------------------------------------------------------- map panel (left)
            RectTransform mapPanel = Card(panel, "MapPanel", new Vector2(47f, 176f), new Vector2(701f, 702f), 36f, PanelGreen, LineGray, sprites.rounded);
            var planImage = NewRect("PlanView", mapPanel).gameObject.AddComponent<Image>();
            Place(planImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * PlanUiSize);
            planImage.sprite = plan;
            planImage.raycastTarget = false;

            // Where each built place is on the plan.
            var plots = HirayaBuildingSetup.ReadPlots(HirayaBuildingSetup.ReadLayoutRows());
            var markers = new MarkerRefs[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                if (string.IsNullOrEmpty(specs[i].building)) continue;
                HirayaBuildingSetup.Plot plot = plots.Find(p => p.prefabName == specs[i].building);
                if (plot == null) continue;

                float scale = PlanUiSize / (2f * PlanWorldHalf);
                var position = new Vector2(plot.center.x * scale, plot.center.z * scale);
                markers[i] = BuildMarker(mapPanel, "Marker_" + specs[i].name, specs[i].name, position, specs[i].icon, sprites, specs[i].name == "Hardin");
            }

            // ---------------------------------------------------------------- places card (right)
            RectTransform places = Card(panel, "PlacesCard", new Vector2(780f, 176f), new Vector2(1091f, 702f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(places, "Title", "Mga Lugar", 46f, Ink, FontStyles.Bold, new Vector2(42f, 28f), new Vector2(600f, 60f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI subtitle = TextAt(places, "Subtitle", "0 sa 5 na-unlock · piliin ang lugar na lalaruin", 24f, SoftInk, FontStyles.Normal,
                                              new Vector2(42f, 90f), new Vector2(900f, 36f), TextAlignmentOptions.MidlineLeft);

            Vector2 cardSize = new Vector2(478f, 113f);
            Vector2[] slots =
            {
                new Vector2(42f, 148f), new Vector2(552f, 148f),    // Tahanan, Palengke
                new Vector2(42f, 278f), new Vector2(552f, 278f),    // Paaralan, Hardin
                new Vector2(42f, 408f)                              // Bundok
            };
            var cards = new WorldMapController.PlaceCard[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                cards[i] = BuildPlaceCard(places, "Place_" + specs[i].name, specs[i].name, slots[i], cardSize, specs[i].icon, icons, sprites, markers[i]);
            }

            // Salita Book card (bottom right of the list).
            RectTransform book = Card(places, "SalitaBook", new Vector2(552f, 408f), cardSize, 26f, CardBackground, LineGray, sprites.rounded);
            RoundedAt(book, "IconCircle", new Vector2(20f, 23.5f), Vector2.one * 66f, IconCircle, sprites.rounded);
            IconAt(book, "Icon", new Vector2(33f, 36.5f), Vector2.one * 40f, icons.book, IconInk).raycastTarget = false;
            TextAt(book, "Name", "Aking Salita Book", 30f, Ink, FontStyles.Bold, new Vector2(108f, 18f), new Vector2(300f, 40f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI bookInfo = TextAt(book, "Info", "0 sa 40 salita", 23f, SoftInk, FontStyles.Normal, new Vector2(108f, 62f), new Vector2(300f, 34f), TextAlignmentOptions.MidlineLeft);
            IconAt(book, "Chevron", new Vector2(cardSize.x - 24f - 30f, 41.5f), Vector2.one * 30f, icons.chevron, IconInk).raycastTarget = false;
            var bookButton = book.gameObject.AddComponent<Button>();
            bookButton.transition = Selectable.Transition.None;
            bookButton.targetGraphic = book.Find("Fill").GetComponent<Image>();

            // Progress.
            TextAt(places, "ProgressLabel", "Kabuuang bituin", 24f, SoftInk, FontStyles.Normal, new Vector2(42f, 552f), new Vector2(300f, 36f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI progressValue = TextAt(places, "ProgressValue", "0 / 15", 26f, Ink, FontStyles.Bold, new Vector2(342f, 552f), new Vector2(178f, 36f), TextAlignmentOptions.MidlineRight);
            Image track = RoundedAt(places, "ProgressTrack", new Vector2(42f, 600f), new Vector2(478f, 16f), LineGray, sprites.rounded);
            RectTransform fillRect = NewRect("Fill", track.transform);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.sprite = sprites.rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Orange;
            fillImage.pixelsPerUnitMultiplier = 128f / 16f;
            fillImage.raycastTarget = false;

            // Pahinga and Parent.
            Button rest = OutlinePill(places, "RestButton", "Pahinga", null, new Vector2(552f, 548f), new Vector2(230f, 76f), sprites);
            Button parent = OutlinePill(places, "ParentButton", "Parent", sprites.lockIcon, new Vector2(800f, 548f), new Vector2(230f, 76f), sprites);

            // ---------------------------------------------------------------- bottom bar
            RectTransform bar = Card(panel, "BottomBar", new Vector2(47f, 907f), new Vector2(1824f, 100f), 50f, Color.white, LineGray, sprites.rounded);
            TextAt(bar, "Label", "SUSUNOD NA LUGAR", 20f, SoftInk, FontStyles.Normal, new Vector2(50f, 12f), new Vector2(600f, 28f), TextAlignmentOptions.MidlineLeft, 3f);
            TextMeshProUGUI nextPlace = TextAt(bar, "Place", "Paaralan — Mga Bagong Salita", 34f, Ink, FontStyles.Bold, new Vector2(50f, 40f), new Vector2(1200f, 46f), TextAlignmentOptions.MidlineLeft);
            Image playCircle = RoundedAt(bar, "PlayButton", new Vector2(1724f, 12f), Vector2.one * 76f, Orange, sprites.rounded);
            IconAt(playCircle.transform, "PlayIcon", Vector2.zero, Vector2.one * 38f, sprites.play, Color.white, true).raycastTarget = false;
            var playButton = playCircle.gameObject.AddComponent<Button>();
            playButton.targetGraphic = playCircle;
            var playColors = playButton.colors;
            playColors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            playColors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            playColors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.4f);
            playButton.colors = playColors;

            // ---------------------------------------------------------------- connect to the controller
            var so = new SerializedObject(controller);
            SerializedProperty cardProperty = so.FindProperty("cards");
            cardProperty.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                SerializedProperty c = cardProperty.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("group").objectReferenceValue = cards[i].group;
                c.FindPropertyRelative("button").objectReferenceValue = cards[i].button;
                c.FindPropertyRelative("border").objectReferenceValue = cards[i].border;
                c.FindPropertyRelative("fill").objectReferenceValue = cards[i].fill;
                c.FindPropertyRelative("status").objectReferenceValue = cards[i].status;
                c.FindPropertyRelative("starsText").objectReferenceValue = cards[i].starsText;
                c.FindPropertyRelative("starIcon").objectReferenceValue = cards[i].starIcon;
                c.FindPropertyRelative("lockIcon").objectReferenceValue = cards[i].lockIcon;
                c.FindPropertyRelative("playPill").objectReferenceValue = cards[i].playPill;
                c.FindPropertyRelative("playPillButton").objectReferenceValue = cards[i].playPillButton;
                c.FindPropertyRelative("markerRoot").objectReferenceValue = cards[i].markerRoot;
                c.FindPropertyRelative("markerButton").objectReferenceValue = cards[i].markerButton;
                c.FindPropertyRelative("markerFill").objectReferenceValue = cards[i].markerFill;
                c.FindPropertyRelative("markerIcon").objectReferenceValue = cards[i].markerIcon;
                c.FindPropertyRelative("markerLockSprite").objectReferenceValue = cards[i].markerLockSprite;
                c.FindPropertyRelative("markerPlaceSprite").objectReferenceValue = cards[i].markerPlaceSprite;
            }
            so.FindProperty("profileName").objectReferenceValue = profileName;
            so.FindProperty("profilePortrait").objectReferenceValue = portrait;
            so.FindProperty("boyPortrait").objectReferenceValue = boyPortrait;
            so.FindProperty("girlPortrait").objectReferenceValue = girlPortrait;
            so.FindProperty("starTotal").objectReferenceValue = starTotal;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("progressValue").objectReferenceValue = progressValue;
            so.FindProperty("progressFill").objectReferenceValue = fillRect;
            so.FindProperty("wordBookButton").objectReferenceValue = bookButton;
            so.FindProperty("wordBookInfo").objectReferenceValue = bookInfo;
            so.FindProperty("restButton").objectReferenceValue = rest;
            so.FindProperty("parentButton").objectReferenceValue = parent;
            so.FindProperty("nextPlaceText").objectReferenceValue = nextPlace;
            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        // ------------------------------------------------------------------ place cards and markers

        class MarkerRefs
        {
            public GameObject root;
            public Button button;
            public Image fill;
            public Image icon;
        }

        static MarkerRefs BuildMarker(Transform parent, string name, string label, Vector2 position, Sprite icon, Sprites sprites, bool labelAbove)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(80f, 80f));

            AddRounded(root, "Ring", new Vector2(88f, 88f), LineGray, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            Image fill = AddRounded(root, "Fill", new Vector2(80f, 80f), Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            Image iconImage = AddIcon(root, "Icon", new Vector2(44f, 44f), icon, IconInk, new Vector2(0.5f, 0.5f), Vector2.zero);
            iconImage.raycastTarget = false;

            float labelWidth = Mathf.Max(110f, label.Length * 17f + 36f);
            Image pill = AddRounded(root, "LabelPill", new Vector2(labelWidth, 42f), Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(0f, labelAbove ? 66f : -66f));
            pill.raycastTarget = false;
            AddText(pill.transform, "Label", label, 24f, Ink, FontStyles.Bold, new Vector2(labelWidth, 42f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f).raycastTarget = false;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return new MarkerRefs { root = root.gameObject, button = button, fill = fill, icon = iconImage };
        }

        static WorldMapController.PlaceCard BuildPlaceCard(Transform parent, string objectName, string displayName, Vector2 topLeft, Vector2 size,
                                                          Sprite icon, Icons icons, Sprites sprites, MarkerRefs marker)
        {
            RectTransform root = Card(parent, objectName, topLeft, size, 26f, CardBackground, LineGray, sprites.rounded);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Image border = root.Find("Border").GetComponent<Image>();
            Image fill = root.Find("Fill").GetComponent<Image>();

            RoundedAt(root, "IconCircle", new Vector2(20f, 23.5f), Vector2.one * 66f, IconCircle, sprites.rounded);
            IconAt(root, "Icon", new Vector2(33f, 36.5f), Vector2.one * 40f, icon, IconInk).raycastTarget = false;
            TextAt(root, "Name", displayName, 32f, Ink, FontStyles.Bold, new Vector2(108f, 18f), new Vector2(260f, 40f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI status = TextAt(root, "Status", "", 21f, SoftInk, FontStyles.Normal, new Vector2(108f, 62f), new Vector2(340f, 34f), TextAlignmentOptions.MidlineLeft);

            TextMeshProUGUI stars = TextAt(root, "Stars", "0 / 3", 28f, Ink, FontStyles.Bold, new Vector2(size.x - 24f - 34f - 110f, 36f), new Vector2(110f, 40f), TextAlignmentOptions.MidlineRight);
            Image starIcon = IconAt(root, "StarIcon", new Vector2(size.x - 24f - 30f, 41.5f), Vector2.one * 30f, icons.star, Color.white);
            starIcon.raycastTarget = false;
            Image lockIcon = IconAt(root, "LockIcon", new Vector2(size.x - 24f - 34f, 39.5f), Vector2.one * 34f, sprites.lockIcon, Color.white);
            lockIcon.raycastTarget = false;

            Image pill = RoundedAt(root, "PlayPill", new Vector2(size.x - 24f - 132f, 28.5f), new Vector2(132f, 56f), Orange, sprites.rounded);
            AddText(pill.transform, "Label", "Maglaro", 25f, Color.white, FontStyles.Bold, new Vector2(132f, 56f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f).raycastTarget = false;
            var pillButton = pill.gameObject.AddComponent<Button>();
            pillButton.targetGraphic = pill;
            var colors = pillButton.colors;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            pillButton.colors = colors;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;

            var card = new WorldMapController.PlaceCard
            {
                group = group, button = button, border = border, fill = fill, status = status,
                starsText = stars, starIcon = starIcon, lockIcon = lockIcon, playPill = pill.gameObject, playPillButton = pillButton
            };
            if (marker != null)
            {
                card.markerRoot = marker.root;
                card.markerButton = marker.button;
                card.markerFill = marker.fill;
                card.markerIcon = marker.icon;
                card.markerLockSprite = sprites.lockIcon;
                card.markerPlaceSprite = icon;
            }
            return card;
        }

        internal static Button OutlinePill(Transform parent, string name, string label, Sprite icon, Vector2 topLeft, Vector2 size, Sprites sprites)
        {
            RectTransform root = Card(parent, name, topLeft, size, size.y * 0.5f, Color.white, LineGray, sprites.rounded);
            if (icon != null)
            {
                IconAt(root, "Icon", new Vector2(34f, (size.y - 36f) * 0.5f), Vector2.one * 36f, icon, Color.white).raycastTarget = false;
                TextAt(root, "Label", label, 28f, Ink, FontStyles.Bold, new Vector2(80f, 12f), new Vector2(size.x - 100f, size.y - 24f), TextAlignmentOptions.MidlineLeft);
            }
            else
            {
                TextAt(root, "Label", label, 28f, Ink, FontStyles.Bold, new Vector2(0f, 12f), new Vector2(size.x, size.y - 24f), TextAlignmentOptions.Center);
            }
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.Find("Fill").GetComponent<Image>();
            return button;
        }

        // ------------------------------------------------------------------ small UI helpers (top-left coordinates)

        /// <summary>A rounded card with a thin border: Card > Border + Fill. Position is measured from the top left corner of the parent.</summary>
        internal static RectTransform Card(Transform parent, string name, Vector2 topLeft, Vector2 size, float radius, Color fill, Color border, Sprite rounded)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(topLeft.x, -topLeft.y), size);
            StretchedRounded(root, "Border", border, rounded, radius, 3f);
            StretchedRounded(root, "Fill", fill, rounded, radius, 0f);
            return root;
        }

        internal static Image StretchedRounded(Transform parent, string name, Color color, Sprite rounded, float radius, float outset)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-outset, -outset);
            rect.offsetMax = new Vector2(outset, outset);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = 64f / radius;
            return image;
        }

        internal static Image RoundedAt(Transform parent, string name, Vector2 topLeft, Vector2 size, Color color, Sprite rounded)
        {
            return AddRounded(parent, name, size, color, rounded, new Vector2(0f, 1f), new Vector2(topLeft.x + size.x * 0.5f, -(topLeft.y + size.y * 0.5f)));
        }

        internal static Image IconAt(Transform parent, string name, Vector2 topLeft, Vector2 size, Sprite sprite, Color color, bool centered = false)
        {
            if (centered) return AddIcon(parent, name, size, sprite, color, new Vector2(0.5f, 0.5f), topLeft);
            return AddIcon(parent, name, size, sprite, color, new Vector2(0f, 1f), new Vector2(topLeft.x + size.x * 0.5f, -(topLeft.y + size.y * 0.5f)));
        }

        internal static TextMeshProUGUI TextAt(Transform parent, string name, string text, float fontSize, Color color, FontStyles style,
                                      Vector2 topLeft, Vector2 size, TextAlignmentOptions alignment, float spacing = 0f)
        {
            TextMeshProUGUI tmp = AddText(parent, name, text, fontSize, color, style, size, new Vector2(0f, 1f),
                                          new Vector2(topLeft.x + size.x * 0.5f, -(topLeft.y + size.y * 0.5f)), spacing, alignment);
            return tmp;
        }

        // ------------------------------------------------------------------ the plan picture

        /// <summary>A top-down picture of the real HirayaMap prefab, so the markers sit on the real buildings.</summary>
        static Sprite RenderPlanView()
        {
            const int size = 1024;
            // A hidden preview scene: nothing in the scenes you have open is touched or even seen by this camera.
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaMapSetup.MapPrefabPath);
                PrefabUtility.InstantiatePrefab(mapPrefab, scene);

                var lightGo = new GameObject("PlanLight");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
                SceneManager.MoveGameObjectToScene(lightGo, scene);

                var camGo = new GameObject("PlanCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = PlanWorldHalf;
                cam.transform.position = new Vector3(0f, 100f, 0f);
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = PanelGreen;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 300f;
                SceneManager.MoveGameObjectToScene(camGo, scene);
                cam.scene = scene;   // render only the preview scene

                var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                File.WriteAllBytes(PlanPath, tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            AssetDatabase.ImportAsset(PlanPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PlanPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(PlanPath);
        }

        // ------------------------------------------------------------------ icons

        static Icons MakeIcons()
        {
            var icons = new Icons();

            var house = new Painter(128, 128);
            house.Fill(Union(Polygon(new Vector2(10f, 66f), new Vector2(64f, 116f), new Vector2(118f, 66f)), Rect(26f, 20f, 102f, 68f), Rect(84f, 88f, 98f, 112f)), Color.white);
            house.Clear(Rect(54f, 20f, 74f, 52f));
            icons.house = SaveSprite(FolderPath + "/HouseIcon.png", house, Vector4.zero);

            var stall = new Painter(128, 128);
            stall.Fill(Union(Polygon(new Vector2(14f, 112f), new Vector2(114f, 112f), new Vector2(124f, 78f), new Vector2(4f, 78f)),
                             Circle(22f, 78f, 11f), Circle(43f, 78f, 11f), Circle(64f, 78f, 11f), Circle(85f, 78f, 11f), Circle(106f, 78f, 11f),
                             Rect(16f, 20f, 112f, 66f)), Color.white);
            stall.Clear(Rect(32f, 30f, 96f, 52f));
            icons.stall = SaveSprite(FolderPath + "/StallIcon.png", stall, Vector4.zero);

            var school = new Painter(128, 128);
            school.Fill(Union(Rect(14f, 18f, 114f, 70f), Polygon(new Vector2(6f, 70f), new Vector2(64f, 106f), new Vector2(122f, 70f)),
                              Rect(62f, 104f, 67f, 124f), Polygon(new Vector2(67f, 124f), new Vector2(67f, 111f), new Vector2(90f, 117f))), Color.white);
            school.Clear(Union(Rect(54f, 18f, 74f, 48f), Rect(26f, 38f, 42f, 58f), Rect(86f, 38f, 102f, 58f)));
            icons.school = SaveSprite(FolderPath + "/SchoolIcon.png", school, Vector4.zero);

            var garden = new Painter(128, 128);
            var shapes = new List<Shape> { Circle(64f, 84f, 14f), Rect(61f, 16f, 67f, 62f), Polygon(new Vector2(67f, 36f), new Vector2(100f, 54f), new Vector2(67f, 52f)) };
            for (int i = 0; i < 5; i++)
            {
                float angle = (90f + i * 72f) * Mathf.Deg2Rad;
                shapes.Add(Circle(64f + Mathf.Cos(angle) * 24f, 84f + Mathf.Sin(angle) * 24f, 16f));
            }
            garden.Fill(Union(shapes.ToArray()), Color.white);
            garden.Clear(Circle(64f, 84f, 7f));
            icons.garden = SaveSprite(FolderPath + "/GardenIcon.png", garden, Vector4.zero);

            var mountain = new Painter(128, 128);
            mountain.Fill(Union(Polygon(new Vector2(4f, 22f), new Vector2(54f, 108f), new Vector2(104f, 22f)),
                                Polygon(new Vector2(64f, 22f), new Vector2(94f, 74f), new Vector2(126f, 22f))), Color.white);
            icons.mountain = SaveSprite(FolderPath + "/MountainIcon.png", mountain, Vector4.zero);

            var book = new Painter(128, 128);
            book.Fill(Union(Polygon(new Vector2(10f, 28f), new Vector2(60f, 40f), new Vector2(60f, 102f), new Vector2(10f, 90f)),
                            Polygon(new Vector2(68f, 40f), new Vector2(118f, 28f), new Vector2(118f, 90f), new Vector2(68f, 102f))), Color.white);
            icons.book = SaveSprite(FolderPath + "/BookIcon.png", book, Vector4.zero);

            var star = new Painter(128, 128);
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? 58f : 24f;
                points[i] = new Vector2(64f + Mathf.Cos(angle) * radius, 62f + Mathf.Sin(angle) * radius);
            }
            star.Fill(Polygon(points), Orange);
            icons.star = SaveSprite(FolderPath + "/StarIcon.png", star, Vector4.zero);

            var chevron = new Painter(128, 128);
            chevron.Fill(Union(Segment(new Vector2(46f, 22f), new Vector2(86f, 64f), 13f), Segment(new Vector2(86f, 64f), new Vector2(46f, 106f), 13f)), Color.white);
            icons.chevron = SaveSprite(FolderPath + "/ChevronIcon.png", chevron, Vector4.zero);

            return icons;
        }
    }
}
