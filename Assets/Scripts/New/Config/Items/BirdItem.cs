using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BirdGame
{
    [CreateAssetMenu(menuName = "游戏配置/Bird", fileName = "BirdItem")]
    public class BirdItem : SerializedScriptableObject
    {
        [PreviewField(ObjectFieldAlignment.Left, Height = 50), HorizontalGroup("content", Width = 50), HideLabel]
        public Sprite preview;
        [HorizontalGroup("content"), VerticalGroup("content/Info"), LabelText("id")]
        public int id;
        [VerticalGroup("content/Info"), LabelText("鸟的预制体")]
        public GameObject prefab;
        [VerticalGroup("content/Info"), LabelText("是否能飞行")]
        public bool canFly = true;
        [VerticalGroup("content/Info"), LabelText("是否能飞行等待")]
        public bool canFlyWait = true;
        [VerticalGroup("content/Info"), LabelText("是否能横飞")]
        public bool canFlyHorizontal = true;
        [VerticalGroup("content/Info"), BoxGroup("content/Info/信息"), LabelText("稀有度"), ValueDropdown("rarityInfo"), GUIColor("realityColor"), OnValueChanged("OnRealityValueChanged")]
        public string reality;
        private Color32 realityColor = Color.white;
        [BoxGroup("content/Info/信息"), LabelText("每分钟收入(幼鸟)")]
        public float eraningForSmall;
        [BoxGroup("content/Info/信息"), LabelText("每分钟收入(成鸟)")]
        public float eraningForBig;
        [BoxGroup("content/Info/信息"), LabelText("价格(小)")]
        public float priceForSmall;
        [BoxGroup("content/Info/信息"), LabelText("价格(大)")]
        public float priceForBig;
        [BoxGroup("content/Info/信息"), LabelText("点击收益)")]
        public float clickEarning = 1;
        [BoxGroup("content/Info/信息"), LabelText("点击5次后的额外收益)")]
        public float clickEarningForFiveTimes = 3;
        [BoxGroup("content/Info/信息"), LabelText("描述的key"), TextArea]
        public string description;
        [BoxGroup("content/Info/信息"), Button("添加描述(key)到本地化配置"), GUIColor("buttonColor")]
        private void OnAddDescription()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(description))
            {
                EditorUtility.DisplayDialog("警告", "描述不能为空", "ok");
                return;
            }

            var config =
                AssetDatabase.LoadAssetAtPath<LocalizationConfig>("Assets/Prefabs/Config/LocalizationConfig.asset");
            foreach (var language in config.languageDic)
            {
                if (language.Value.words.ContainsKey(description))
                {
                    EditorUtility.DisplayDialog("警告", $"本地化配置中已经存在key: {description}", "ok");
                    return;
                }
                language.Value.words.Add(description, new Pattern());
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("提示", $"key[{description}]已添加,请在本地化配置中配置语言翻译！", "ok");
#endif
        }
        
        [BoxGroup("content/Info/信息"), LabelText("点击音效")]
        public AudioClip clickAudio;
        
        [BoxGroup("content/Info/成长"), LabelText("成长所需成长值")]
        public float totalExp;
        [BoxGroup("content/Info/成长"), LabelText("每次食物的成长值")]
        public float eatExp;
        [BoxGroup("content/Info/成长"), LabelText("每分钟增加的成长值")]
        public float autoExp;

        [VerticalGroup("content/Info"), BoxGroup("content/Info/习性"), LabelText("习性的key"), TextArea]
        public string habitat;
        
        [BoxGroup("content/Info/习性"), Button("添加习性(key)到本地化配置"), GUIColor("buttonColor")]
        private void OnAddHabitat()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(habitat))
            {
                EditorUtility.DisplayDialog("警告", "习性不能为空", "ok");
                return;
            }
            var config =
                AssetDatabase.LoadAssetAtPath<LocalizationConfig>("Assets/Prefabs/Config/LocalizationConfig.asset");
            foreach (var language in config.languageDic)
            {
                if (language.Value.words.ContainsKey(habitat))
                {
                    EditorUtility.DisplayDialog("警告", $"本地化配置中已经存在key: {habitat}", "ok");
                    return;
                }
                language.Value.words.Add(habitat, new Pattern());
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("提示", $"key[{habitat}]已添加,请在本地化配置中配置语言翻译！", "ok");
#endif
        }
        [BoxGroup("content/Info/习性"), HideLabel, PreviewField]
        public Sprite scenePreview;

        private IEnumerable rarityInfo = new ValueDropdownList<string>()
        {
            { "Common" , "Common" },
            { "Rare" , "Rare" },
            { "Endangered" , "Endangered" },
            { "Extinct" , "Extinct" },
            { "Unknown" , "Unknown" }
        };

        private Color32 buttonColor = Color.green;

        private void OnRealityValueChanged()
        {
#if UNITY_EDITOR
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            if (config.colorSettings.TryGetValue(reality, out var setting))
            {
                realityColor = setting;
            }
#endif
        }
    }
}