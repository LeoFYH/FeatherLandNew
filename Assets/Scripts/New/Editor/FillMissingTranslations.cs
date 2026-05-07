#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 一次性填补 5 个 key 全部 8 种语言的翻译（覆盖现有值）
    /// </summary>
    public static class FillMissingTranslations
    {
        private const string ConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        // (key, [EN, SC, TC, DE, FR, RU, ES, PT])
        private static readonly Dictionary<string, string[]> Data = new Dictionary<string, string[]>
        {
            // 1. Owl
            { "Owl", new []
                {
                    "pharaoh eagle-owl",
                    "雕鸮",
                    "雕鴞",
                    "Pharaonenuhu",
                    "Grand-duc du désert",
                    "Фараонов филин",
                    "Búho real del desierto",
                    "Bufo-faraó",
                }},
            // 2. EgyptianVulture
            { "EgyptianVulture", new []
                {
                    "Egyptian Vulture",
                    "埃及秃鹫",
                    "埃及禿鷲",
                    "Schmutzgeier",
                    "Vautour percnoptère",
                    "Стервятник",
                    "Alimoche común",
                    "Abutre-do-egito",
                }},
            // 3. EgyptianVultureDescription
            { "EgyptianVultureDescription", new []
                {
                    "A gentle and clever Egyptian vulture living in the warm desert. It wanders softly across golden dunes, curiously looking around and thinking up little ideas.",
                    "一只温柔又有点小聪明的埃及秃鹫,生活在温暖的沙漠中。虽然环境有些荒凉,但它总能找到有趣的小宝藏,是个安静又可爱的沙漠小伙伴。",
                    "一隻溫柔又有點小聰明的埃及禿鷲,生活在溫暖的沙漠中。雖然環境有些荒涼,但它總能找到有趣的小寶藏,是個安靜又可愛的沙漠小夥伴。",
                    "Ein sanfter und kluger Schmutzgeier, der in der warmen Wüste lebt. Er streift gemächlich über goldene Dünen, schaut sich neugierig um und heckt kleine Ideen aus.",
                    "Un percnoptère doux et malin qui vit dans le désert chaud. Il déambule paisiblement sur les dunes dorées, observe avec curiosité et imagine de petites idées.",
                    "Кроткий и смышлёный египетский стервятник, обитающий в тёплой пустыне. Он неспешно бродит по золотистым дюнам, с любопытством оглядывается по сторонам и обдумывает свои маленькие задумки.",
                    "Un alimoche dulce e ingenioso que vive en el cálido desierto. Deambula con suavidad por las dunas doradas, mirando alrededor con curiosidad e ideando pequeñas ocurrencias.",
                    "Um abutre-do-egito gentil e esperto que vive no deserto cálido. Ele perambula tranquilamente pelas dunas douradas, olhando ao redor com curiosidade e bolando pequenas ideias.",
                }},
            // 4. African ostrich
            { "African ostrich", new []
                {
                    "African Ostrich",
                    "非洲鸵鸟",
                    "非洲鴕鳥",
                    "Afrikanischer Strauß",
                    "Autruche d'Afrique",
                    "Африканский страус",
                    "Avestruz africano",
                    "Avestruz-africano",
                }},
            // 5. Powdered Head Duck
            { "Powdered Head Duck", new []
                {
                    "Pink-headed Duck",
                    "粉红头鸭",
                    "粉紅頭鴨",
                    "Rosenkopfente",
                    "Canard à tête rose",
                    "Розовоголовая утка",
                    "Pato cabecirrosado",
                    "Pato-de-cabeça-rosa",
                }},
            // 6. ExitYesButton
            { "ExitYesButton", new []
                {
                    "Yes!",
                    "是的！",
                    "是的！",
                    "Ja!",
                    "Oui !",
                    "Да!",
                    "¡Sí!",
                    "Sim!",
                }},
            // 7. ExitNoButton
            { "ExitNoButton", new []
                {
                    "Just Quit",
                    "直接退出",
                    "直接退出",
                    "Direkt beenden",
                    "Quitter",
                    "Просто выйти",
                    "Salir sin más",
                    "Sair direto",
                }},
            // 8. Kingfisher
            { "Kingfisher", new []
                {
                    "Kingfisher",
                    "翠鸟",
                    "翠鳥",
                    "Eisvogel",
                    "Martin-pêcheur",
                    "Зимородок",
                    "Martín pescador",
                    "Martim-pescador",
                }},
            // 9-14: 设置面板音量 dropdown 下的 6 个 label
            { "VolumeEffect", new [] {
                "Effect", "音效", "音效",
                "Effekte", "Effets", "Эффекты", "Efectos", "Efeitos" }},
            { "VolumeEnvironment", new [] {
                "Environment", "环境", "環境",
                "Umgebung", "Environnement", "Окружение", "Ambiente", "Ambiente" }},
            { "VolumeMusic", new [] {
                "Music", "音乐", "音樂",
                "Musik", "Musique", "Музыка", "Música", "Música" }},
            { "VolumePetting", new [] {
                "Petting", "抚摸", "撫摸",
                "Streicheln", "Caresses", "Поглаживание", "Caricias", "Carícias" }},
            { "VolumeAlarm", new [] {
                "Alarm", "闹钟", "鬧鐘",
                "Alarm", "Alarme", "Будильник", "Alarma", "Alarme" }},
            { "VolumeMaster", new [] {
                "Master", "主音量", "主音量",
                "Master", "Principal", "Общая", "Principal", "Principal" }},
        };

        [MenuItem("Tools/本地化/补5key缺失翻译")]
        public static void Apply()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(ConfigPath);
            if (config == null || config.languageDic == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 LocalizationConfig", "OK");
                return;
            }

            // 列顺序：EN, SC, TC, DE, FR, RU, ES, PT
            var langOrder = new[]
            {
                SystemLanguage.English,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional,
                SystemLanguage.German,
                SystemLanguage.French,
                SystemLanguage.Russian,
                SystemLanguage.Spanish,
                SystemLanguage.Portuguese,
            };

            var sb = new System.Text.StringBuilder();
            int updates = 0;
            foreach (var entry in Data)
            {
                string key = entry.Key;
                string[] values = entry.Value;
                sb.AppendLine($"\n[{key}]");
                for (int i = 0; i < langOrder.Length; i++)
                {
                    var lang = langOrder[i];
                    string val = values[i];
                    if (!config.languageDic.TryGetValue(lang, out var langData) || langData == null)
                    {
                        sb.AppendLine($"  ⚠️ {lang}: 字典里没这个语言，跳过");
                        continue;
                    }
                    if (langData.words == null) langData.words = new Dictionary<string, Pattern>();
                    if (langData.words.TryGetValue(key, out var p) && p != null)
                    {
                        string old = p.text ?? "";
                        p.text = val;
                        sb.AppendLine($"  ✅ {lang}: \"{old}\" → \"{val}\"");
                    }
                    else
                    {
                        langData.words[key] = new Pattern { text = val };
                        sb.AppendLine($"  ✅ {lang}: <新增> → \"{val}\"");
                    }
                    updates++;
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sb.Insert(0, $"共更新 {updates} 项\n");
            Debug.Log("[FillMissing]" + sb);
            EditorUtility.DisplayDialog("完成", sb.ToString(), "OK");
        }
    }
}
#endif
