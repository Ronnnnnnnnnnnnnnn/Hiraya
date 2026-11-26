using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only: puts your art from Assets/2D asset/UI on the screens.
    ///   green  = the main button of a screen (Play, Next, Continue, Resume, Check)
    ///   yellow = the other buttons (Replay, Restart, Settings, Erase, Listen ...)
    ///   purple = leaving or the parent area (Back to Map, Parent / Teacher, Exit)
    /// The pressed picture (on-click_*) shows while a button is held. title_background.jpg goes behind the title and age
    /// screens. Copies cut to the button shape are made in Assets/UI/Skin; your files are not changed.
    /// </summary>
    public static class HirayaSkin
    {
        const string SourceFolder = "Assets/2D asset/UI";
        const string SkinFolder = "Assets/UI/Skin";
        const string AppLogoPath = SourceFolder + "/app_logo.jpg";

        enum Style { Green, Yellow, Purple }

        static readonly string[] TitleGreen = { "TitlePanel/PlayButton", "AgePanel/NextButton" };
        static readonly string[] TitleYellow = { "WorldMapPanel/PlacesCard/RestButton", "SettingsPanel/ParentCard/DashboardButton", "ParentDashboardPanel/ExportButton" };
        static readonly string[] TitlePurple = { "TitlePanel/ParentButton", "WorldMapPanel/PlacesCard/ParentButton", "ParentDashboardPanel/ExitButton" };

        static readonly string[] GameGreen = { "HUD/InteractButton", "WordPanel/NextButton", "SentencePanel/NextButton", "PausePanel/Menu/Card/ResumeButton", "QuizPanel/NextButton", "ResultsPanel/Card/NextButton", "SpellingPanel/SubmitButton" };
        static readonly string[] GameYellow =
        {
            "PausePanel/Menu/Card/RestartButton", "PausePanel/Menu/Card/SettingsButton", "PausePanel/SettingsScreen/SettingsPanel/ParentCard/DashboardButton",
            "ResultsPanel/Card/ReplayButton", "SpellingPanel/EraseButton", "SpellingPanel/ListenButton", "WordPanel/ListenButton", "SentencePanel/HintButton"
        };
        static readonly string[] GamePurple = { "PausePanel/Menu/Card/MapButton" };

        public static void ApplyToTitle(GameObject root)
        {
            Apply(root, TitleGreen, Style.Green);
            Apply(root, TitleYellow, Style.Yellow);
            Apply(root, TitlePurple, Style.Purple);
            AddBackground(root.transform.Find("TitlePanel"));
            AddBackground(root.transform.Find("AgePanel"));
        }

        public static void ApplyToGameplay(GameObject root)
        {
            Apply(root, GameGreen, Style.Green);
            Apply(root, GameYellow, Style.Yellow);
            Apply(root, GamePurple, Style.Purple);

            // The Check button is greyed while the boxes are not full: tint the art instead of changing its colour.
            foreach (SpellingScreen spelling in root.GetComponentsInChildren<SpellingScreen>(true))
            {
                var so = new SerializedObject(spelling);
                so.FindProperty("submitReadyFill").colorValue = Color.white;
                so.FindProperty("submitWaitingFill").colorValue = new Color(0.72f, 0.72f, 0.72f, 1f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void Apply(GameObject root, string[] paths, Style style)
        {
            string colour = style.ToString().ToLowerInvariant();
            Sprite normal = Cut(colour == "yellow" ? "yellow (1).png" : colour + ".png", "Button_" + style);
            Sprite pressed = Cut("on-click_" + colour + ".png", "Button_" + style + "Pressed");
            if (normal == null) return;

            foreach (string path in paths)
            {
                Transform item = root.transform.Find(path);
                if (item == null) { Debug.LogWarning("Hiraya skin: no button at " + path); continue; }
                var button = item.GetComponent<Button>();
                var image = button != null ? button.targetGraphic as Image : null;
                if (image == null) continue;

                var rect = (RectTransform)item;
                float height = rect.sizeDelta.y > 1f ? rect.sizeDelta.y : normal.rect.height;
                image.sprite = normal;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.pixelsPerUnitMultiplier = normal.rect.height / height;
                button.targetGraphic = image;
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { pressedSprite = pressed != null ? pressed : normal };

                Color ink = style == Style.Yellow ? new Color(0.20f, 0.14f, 0.05f, 1f) : Color.white;
                foreach (Image other in item.GetComponentsInChildren<Image>(true))
                {
                    if (other == image) continue;
                    if (other.name == "Border" || other.name == "Ring" || other.name == "Fill") other.enabled = false;
                    else other.color = ink;
                }
                foreach (TMP_Text text in item.GetComponentsInChildren<TMP_Text>(true)) text.color = ink;
            }
        }

        static void AddBackground(Transform panel)
        {
            if (panel == null || panel.Find("ArtBackground") != null) return;
            Sprite art = Background();
            if (art == null) return;
            var go = new GameObject("ArtBackground", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = art;
            image.raycastTarget = false;
            var fitter = go.AddComponent<AspectRatioFitter>();   // covers the whole screen on any shape of phone
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = art.rect.width / art.rect.height;
        }

        static Sprite Background()
        {
            string target = SkinFolder + "/TitleBackground.jpg";
            if (!File.Exists(target))
            {
                HirayaCharacterSetup.EnsureFolder(SkinFolder);
                File.Copy(SourceFolder + "/title_background.jpg", target);
            }
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        /// <summary>The button picture without its empty margin, as a nine-slice sprite with round ends.</summary>
        static Sprite Cut(string file, string name)
        {
            string target = SkinFolder + "/" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(target);
            if (existing != null) return existing;
            string path = SourceFolder + "/" + file;
            if (!File.Exists(path)) return null;
            HirayaCharacterSetup.EnsureFolder(SkinFolder);

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            int w = source.width, h = source.height;
            Color32[] pixels = source.GetPixels32();
            Object.DestroyImmediate(source);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (pixels[y * w + x].a > 40)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            var cut = new Color32[cw * ch];
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    cut[y * cw + x] = pixels[(minY + y) * w + minX + x];
            var made = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            made.SetPixels32(cut);
            File.WriteAllBytes(target, made.EncodeToPNG());
            Object.DestroyImmediate(made);

            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            float round = Mathf.Floor(ch * 0.5f) - 1f;
            importer.spriteBorder = new Vector4(round, round, round, round);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        // ------------------------------------------------------------------ app icon

        public static void SetAppIcon()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(AppLogoPath);
            if (logo == null) { Debug.LogWarning("Hiraya: no " + AppLogoPath); return; }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { logo }, IconKind.Any);
            AssetDatabase.SaveAssets();
        }
    }
}
