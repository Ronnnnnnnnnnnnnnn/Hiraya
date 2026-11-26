using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Hiraya.EditorTools.HirayaTitleSetup;
using static Hiraya.EditorTools.HirayaWorldMapSetup;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Builds the parent / teacher dashboard (wireframe 10) and the small gate in front of it as two panels of the TitleScreen prefab:
    ///   ParentGatePanel      (a sum to solve, so a child does not open the dashboard by accident)
    ///   ParentDashboardPanel (the child, 4 weeks of progress, the skills and the recent lessons)
    /// They are opened by the "Parent / Teacher" button of the title screen, the "Parent" button of the world map and the
    /// "Buksan ang Dashboard" button of the settings.
    /// </summary>
    public static class HirayaParentSetup
    {
        static readonly Color PictureGray = new Color(0.93f, 0.93f, 0.91f, 1f);
        static readonly Color Warning = new Color(0.80f, 0.25f, 0.15f, 1f);

        // ------------------------------------------------------------------ menu: a history to look at

        public static void AddSampleHistoryMenu()
        {
            if (!EditorUtility.DisplayDialog("Sample history",
                    "This replaces the saved progress history of the game (the numbers on the parent dashboard) with made-up numbers. Stars and learned words are not changed.", "Replace", "Cancel"))
                return;
            AddSampleHistory();
        }

        public static void ClearHistory()
        {
            ProgressLog.Clear();
        }

        /// <summary>Four weeks of words (the accuracy goes up from 40% to 82%), three lessons and a few days in a row.</summary>
        public static void AddSampleHistory()
        {
            ProgressLog.Clear();
            DateTime now = DateTime.UtcNow;
            string[] words = { "upuan", "hapag", "kama", "aso", "pusa", "mansanas", "saging", "ubas", "pula", "dilaw" };
            float[] accuracy = { 0.40f, 0.54f, 0.66f, 0.82f };   // oldest week first
            var random = new System.Random(7);
            for (int week = 0; week < 4; week++)
            {
                int right = Mathf.RoundToInt(accuracy[week] * 10f);
                for (int i = 0; i < 10; i++)
                {
                    // Spread over the 7 days of that week (weeksAgo = 3 - week).
                    DateTime when = now.AddDays(-7 * (3 - week) - 0.5 - i * 0.6);
                    bool firstTry = i < right;
                    int spelling = random.NextDouble() < accuracy[week] - 0.1 ? 0 : (random.NextDouble() < 0.2 ? -1 : 1);
                    int sentence = random.NextDouble() < accuracy[week] - 0.2 ? 1 : 2;   // fill in the blank
                    ProgressLog.AddWord(words[i % words.Length], firstTry, spelling, sentence, when);
                }
            }
            ProgressLog.AddLesson("Tahanan", 3, 5, 2, 238, 3, 5, "upuan, hapag, kama", now.AddDays(-2).AddHours(-3));
            ProgressLog.AddLesson("Palengke", 4, 5, 2, 175, 4, 5, "mansanas, saging, ubas", now.AddDays(-1).AddHours(-1));
            ProgressLog.AddLesson("Paaralan", 4, 5, 3, 198, 3, 4, "aso, pusa, pula, dilaw", now.AddHours(-2));
            ProgressLog.MarkPlayed(now.AddDays(-2));
            ProgressLog.MarkPlayed(now.AddDays(-1));
            ProgressLog.MarkPlayed(now);
        }

        // ------------------------------------------------------------------ the gate

        internal static ParentGateController BuildGate(Transform root, Sprites sprites, out RectTransform panel)
        {
            panel = NewStretched("ParentGatePanel", root);
            var controller = panel.gameObject.AddComponent<ParentGateController>();

            Image backIcon;
            Button back = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)back.transform).pivot = new Vector2(0f, 1f);

            RectTransform card = Card(panel, "Card", new Vector2(560f, 170f), new Vector2(800f, 740f), 48f, Color.white, LineGray, sprites.rounded);
            IconAt(card, "LockIcon", new Vector2(364f, 40f), Vector2.one * 72f, sprites.lockIcon, Color.white).raycastTarget = false;
            TextAt(card, "Title", "Para sa magulang at guro", 46f, Ink, FontStyles.Bold, new Vector2(0f, 126f), new Vector2(800f, 64f), TextAlignmentOptions.Center);
            TextAt(card, "Hint", "Sagutin ang sum para makapasok", 28f, SoftInk, FontStyles.Normal, new Vector2(0f, 196f), new Vector2(800f, 40f), TextAlignmentOptions.Center);
            TextMeshProUGUI question = TextAt(card, "Question", "7 + 5 = ?", 96f, Ink, FontStyles.Bold, new Vector2(0f, 268f), new Vector2(800f, 130f), TextAlignmentOptions.Center);

            var buttons = new Button[4];
            var labels = new TMP_Text[4];
            for (int i = 0; i < buttons.Length; i++)
            {
                RectTransform answer = Card(card, "Answer_" + (i + 1), new Vector2(55f + i * 180f, 440f), new Vector2(150f, 130f), 34f, Color.white, LineGray, sprites.rounded);
                labels[i] = TextAt(answer, "Label", "0", 64f, Ink, FontStyles.Bold, new Vector2(0f, 24f), new Vector2(150f, 82f), TextAlignmentOptions.Center);
                buttons[i] = answer.gameObject.AddComponent<Button>();
                buttons[i].transition = Selectable.Transition.None;
                buttons[i].targetGraphic = answer.Find("Fill").GetComponent<Image>();
            }
            TextMeshProUGUI status = TextAt(card, "Status", string.Empty, 32f, Warning, FontStyles.Bold, new Vector2(0f, 612f), new Vector2(800f, 50f), TextAlignmentOptions.Center);

            var so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("question").objectReferenceValue = question;
            so.FindProperty("status").objectReferenceValue = status;
            SerializedProperty buttonsProperty = so.FindProperty("answerButtons");
            SerializedProperty labelsProperty = so.FindProperty("answerLabels");
            buttonsProperty.arraySize = labelsProperty.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                labelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        // ------------------------------------------------------------------ the dashboard

        internal static ParentDashboardController BuildDashboard(Transform root, Sprites sprites, Sprite boyPortrait, Sprite girlPortrait, out RectTransform panel)
        {
            panel = NewStretched("ParentDashboardPanel", root);
            var controller = panel.gameObject.AddComponent<ParentDashboardController>();

            // ---------------------------------------------------------------- top
            Image backIcon;
            Button back = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)back.transform).pivot = new Vector2(0f, 1f);
            IconAt(panel, "LockIcon", new Vector2(162f, 50f), Vector2.one * 44f, sprites.lockIcon, Color.white).raycastTarget = false;
            TextAt(panel, "Title", "Parent & Teacher", 56f, Ink, FontStyles.Bold, new Vector2(218f, 34f), new Vector2(800f, 72f), TextAlignmentOptions.MidlineLeft);
            TextAt(panel, "Subtitle", "Dashboard ng Progreso · para sa magulang at guro", 26f, SoftInk, FontStyles.Normal, new Vector2(162f, 104f), new Vector2(900f, 36f), TextAlignmentOptions.MidlineLeft);
            Button export = OutlinePill(panel, "ExportButton", "I-export ang Ulat", null, new Vector2(1300f, 52f), new Vector2(360f, 76f), sprites);
            Button exit = OutlinePill(panel, "ExitButton", "Lumabas", null, new Vector2(1690f, 52f), new Vector2(190f, 76f), sprites);
            TextMeshProUGUI exportStatus = TextAt(panel, "ExportStatus", string.Empty, 22f, SoftInk, FontStyles.Normal, new Vector2(560f, 136f), new Vector2(1320f, 30f), TextAlignmentOptions.MidlineRight);

            // ---------------------------------------------------------------- the child (left card)
            RectTransform child = Card(panel, "ProfileCard", new Vector2(60f, 180f), new Vector2(440f, 410f), 36f, Color.white, LineGray, sprites.rounded);
            RoundedAt(child, "PortraitCircle", new Vector2(28f, 28f), new Vector2(88f, 88f), PictureGray, sprites.rounded);
            Image portrait = IconAt(child, "Portrait", new Vector2(28f, 24f), new Vector2(88f, 96f), boyPortrait, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.rectTransform.sizeDelta = new Vector2(112f, 112f);
            portrait.rectTransform.anchoredPosition = new Vector2(72f, -72f);
            TextMeshProUGUI childName = TextAt(child, "Name", "Fin", 38f, Ink, FontStyles.Bold, new Vector2(136f, 28f), new Vector2(280f, 48f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI childInfo = TextAt(child, "Info", "6 taong gulang", 22f, SoftInk, FontStyles.Normal, new Vector2(136f, 78f), new Vector2(280f, 34f), TextAlignmentOptions.MidlineLeft);
            childInfo.enableAutoSizing = true;
            childInfo.fontSizeMin = 15f;
            childInfo.fontSizeMax = 22f;

            RectTransform tag = NewRect("StatusTag", child);
            Place(tag, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -138f), new Vector2(384f, 46f));
            Image tagFill = AddRounded(tag, "Fill", new Vector2(384f, 46f), new Color(0.85f, 0.85f, 0.83f, 1f), sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            tagFill.raycastTarget = false;
            TextMeshProUGUI tagText = AddText(tag, "Text", "Wala pang datos", 22f, SoftInk, FontStyles.Bold, new Vector2(370f, 40f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);
            tagText.enableAutoSizing = true;
            tagText.fontSizeMin = 15f;
            tagText.fontSizeMax = 22f;

            TextMeshProUGUI starsValue = StatRow(child, "Stars", "Kabuuang bituin", 208f, sprites);
            TextMeshProUGUI wordsValue = StatRow(child, "Words", "Natutunang salita", 260f, sprites);
            TextMeshProUGUI streakValue = StatRow(child, "Streak", "Sunod-sunod na araw", 312f, sprites);
            TextMeshProUGUI lastPlayed = TextAt(child, "LastPlayed", "Huling laro: wala pa", 22f, SoftInk, FontStyles.Normal, new Vector2(28f, 366f), new Vector2(384f, 32f), TextAlignmentOptions.MidlineLeft);

            // ---------------------------------------------------------------- four weeks (middle card)
            RectTransform weeksCard = Card(panel, "WeeksCard", new Vector2(520f, 180f), new Vector2(760f, 410f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(weeksCard, "Title", "Pag-unlad sa 4 na Linggo", 36f, Ink, FontStyles.Bold, new Vector2(30f, 24f), new Vector2(700f, 48f), TextAlignmentOptions.MidlineLeft);
            TextAt(weeksCard, "Subtitle", "Tamang sagot sa quiz kada linggo", 22f, SoftInk, FontStyles.Normal, new Vector2(30f, 72f), new Vector2(700f, 30f), TextAlignmentOptions.MidlineLeft);
            var weeks = new ParentDashboardController.WeekColumn[4];
            for (int i = 0; i < weeks.Length; i++) weeks[i] = WeekColumn(weeksCard, i, new Vector2(80f + i * 160f, 112f), sprites);

            // ---------------------------------------------------------------- skills (right card)
            RectTransform skillsCard = Card(panel, "SkillsCard", new Vector2(1300f, 180f), new Vector2(560f, 410f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(skillsCard, "Title", "Kasanayan", 36f, Ink, FontStyles.Bold, new Vector2(30f, 24f), new Vector2(500f, 48f), TextAlignmentOptions.MidlineLeft);
            TextAt(skillsCard, "Subtitle", "Batay sa mga sagot at pagbaybay", 22f, SoftInk, FontStyles.Normal, new Vector2(30f, 72f), new Vector2(500f, 30f), TextAlignmentOptions.MidlineLeft);
            string[] skillNames = { "Bokabularyo", "Pagbaybay", "Pangungusap" };
            var skills = new ParentDashboardController.SkillRow[skillNames.Length];
            for (int i = 0; i < skills.Length; i++) skills[i] = SkillRow(skillsCard, i, skillNames[i], 124f + i * 84f, sprites);
            TextMeshProUGUI focusNote = TextAt(skillsCard, "FocusNote", "Pangunahing pokus: —", 22f, SoftInk, FontStyles.Normal, new Vector2(30f, 372f), new Vector2(500f, 30f), TextAlignmentOptions.MidlineLeft);

            // ---------------------------------------------------------------- recent lessons (table)
            RectTransform table = Card(panel, "LessonsCard", new Vector2(60f, 610f), new Vector2(1800f, 420f), 36f, Color.white, LineGray, sprites.rounded);
            TextAt(table, "Title", "Mga Kamakailang Pagsusulit", 34f, Ink, FontStyles.Bold, new Vector2(36f, 22f), new Vector2(900f, 46f), TextAlignmentOptions.MidlineLeft);
            float[] columnX = { 40f, 230f, 440f, 1000f, 1190f, 1420f, 1600f };
            float[] columnWidth = { 170f, 190f, 540f, 170f, 210f, 160f, 170f };
            string[] headers = { "PETSA", "LUGAR", "MGA SALITA", "TAMA", "PAGBAYBAY", "BITUIN", "TAGAL" };
            for (int i = 0; i < headers.Length; i++)
                TextAt(table, "Header_" + (i + 1), headers[i], 20f, SoftInk, FontStyles.Normal, new Vector2(columnX[i], 84f), new Vector2(columnWidth[i], 30f), TextAlignmentOptions.MidlineLeft, 2f);

            var lessons = new ParentDashboardController.LessonRow[5];
            for (int i = 0; i < lessons.Length; i++) lessons[i] = LessonRow(table, i, 118f + i * 59f, columnX, columnWidth, sprites);
            GameObject noLessons = TextAt(table, "NoLessons", "Wala pang naitalang aralin. Maglaro muna para magkaroon ng ulat.", 28f, SoftInk, FontStyles.Normal,
                                          new Vector2(0f, 220f), new Vector2(1800f, 50f), TextAlignmentOptions.Center).gameObject;

            // ---------------------------------------------------------------- connect
            var so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("exitButton").objectReferenceValue = exit;
            so.FindProperty("exportButton").objectReferenceValue = export;
            so.FindProperty("exportStatus").objectReferenceValue = exportStatus;
            so.FindProperty("childName").objectReferenceValue = childName;
            so.FindProperty("childInfo").objectReferenceValue = childInfo;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("boyPortrait").objectReferenceValue = boyPortrait;
            so.FindProperty("girlPortrait").objectReferenceValue = girlPortrait;
            so.FindProperty("statusTag").objectReferenceValue = tagText;
            so.FindProperty("statusTagFill").objectReferenceValue = tagFill;
            so.FindProperty("starsValue").objectReferenceValue = starsValue;
            so.FindProperty("wordsValue").objectReferenceValue = wordsValue;
            so.FindProperty("streakValue").objectReferenceValue = streakValue;
            so.FindProperty("lastPlayed").objectReferenceValue = lastPlayed;
            so.FindProperty("tallestBar").floatValue = 186f;
            so.FindProperty("focusNote").objectReferenceValue = focusNote;
            so.FindProperty("noLessonsText").objectReferenceValue = noLessons;

            SerializedProperty weeksProperty = so.FindProperty("weeks");
            weeksProperty.arraySize = weeks.Length;
            for (int i = 0; i < weeks.Length; i++)
            {
                SerializedProperty w = weeksProperty.GetArrayElementAtIndex(i);
                w.FindPropertyRelative("bar").objectReferenceValue = weeks[i].bar;
                w.FindPropertyRelative("barImage").objectReferenceValue = weeks[i].barImage;
                w.FindPropertyRelative("percent").objectReferenceValue = weeks[i].percent;
                w.FindPropertyRelative("label").objectReferenceValue = weeks[i].label;
            }
            SerializedProperty skillsProperty = so.FindProperty("skills");
            skillsProperty.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
            {
                SerializedProperty k = skillsProperty.GetArrayElementAtIndex(i);
                k.FindPropertyRelative("value").objectReferenceValue = skills[i].value;
                k.FindPropertyRelative("fill").objectReferenceValue = skills[i].fill;
            }
            SerializedProperty lessonsProperty = so.FindProperty("lessons");
            lessonsProperty.arraySize = lessons.Length;
            for (int i = 0; i < lessons.Length; i++)
            {
                SerializedProperty l = lessonsProperty.GetArrayElementAtIndex(i);
                l.FindPropertyRelative("root").objectReferenceValue = lessons[i].root;
                l.FindPropertyRelative("date").objectReferenceValue = lessons[i].date;
                l.FindPropertyRelative("place").objectReferenceValue = lessons[i].place;
                l.FindPropertyRelative("words").objectReferenceValue = lessons[i].words;
                l.FindPropertyRelative("correct").objectReferenceValue = lessons[i].correct;
                l.FindPropertyRelative("spelling").objectReferenceValue = lessons[i].spelling;
                l.FindPropertyRelative("stars").objectReferenceValue = lessons[i].stars;
                l.FindPropertyRelative("time").objectReferenceValue = lessons[i].time;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        /// <summary>A grey label on the left, a big number on the right and a thin line under them. Returns the number.</summary>
        static TextMeshProUGUI StatRow(Transform parent, string id, string label, float y, Sprites sprites)
        {
            TextAt(parent, id + "Label", label, 24f, SoftInk, FontStyles.Normal, new Vector2(28f, y), new Vector2(250f, 40f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI value = TextAt(parent, id + "Value", "0", 32f, Ink, FontStyles.Bold, new Vector2(262f, y - 2f), new Vector2(150f, 44f), TextAlignmentOptions.MidlineRight);
            RoundedAt(parent, id + "Line", new Vector2(28f, y + 46f), new Vector2(384f, 2f), LineGray, sprites.rounded).raycastTarget = false;
            return value;
        }

        /// <summary>One column of the bar chart: the bar grows up from the bottom, its number sits above it and "Linggo n" below.</summary>
        static ParentDashboardController.WeekColumn WeekColumn(Transform parent, int index, Vector2 topLeft, Sprites sprites)
        {
            RectTransform column = NewRect("Week_" + (index + 1), parent);
            Place(column, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(topLeft.x, -topLeft.y), new Vector2(120f, 280f));

            Image bar = AddRounded(column, "Bar", new Vector2(120f, 100f), new Color(0.85f, 0.85f, 0.83f, 1f), sprites.rounded, new Vector2(0.5f, 0f), new Vector2(0f, 40f));
            bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bar.pixelsPerUnitMultiplier = 64f / 18f;   // the corners stay 18 px big whatever the height
            bar.raycastTarget = false;

            TextMeshProUGUI percent = AddText(column, "Percent", "—", 30f, Ink, FontStyles.Bold, new Vector2(140f, 38f), new Vector2(0.5f, 0f), new Vector2(0f, 148f), 0f);
            percent.rectTransform.pivot = new Vector2(0.5f, 0f);
            TextMeshProUGUI label = AddText(column, "Label", "Linggo " + (index + 1), 22f, SoftInk, FontStyles.Normal, new Vector2(140f, 32f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);

            return new ParentDashboardController.WeekColumn { bar = bar.rectTransform, barImage = bar, percent = percent, label = label };
        }

        /// <summary>A skill: its name, its percentage and an orange bar on a grey track.</summary>
        static ParentDashboardController.SkillRow SkillRow(Transform parent, int index, string name, float y, Sprites sprites)
        {
            string id = "Skill_" + (index + 1);
            TextAt(parent, id + "Name", name, 28f, Ink, FontStyles.Normal, new Vector2(30f, y), new Vector2(300f, 36f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI value = TextAt(parent, id + "Value", "—", 28f, Ink, FontStyles.Bold, new Vector2(330f, y), new Vector2(200f, 36f), TextAlignmentOptions.MidlineRight);
            Image track = RoundedAt(parent, id + "Track", new Vector2(30f, y + 46f), new Vector2(500f, 16f), LineGray, sprites.rounded);
            track.raycastTarget = false;
            track.pixelsPerUnitMultiplier = 64f / 8f;

            RectTransform fill = NewRect("Fill", track.transform);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = sprites.rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Orange;
            fillImage.pixelsPerUnitMultiplier = 64f / 8f;
            fillImage.raycastTarget = false;

            return new ParentDashboardController.SkillRow { value = value, fill = fill };
        }

        /// <summary>One row of the lessons table: a thin line on top and seven texts.</summary>
        static ParentDashboardController.LessonRow LessonRow(Transform parent, int index, float y, float[] x, float[] width, Sprites sprites)
        {
            RectTransform row = NewRect("Row_" + (index + 1), parent);
            Place(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y), new Vector2(1800f, 58f));
            RoundedAt(row, "Line", new Vector2(40f, 0f), new Vector2(1720f, 2f), LineGray, sprites.rounded).raycastTarget = false;

            TextMeshProUGUI Cell(string name, int column, bool bold)
            {
                TextMeshProUGUI text = TextAt(row, name, "", 28f, Ink, bold ? FontStyles.Bold : FontStyles.Normal, new Vector2(x[column], 8f), new Vector2(width[column], 42f), TextAlignmentOptions.MidlineLeft);
                text.overflowMode = TextOverflowModes.Ellipsis;
                return text;
            }
            return new ParentDashboardController.LessonRow
            {
                root = row.gameObject,
                date = Cell("Date", 0, false), place = Cell("Place", 1, false), words = Cell("Words", 2, false),
                correct = Cell("Correct", 3, true), spelling = Cell("Spelling", 4, true), stars = Cell("Stars", 5, true), time = Cell("Time", 6, false)
            };
        }
    }
}
