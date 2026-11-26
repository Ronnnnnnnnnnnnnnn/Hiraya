using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Hiraya.EditorTools.HirayaTitleSetup;
using static Hiraya.EditorTools.HirayaWorldMapSetup;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Builds the settings screen (wireframe 09) as the SettingsPanel inside the TitleScreen prefab:
    /// Tunog (three sliders and a mute switch), Wika (Filipino / English), Accessibility (large text) and the parent card.
    /// The sliders and switches are standard Unity UI Slider and Toggle components made of plain objects.
    /// The small Filipino texts under the headings are my reading of the wireframe; change them on the objects if they differ.
    /// </summary>
    internal static class HirayaSettingsSetup
    {
        public static SettingsController BuildPanel(Transform root, Sprites sprites, out RectTransform panel)
        {
            panel = NewStretched("SettingsPanel", root);
            var controller = panel.gameObject.AddComponent<SettingsController>();

            // ---------------------------------------------------------------- top
            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);
            TextAt(panel, "Title", "Mga Setting", 58f, Ink, FontStyles.Bold, new Vector2(162f, 34f), new Vector2(800f, 72f), TextAlignmentOptions.MidlineLeft);
            TextAt(panel, "Subtitle", "Para sa bata at sa magulang", 26f, SoftInk, FontStyles.Normal, new Vector2(162f, 104f), new Vector2(800f, 36f), TextAlignmentOptions.MidlineLeft);

            // ---------------------------------------------------------------- Tunog (left)
            RectTransform sound = Card(panel, "SoundCard", new Vector2(80f, 180f), new Vector2(800f, 800f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(sound, "Title", "Tunog", 42f, Ink, FontStyles.Bold, new Vector2(44f, 28f), new Vector2(600f, 56f), TextAlignmentOptions.MidlineLeft);
            TextAt(sound, "Subtitle", "Ayusin ang tunog ng laro", 24f, SoftInk, FontStyles.Normal, new Vector2(44f, 86f), new Vector2(700f, 34f), TextAlignmentOptions.MidlineLeft);

            SettingsController.SliderRow music = SliderRow(sound, "Music", "Musika", 165f, sprites);
            SettingsController.SliderRow sfx = SliderRow(sound, "Sfx", "Tunog ng Pindot", 335f, sprites);
            SettingsController.SliderRow voice = SliderRow(sound, "Voice", "Boses ng Tagapagsalita", 505f, sprites);

            SettingsController.SwitchRow muteAll = SwitchRow(sound, "MuteAll", new Vector2(44f, 690f), sprites);
            TextAt(sound, "MuteAllLabel", "I-mute ang lahat ng tunog", 28f, Ink, FontStyles.Normal, new Vector2(170f, 690f), new Vector2(560f, 56f), TextAlignmentOptions.MidlineLeft);

            // ---------------------------------------------------------------- Wika (right, top)
            RectTransform language = Card(panel, "LanguageCard", new Vector2(920f, 180f), new Vector2(920f, 250f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(language, "Title", "Wika", 42f, Ink, FontStyles.Bold, new Vector2(44f, 28f), new Vector2(600f, 56f), TextAlignmentOptions.MidlineLeft);
            TextAt(language, "Subtitle", "Piliin ang wika ng laro", 24f, SoftInk, FontStyles.Normal, new Vector2(44f, 86f), new Vector2(700f, 34f), TextAlignmentOptions.MidlineLeft);
            Image filipinoFill, englishFill;
            TextMeshProUGUI filipinoLabel, englishLabel;
            Button filipinoButton = LanguageButton(language, "FilipinoButton", "Filipino", new Vector2(44f, 140f), true, sprites, out filipinoFill, out filipinoLabel);
            Button englishButton = LanguageButton(language, "EnglishButton", "English", new Vector2(468f, 140f), false, sprites, out englishFill, out englishLabel);

            // ---------------------------------------------------------------- Accessibility (right, middle)
            RectTransform access = Card(panel, "AccessibilityCard", new Vector2(920f, 450f), new Vector2(920f, 250f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(access, "Title", "Accessibility", 42f, Ink, FontStyles.Bold, new Vector2(44f, 28f), new Vector2(600f, 56f), TextAlignmentOptions.MidlineLeft);
            TextAt(access, "Subtitle", "Para mas madali ang paglalaro", 24f, SoftInk, FontStyles.Normal, new Vector2(44f, 86f), new Vector2(700f, 34f), TextAlignmentOptions.MidlineLeft);
            TextAt(access, "LargeTextLabel", "Malaking teksto", 30f, Ink, FontStyles.Bold, new Vector2(44f, 138f), new Vector2(600f, 38f), TextAlignmentOptions.MidlineLeft);
            TextAt(access, "LargeTextHint", "Mas malalaki ang mga letra", 22f, SoftInk, FontStyles.Normal, new Vector2(44f, 178f), new Vector2(640f, 30f), TextAlignmentOptions.MidlineLeft);
            SettingsController.SwitchRow largeText = SwitchRow(access, "LargeText", new Vector2(772f, 148f), sprites);

            // ---------------------------------------------------------------- Parent / Teacher (right, bottom)
            RectTransform parent = Card(panel, "ParentCard", new Vector2(920f, 720f), new Vector2(920f, 260f), 36f, new Color(0.96f, 0.96f, 0.94f, 1f), LineGray, sprites.rounded);
            IconAt(parent, "LockIcon", new Vector2(44f, 26f), Vector2.one * 40f, sprites.lockIcon, Color.white).raycastTarget = false;
            TextAt(parent, "Title", "Parent / Teacher Controls", 30f, Ink, FontStyles.Bold, new Vector2(100f, 22f), new Vector2(700f, 44f), TextAlignmentOptions.MidlineLeft);
            TextAt(parent, "Hint", "Para sa mga magulang at guro", 22f, SoftInk, FontStyles.Normal, new Vector2(100f, 64f), new Vector2(700f, 30f), TextAlignmentOptions.MidlineLeft);
            Button dashboard = OutlinePill(parent, "DashboardButton", "Buksan ang Dashboard", null, new Vector2(44f, 132f), new Vector2(460f, 94f), sprites);

            // ---------------------------------------------------------------- connect to the controller
            var so = new SerializedObject(controller);
            Link(so.FindProperty("music"), music);
            Link(so.FindProperty("sfx"), sfx);
            Link(so.FindProperty("voice"), voice);
            Link(so.FindProperty("muteAll"), muteAll);
            Link(so.FindProperty("largeText"), largeText);
            so.FindProperty("filipinoButton").objectReferenceValue = filipinoButton;
            so.FindProperty("filipinoFill").objectReferenceValue = filipinoFill;
            so.FindProperty("filipinoLabel").objectReferenceValue = filipinoLabel;
            so.FindProperty("englishButton").objectReferenceValue = englishButton;
            so.FindProperty("englishFill").objectReferenceValue = englishFill;
            so.FindProperty("englishLabel").objectReferenceValue = englishLabel;
            so.FindProperty("dashboardButton").objectReferenceValue = dashboard;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        static void Link(SerializedProperty property, SettingsController.SliderRow row)
        {
            property.FindPropertyRelative("slider").objectReferenceValue = row.slider;
            property.FindPropertyRelative("percent").objectReferenceValue = row.percent;
        }

        static void Link(SerializedProperty property, SettingsController.SwitchRow row)
        {
            property.FindPropertyRelative("toggle").objectReferenceValue = row.toggle;
            property.FindPropertyRelative("track").objectReferenceValue = row.track;
            property.FindPropertyRelative("knob").objectReferenceValue = row.knob;
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>A label, a percentage and a slider (a normal Unity UI Slider: track, orange fill and a round handle).</summary>
        static SettingsController.SliderRow SliderRow(Transform parent, string id, string label, float y, Sprites sprites)
        {
            TextAt(parent, id + "Label", label, 30f, Ink, FontStyles.Normal, new Vector2(44f, y), new Vector2(560f, 40f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI percent = TextAt(parent, id + "Percent", "0%", 28f, SoftInk, FontStyles.Normal, new Vector2(616f, y), new Vector2(140f, 40f), TextAlignmentOptions.MidlineRight);

            RectTransform sliderRect = NewRect(id + "Slider", parent);
            Place(sliderRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -(y + 56f)), new Vector2(712f, 48f));

            // Track
            RectTransform track = NewRect("Background", sliderRect);
            Place(track, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 18f));
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 18f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = sprites.rounded;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = LineGray;
            trackImage.pixelsPerUnitMultiplier = 128f / 18f;

            // Orange fill (starts at the left end of the track, ends under the middle of the handle)
            RectTransform fillArea = NewRect("Fill Area", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.offsetMin = new Vector2(22f, -9f);
            fillArea.offsetMax = new Vector2(-22f, 9f);
            RectTransform fill = NewRect("Fill", fillArea);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = new Vector2(-22f, 0f);
            fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = sprites.rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Orange;
            fillImage.pixelsPerUnitMultiplier = 128f / 18f;

            // Handle
            RectTransform slideArea = NewRect("Handle Slide Area", sliderRect);
            slideArea.anchorMin = Vector2.zero;
            slideArea.anchorMax = Vector2.one;
            slideArea.offsetMin = new Vector2(22f, 0f);
            slideArea.offsetMax = new Vector2(-22f, 0f);
            RectTransform handle = NewRect("Handle", slideArea);
            handle.anchorMin = new Vector2(0.5f, 0f);
            handle.anchorMax = new Vector2(0.5f, 1f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(48f, 0f);
            var ring = handle.gameObject.AddComponent<Image>();
            ring.sprite = sprites.rounded;
            ring.type = Image.Type.Sliced;
            ring.color = LineGray;
            ring.pixelsPerUnitMultiplier = 128f / 48f;
            Image knob = AddRounded(handle, "Knob", Vector2.one * 42f, Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            knob.raycastTarget = false;

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = ring;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.transition = Selectable.Transition.None;
            slider.value = 0.5f;

            return new SettingsController.SliderRow { slider = slider, percent = percent };
        }

        /// <summary>An on / off switch: a standard Toggle whose track turns orange and whose round knob slides.</summary>
        static SettingsController.SwitchRow SwitchRow(Transform parent, string id, Vector2 topLeft, Sprites sprites)
        {
            RectTransform root = NewRect(id + "Switch", parent);
            Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(topLeft.x, -topLeft.y), new Vector2(104f, 56f));

            RectTransform trackRect = NewRect("Track", root);
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
            var track = trackRect.gameObject.AddComponent<Image>();
            track.sprite = sprites.rounded;
            track.type = Image.Type.Sliced;
            track.color = new Color(0.82f, 0.82f, 0.80f, 1f);
            track.pixelsPerUnitMultiplier = 128f / 56f;

            Image knob = AddRounded(root, "Knob", Vector2.one * 44f, Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(-24f, 0f));
            knob.raycastTarget = false;

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = false;

            return new SettingsController.SwitchRow { toggle = toggle, track = track, knob = knob.rectTransform };
        }

        static Button LanguageButton(Transform parent, string name, string label, Vector2 topLeft, bool selected, Sprites sprites,
                                     out Image fill, out TextMeshProUGUI text)
        {
            RectTransform root = Card(parent, name, topLeft, new Vector2(400f, 80f), 40f, selected ? Orange : Color.white, LineGray, sprites.rounded);
            fill = root.Find("Fill").GetComponent<Image>();
            text = TextAt(root, "Label", label, 32f, selected ? Color.white : Ink, FontStyles.Bold, new Vector2(0f, 10f), new Vector2(400f, 60f), TextAlignmentOptions.Center);
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return button;
        }
    }
}
