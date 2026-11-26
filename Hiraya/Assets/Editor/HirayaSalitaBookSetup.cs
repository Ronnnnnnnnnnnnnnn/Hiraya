using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Hiraya.EditorTools.HirayaTitleSetup;
using static Hiraya.EditorTools.HirayaWorldMapSetup;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Builds the Salita Book (wireframe 08) as the SalitaBookPanel inside the TitleScreen prefab, the WordCard prefab it fills
    /// the grid with (Assets/Prefabs/UI/WordCard.prefab) and the word list it shows (Assets/Data/WordCatalog.asset).
    /// The words are grouped by place (the items of HirayaItems); the list is only filled when it is empty.
    ///   SalitaBookPanel > BackButton, Title, Subtitle, CounterPill, Chips (category buttons), Scroll > Viewport > Content (the cards)
    /// </summary>
    internal static class HirayaSalitaBookSetup
    {
        public const string DataFolder = "Assets/Data";
        public const string CatalogPath = DataFolder + "/WordCatalog.asset";
        public const string CardPrefabPath = HirayaTitleSetup.PrefabFolder + "/WordCard.prefab";
        const string BookFolder = "Assets/UI/Book";

        const float CardWidth = 385f;
        const float CardHeight = 323f;
        static readonly Color PictureGray = new Color(0.93f, 0.93f, 0.91f, 1f);

        /// <summary>The place buttons of the book. Market, park and mountain have no words yet and stay locked.</summary>
        static readonly string[] PlaceIds = { "paaralan", "tahanan", "palengke", "hardin", "bundok" };

        // ------------------------------------------------------------------ the word list

        /// <summary>
        /// The word list: the items of HirayaItems, school first. It is only filled when it has no places yet, so your own
        /// changes to the words are never overwritten.
        /// </summary>
        public static WordCatalog EnsureCatalog()
        {
            HirayaCharacterSetup.EnsureFolder(DataFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<WordCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WordCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty places = so.FindProperty("places");
            if (places.arraySize > 0)
            {
                FillSentences(catalog);
                return catalog;
            }

            places.arraySize = PlaceIds.Length;
            for (int i = 0; i < PlaceIds.Length; i++)
            {
                places.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = PlaceIds[i];
                places.GetArrayElementAtIndex(i).FindPropertyRelative("locked").boolValue = PlaceIds[i] != "paaralan" && PlaceIds[i] != "tahanan";
            }

            SerializedProperty words = so.FindProperty("words");
            words.arraySize = HirayaItems.All.Length;
            for (int i = 0; i < HirayaItems.All.Length; i++)
            {
                HirayaItems.Item item = HirayaItems.All[i];
                SerializedProperty entry = words.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("word").stringValue = item.word;
                entry.FindPropertyRelative("place").stringValue = item.place;
                entry.FindPropertyRelative("english").stringValue = item.english;
                entry.FindPropertyRelative("picture").objectReferenceValue = HirayaItems.SpriteOf(item);
                entry.FindPropertyRelative("sound").objectReferenceValue = HirayaItems.SoundOf(item);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            FillSentences(catalog);
            return catalog;
        }

        /// <summary>Writes the texts for 6 and up into words that have none yet. A word you filled in yourself is left alone.</summary>
        static void FillSentences(WordCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            SerializedProperty words = so.FindProperty("words");
            bool changed = false;
            for (int i = 0; i < words.arraySize; i++)
            {
                SerializedProperty entry = words.GetArrayElementAtIndex(i);
                string word = entry.FindPropertyRelative("word").stringValue;
                string[] text = System.Array.Find(HirayaItems.Sentences, t => t[0] == word);
                if (text == null) continue;
                if (!string.IsNullOrEmpty(entry.FindPropertyRelative("description").stringValue) ||
                    !string.IsNullOrEmpty(entry.FindPropertyRelative("blank").stringValue)) continue;

                entry.FindPropertyRelative("description").stringValue = text[1];
                entry.FindPropertyRelative("example").stringValue = text[2];
                entry.FindPropertyRelative("exampleEnglish").stringValue = text[3];
                entry.FindPropertyRelative("blank").stringValue = text[4];
                entry.FindPropertyRelative("blankEnglish").stringValue = text[5];
                SerializedProperty wrong = entry.FindPropertyRelative("blankWrong");
                wrong.arraySize = 2;
                wrong.GetArrayElementAtIndex(0).stringValue = text[6];
                wrong.GetArrayElementAtIndex(1).stringValue = text[7];
                changed = true;
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ the panel

        public static SalitaBookController BuildPanel(Transform root, Sprites sprites, out RectTransform panel)
        {
            HirayaCharacterSetup.EnsureFolder(BookFolder);
            WordCatalog catalog = EnsureCatalog();
            WordCardView cardPrefab = BuildCardPrefab(sprites);
            var bookIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/WorldMap/BookIcon.png");

            panel = NewStretched("SalitaBookPanel", root);
            var controller = panel.gameObject.AddComponent<SalitaBookController>();

            // ---------------------------------------------------------------- top
            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);
            TextAt(panel, "Title", "Aking Salita Book", 58f, Ink, FontStyles.Bold, new Vector2(162f, 34f), new Vector2(900f, 72f), TextAlignmentOptions.MidlineLeft);
            TextAt(panel, "Subtitle", "Mga salita sa bawat lugar", 26f, SoftInk, FontStyles.Normal, new Vector2(162f, 104f), new Vector2(900f, 36f), TextAlignmentOptions.MidlineLeft);

            RectTransform counterPill = Card(panel, "CounterPill", new Vector2(1480f, 48f), new Vector2(300f, 76f), 38f, Color.white, LineGray, sprites.rounded);
            IconAt(counterPill, "Icon", new Vector2(24f, 18f), Vector2.one * 40f, bookIcon, Orange).raycastTarget = false;
            TextMeshProUGUI counter = TextAt(counterPill, "Count", "0 / 9 salita", 27f, Ink, FontStyles.Bold, new Vector2(76f, 14f), new Vector2(212f, 48f), TextAlignmentOptions.MidlineLeft);
            counter.enableAutoSizing = true;
            counter.fontSizeMin = 20f;
            counter.fontSizeMax = 27f;

            // ---------------------------------------------------------------- category buttons
            RectTransform chipRow = NewRect("Chips", panel);
            Place(chipRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -164f), new Vector2(1640f, 66f));
            var rowLayout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var chips = new SalitaBookController.PlaceChip[PlaceIds.Length];
            for (int i = 0; i < chips.Length; i++) chips[i] = BuildChip(chipRow, "Chip_" + (i + 1), "Paaralan 0/9", sprites);

            // ---------------------------------------------------------------- the cards: a scroll view with a grid of 4 columns
            RectTransform scrollRect = NewRect("Scroll", panel);
            Place(scrollRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -250f), new Vector2(1640f, 810f));
            var scrollImage = scrollRect.gameObject.AddComponent<Image>();   // invisible, lets the empty space between the cards be dragged
            scrollImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform viewport = NewStretched("Viewport", scrollRect);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardWidth, CardHeight);
            grid.spacing = new Vector2(33f, 48f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(0, 0, 14, 40);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            // ---------------------------------------------------------------- connect
            var so = new SerializedObject(controller);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("cardParent").objectReferenceValue = content;
            so.FindProperty("scroll").objectReferenceValue = scroll;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("counter").objectReferenceValue = counter;
            SerializedProperty chipsProperty = so.FindProperty("chips");
            chipsProperty.arraySize = chips.Length;
            for (int i = 0; i < chips.Length; i++)
            {
                SerializedProperty c = chipsProperty.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("button").objectReferenceValue = chips[i].button;
                c.FindPropertyRelative("fill").objectReferenceValue = chips[i].fill;
                c.FindPropertyRelative("label").objectReferenceValue = chips[i].label;
                c.FindPropertyRelative("lockIcon").objectReferenceValue = chips[i].lockIcon;
                c.FindPropertyRelative("group").objectReferenceValue = chips[i].group;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        /// <summary>A place button as wide as its text, with a small lock while the place is locked.</summary>
        static SalitaBookController.PlaceChip BuildChip(Transform parent, string name, string text, Sprites sprites)
        {
            RectTransform root = NewRect(name, parent);
            Image border = StretchedRounded(root, "Border", LineGray, sprites.rounded, 31f, 3f);
            Image fill = StretchedRounded(root, "Fill", Color.white, sprites.rounded, 31f, 0f);
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var size = root.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 62f;

            layout.spacing = 10f;
            Image lockImage = AddIcon(root, "Lock", Vector2.one * 28f, sprites.lockIcon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
            lockImage.raycastTarget = false;
            HirayaGameplaySetup.FixedSize(lockImage.gameObject, 28f, 28f);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI label = AddText(root, "Label", text, 28f, Ink, FontStyles.Bold, new Vector2(120f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return new SalitaBookController.PlaceChip { button = button, fill = fill, label = label, lockIcon = lockImage.gameObject, group = group };
        }

        // ------------------------------------------------------------------ the word card prefab

        static WordCardView BuildCardPrefab(Sprites sprites)
        {
            var speaker = AssetDatabase.LoadAssetAtPath<Sprite>(HirayaGameplaySetup.IconFolder + "/SpeakerIcon.png");
            Sprite dashed = MakeDashedBorder();

            var go = new GameObject("WordCard", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            var view = go.AddComponent<WordCardView>();

            StretchedRounded(rect, "Border", LineGray, sprites.rounded, 30f, 3f);
            Image fill = StretchedRounded(rect, "Fill", Color.white, sprites.rounded, 30f, 0f);

            // Picture area with the picture, the first letter and the lock.
            Card(rect, "PictureArea", new Vector2(16f, 16f), new Vector2(353f, 170f), 24f, PictureGray, PictureGray, sprites.rounded);
            Image picture = IconAt(rect, "Picture", new Vector2(40f, 22f), new Vector2(305f, 158f), null, Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            TextMeshProUGUI letter = TextAt(rect, "Letter", "M", 110f, new Color(0.76f, 0.76f, 0.74f, 1f), FontStyles.Bold, new Vector2(16f, 16f), new Vector2(353f, 170f), TextAlignmentOptions.Center);
            Image lockIcon = IconAt(rect, "LockIcon", new Vector2(16f + 353f * 0.5f - 34f, 16f + 85f - 34f), Vector2.one * 68f, sprites.lockIcon, new Color(1f, 1f, 1f, 0.5f));
            lockIcon.raycastTarget = false;

            TextMeshProUGUI word = TextAt(rect, "Word", "mansanas", 38f, Ink, FontStyles.Bold, new Vector2(26f, 204f), new Vector2(250f, 48f), TextAlignmentOptions.MidlineLeft);
            word.enableAutoSizing = true;
            word.fontSizeMin = 24f;
            word.fontSizeMax = 38f;
            TextMeshProUGUI category = TextAt(rect, "Category", "Prutas", 25f, SoftInk, FontStyles.Normal, new Vector2(26f, 254f), new Vector2(250f, 34f), TextAlignmentOptions.MidlineLeft);

            Image speakCircle = RoundedAt(rect, "SpeakButton", new Vector2(295f, 218f), new Vector2(68f, 68f), new Color(Orange.r, Orange.g, Orange.b, 0.15f), sprites.rounded);
            IconAt(speakCircle.transform, "Icon", Vector2.zero, Vector2.one * 38f, speaker, Orange, true).raycastTarget = false;
            var speakButton = speakCircle.gameObject.AddComponent<Button>();
            speakButton.transition = Selectable.Transition.None;
            speakButton.targetGraphic = speakCircle;

            // The dashed outline of a locked card sits on top of the fill.
            Image dashedImage = NewStretched("DashedBorder", rect).gameObject.AddComponent<Image>();
            dashedImage.sprite = dashed;
            dashedImage.raycastTarget = false;

            var so = new SerializedObject(view);
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("border").objectReferenceValue = rect.Find("Border").GetComponent<Image>();
            so.FindProperty("dashedBorder").objectReferenceValue = dashedImage;
            so.FindProperty("picture").objectReferenceValue = picture;
            so.FindProperty("letter").objectReferenceValue = letter;
            so.FindProperty("lockIcon").objectReferenceValue = lockIcon.gameObject;
            so.FindProperty("word").objectReferenceValue = word;
            so.FindProperty("category").objectReferenceValue = category;
            so.FindProperty("speakButton").objectReferenceValue = speakButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            HirayaAudioSetup.AddClickSounds(go);   // the speaker button plays the click
            HirayaLanguage.AddLargeText(go);       // the Large text switch
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CardPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<WordCardView>();
        }

        /// <summary>A rounded outline made of dashes, exactly the size of a card (so it is never stretched).</summary>
        static Sprite MakeDashedBorder()
        {
            const float w = CardWidth;
            const float h = CardHeight;
            Shape outer = RoundRect(0f, 0f, w, h, 30f);
            Shape inner = RoundRect(3f, 3f, w - 3f, h - 3f, 27f);
            Shape dashes = (x, y) =>
            {
                if (!outer(x, y) || inner(x, y)) return false;
                float toLeft = x, toRight = w - x, toBottom = y, toTop = h - y;
                float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
                float along;   // how far we are along the outline, so the dashes run around the card
                if (nearest == toTop) along = x;
                else if (nearest == toRight) along = w + (h - y);
                else if (nearest == toBottom) along = w + h + (w - x);
                else along = 2f * w + h + y;
                return Mathf.Repeat(along, 30f) < 18f;
            };

            var painter = new Painter((int)w, (int)h);
            painter.Fill(dashes, new Color(0.74f, 0.74f, 0.72f, 1f));
            return SaveSprite(BookFolder + "/DashedCardBorder.png", painter, Vector4.zero);
        }
    }
}
