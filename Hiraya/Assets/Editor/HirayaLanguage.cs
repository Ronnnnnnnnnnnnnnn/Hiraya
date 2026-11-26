using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only: turns the Filipino texts written by the screen builders into English, keeping the Filipino as the
    /// other language. A text that no script changes gets a LocalizedText component (both versions editable in the
    /// Inspector); a text that a script fills in only gets the English placeholder.
    /// Called by the builders just before a screen prefab is saved.
    /// </summary>
    internal static class HirayaLanguage
    {
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            // title, age, world map
            { "Isang Paglalakbay sa Wikang Filipino", "A Journey into the Filipino Language" },
            { "MAGLARO", "PLAY" },
            { "PUMILI NG BATA", "CHOOSE A PLAYER" },
            { "Ilang taon ka na?", "How old are you?" },
            { "Piliin ang iyong edad", "Choose your age" },
            { "Susunod", "Next" },
            { "Mga Lugar", "Places" },
            { "Tahanan", "Home" },
            { "Palengke", "Market" },
            { "Paaralan", "School" },
            { "Hardin", "Park" },
            { "Bundok", "Mountain" },
            { "Aking Salita Book", "My Word Book" },
            { "Kabuuang bituin", "Total stars" },
            { "Pahinga", "Rest" },
            { "SUSUNOD NA LUGAR", "NEXT PLACE" },
            { "Maglaro", "Play" },
            // settings
            { "Mga Setting", "Settings" },
            { "Para sa bata at sa magulang", "For children and parents" },
            { "Tunog", "Sound" },
            { "Ayusin ang tunog ng laro", "Adjust the game sounds" },
            { "Musika", "Music" },
            { "Tunog ng Pindot", "Button Sounds" },
            { "Boses ng Tagapagsalita", "Narrator Voice" },
            { "I-mute ang lahat ng tunog", "Mute all sounds" },
            { "Wika", "Language" },
            { "Piliin ang wika ng laro", "Choose the game language" },
            { "Para mas madali ang paglalaro", "To make playing easier" },
            { "Malaking teksto", "Large text" },
            { "Mas malalaki ang mga letra", "Bigger letters" },
            { "Para sa mga magulang at guro", "For parents and teachers" },
            { "Buksan ang Dashboard", "Open Dashboard" },
            // word book
            { "Koleksyon ng mga natutunang salita", "Words you have learned" },
            { "Mga salita sa bawat lugar", "The words of each place" },
            // parent gate and dashboard
            { "Para sa magulang at guro", "For parents and teachers" },
            { "Sagutin ang sum para makapasok", "Solve the sum to continue" },
            { "Dashboard ng Progreso · para sa magulang at guro", "Progress dashboard · for parents and teachers" },
            { "I-export ang Ulat", "Export Report" },
            { "Lumabas", "Exit" },
            { "Natutunang salita", "Words learned" },
            { "Sunod-sunod na araw", "Days in a row" },
            { "Pag-unlad sa 4 na Linggo", "Progress over 4 weeks" },
            { "Tamang sagot sa quiz kada linggo", "Correct quiz answers each week" },
            { "Kasanayan", "Skills" },
            { "Batay sa mga sagot at pagbaybay", "Based on answers and spelling" },
            { "Bokabularyo", "Vocabulary" },
            { "Pagbaybay", "Spelling" },
            { "Pangungusap", "Sentences" },
            { "Mga Kamakailang Pagsusulit", "Recent Lessons" },
            { "PETSA", "DATE" },
            { "LUGAR", "PLACE" },
            { "MGA SALITA", "WORDS" },
            { "TAMA", "CORRECT" },
            { "PAGBAYBAY", "SPELLING" },
            { "BITUIN", "STARS" },
            { "TAGAL", "TIME" },
            { "Wala pang naitalang aralin. Maglaro muna para magkaroon ng ulat.", "No lessons recorded yet. Play first to get a report." },
            // HUD, quiz, spelling, pause, results
            { "LARAWAN", "PICTURE" },
            { "Ano ang tawag dito?", "What is this called?" },
            { "Bagong salita", "New word" },
            { "Tingnan", "Look" },
            { "Punan ang Patlang", "Fill in the Blank" },
            { "Piliin ang salitang bubuo sa pangungusap", "Choose the word that completes the sentence" },
            { "Magpatuloy", "Continue" },
            { "Ispel ang Salita", "Spell the Word" },
            { "Pakinggan", "Listen" },
            { "Pahiwatig", "Hint" },
            { "Anong salita ang nasa larawan?", "What word is in the picture?" },
            { "I-tap ang mga titik sa tamang pagkakasunod-sunod", "Tap the letters in the right order" },
            { "MGA TITIK", "LETTERS" },
            { "Burahin", "Erase" },
            { "Suriin", "Check" },
            { "Naka-pause", "Paused" },
            { "Ituloy", "Resume" },
            { "Uliting ang antas", "Restart Level" },
            { "Bumalik sa Mapa", "Back to Map" },
            { "Uliting", "Replay" },
            { "TAMANG SAGOT", "CORRECT" },
            { "BAGONG SALITA", "NEW WORDS" },
            { "PAGSUBOK", "ATTEMPT" },
            { "MGA SALITANG NATUTUNAN", "WORDS LEARNED" }
        };

        /// <summary>Lets every text of a screen follow the Large text switch of the settings.</summary>
        public static void AddLargeText(GameObject root)
        {
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                if (label.GetComponent<LargeTextSupport>() == null) label.gameObject.AddComponent<LargeTextSupport>();
        }

        public static void Apply(GameObject root)
        {
            var filledByScripts = new HashSet<TMP_Text>();
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour is LocalizedText || behaviour.GetType().Namespace != "Hiraya") continue;
                SerializedProperty property = new SerializedObject(behaviour).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is TMP_Text text)
                        filledByScripts.Add(text);
            }

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string english;
                if (label.GetComponent<LocalizedText>() != null || !English.TryGetValue(label.text, out english)) continue;
                string filipino = label.text;
                label.text = english;
                if (filledByScripts.Contains(label)) continue;

                var localized = label.gameObject.AddComponent<LocalizedText>();
                var so = new SerializedObject(localized);
                so.FindProperty("english").stringValue = english;
                so.FindProperty("filipino").stringValue = filipino;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
