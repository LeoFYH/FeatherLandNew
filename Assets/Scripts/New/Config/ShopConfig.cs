using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BirdGame
{
    public class ShopConfig : ScriptableObject
    {
        [ShowInInspector, HideLabel, BoxGroup("场景商店配置"), OnValueChanged("OnSelectSceneChanged"), ValueDropdown("GetScenes", DropdownTitle = "选择地图")]
        private int sceneIndex;
        [ShowInInspector, BoxGroup("场景商店配置"), Title("鸟蛋配置"), HideLabel]
        private SceneEgg currentEgg = null;
        [ShowInInspector, BoxGroup("场景商店配置"), Title("饰品配置"), HideLabel]
        private SceneDecoration currentDecoration = null;

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInit()
        {
            if (sceneEggs == null)
                sceneEggs = new List<SceneEgg>();
            if (sceneDecorations == null)
                sceneDecorations = new List<SceneDecoration>();
        }

        private ValueDropdownList<int> GetScenes()
        {
            var list = new ValueDropdownList<int>();
            var config = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Prefabs/Config/MapConfig.asset");
            for (int i = 0; i < config.maps.Length; i++)
            {
                list.Add(new ValueDropdownItem<int>(config.maps[i].mapName, i));
            }

            return list;
        }

        private void OnSelectSceneChanged()
        {
            while (sceneIndex >= sceneEggs.Count)
            {
                sceneEggs.Add(new SceneEgg()
                {
                    eggs = new EggItem[]{}
                });
            }
            currentEgg = sceneEggs[sceneIndex];
            while (sceneIndex >= sceneDecorations.Count)
            {
                sceneDecorations.Add(new SceneDecoration()
                {
                    decorations = new DecorationItem[]{}
                });
            }

            currentDecoration = sceneDecorations[sceneIndex];
        }
#endif

        public int MapIndex()
        {
            return sceneIndex;
        }


        [LabelText("能否拖拽"), BoxGroup("信息")]
        public bool canDrag = false;
        [LabelText("初始金币"), BoxGroup("信息")]
        public int startCoins = 200;
        [LabelText("金币上限"), BoxGroup("信息")]
        public int coinsLimit = 10000;
            
        [HideInInspector]
        public List<SceneEgg> sceneEggs = new List<SceneEgg>();
        
        [TableList(ShowIndexLabels = true)]
        public List<SceneDecoration> sceneDecorations = new List<SceneDecoration>();
        [TableList(ShowIndexLabels = true), BoxGroup("工具配置")]
        public ToolItem[] tools;
    }
    

    [Serializable]
    public class SceneEgg
    {
        [TableList(ShowIndexLabels = true)]
        public EggItem[] eggs;
    }
    
    [Serializable]
    public class SceneDecoration
    {
        [TableList(ShowIndexLabels = true)]
        public DecorationItem[] decorations;
    }

    [Serializable]
    public class EggItem
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("content", Width = 50), HideLabel]
        public Sprite eggSp;
        [LabelText("价格"), LabelWidth(50), HorizontalGroup("content"), VerticalGroup("content/birds")]
        public int price;
        [LabelText("开出鸟的数量"), VerticalGroup("content/birds"), InfoBox("开出鸟的数量不能小于或等于0！", InfoMessageType.Error, VisibleIf = "@birdCount<=0")]
        public int birdCount = 3;
        [LabelText("描述"), VerticalGroup("content/birds"), TextArea]
        public string description;
        [TableList(ShowIndexLabels = true), VerticalGroup("content/birds"), InfoBox("鸟蛋包含的鸟的列表不能为空！", InfoMessageType.Warning, VisibleIf = "@birds==null||birds.Length==0")]
        public EggBirdItem[] birds;

        public float GetTotalProbability()
        {
            if (birds == null || birds.Length == 0)
                return 0;
            
            float total = 0;
            
            foreach (var item in birds)
            {
                total += item.probability;
            }

            return total;
        }
    }

    [Serializable]
    public class EggBirdItem
    {
        [ShowInInspector, ReadOnly, PreviewField(ObjectFieldAlignment.Left), HorizontalGroup("info", Width = 30), HideLabel]
        private Texture2D preview;
        [ValueDropdown("GetBirdList"), HorizontalGroup("info", PaddingLeft = 30), VerticalGroup("info/content"), HideLabel, OnValueChanged("RefreshBirdTexture"), OnInspectorGUI("OnDrawTexture")]
        public int birdType;
        [VerticalGroup("info/content"), LabelText("概率")]
        public float probability = 0.5f;


        private void OnDrawTexture()
        {
            if(preview != null)
                return;
            RefreshBirdTexture();
        }

#if UNITY_EDITOR
        private void RefreshBirdTexture()
        {
#if UNITY_EDITOR
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            int mapIndex = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset")
                .MapIndex();
            var bird = config.GetBird(birdType, mapIndex);
            if (bird != null)
                preview = bird.preview.texture;
#endif
        }

        private ValueDropdownList<int> GetBirdList()
        {
#if UNITY_EDITOR
            
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            int mapIndex = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset")
                .MapIndex();
            var list = new ValueDropdownList<int>();
            for (int i = 0; i < config.sceneBirds[mapIndex].birdClasses.Length; i++)
            {
                int index = 0;
                foreach (var bird in config.sceneBirds[mapIndex].birdClasses[i].birds)
                {
                    if (config.sceneBirds[mapIndex].birdClasses[i] == null || bird == null)
                    {
                        Debug.Log($"{config.sceneBirds[mapIndex].birdClasses[i].birdName} {index}为空！");
                        continue;
                    }
                    list.Add(config.sceneBirds[mapIndex].birdClasses[i].birdName + index, bird.id);
                    index++;
                }
            }
            
            return list;
#else
            return null;
#endif
        }
#else
        private void RefreshBirdTexture()
        {
            // 在构建版本中，这个方法不会被调用
        }

        private ValueDropdownList<int> GetBirdList()
        {
            // 在构建版本中，这个方法不会被调用
            return new ValueDropdownList<int>();
        }
#endif
    }

    public enum DragType
    {
        DefaultGround,
        MultiAreas
    }

    [Serializable]
    public class DecorationItem
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("Icon", Width = 50), HideLabel]
        public Sprite icon;
        [LabelText("名称"), HorizontalGroup("Icon"), VerticalGroup("Icon/Info")]
        public string name;
        [VerticalGroup("Icon/Info")]
        public AssetReferenceGameObject prefab;
        [LabelText("描述"), VerticalGroup("Icon/Info")]
        public string description;
        [LabelText("价格"), VerticalGroup("Icon/Info")]
        public int price;
        [LabelText("大小"), VerticalGroup("Icon/Info"), Range(0.01f, 2f)]
        public float scale = 1f;
        [LabelText("Icon大小"), VerticalGroup("Icon/Info"), Range(0.01f, 1f)]
        public float iconScale = 0.5f;
        [LabelText("最大购买数量"), VerticalGroup("Icon/Info"), InfoBox("设置为0表示无限制", InfoMessageType.Info), OnValueChanged("OnCountValueChanged")]
        public int maxQuantity = 0;
        [LabelText("场景Sprite"), VerticalGroup("Icon/Info")]
        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite sceneSprite;
        [LabelText("固定位置"), VerticalGroup("Icon/Info")]
        public Vector3[] fixedPositions;
        [VerticalGroup("Icon/Info")]
        public bool isGround = false;
        [VerticalGroup("Icon/Info"), ShowIf("@isGround==true")]
        public DragType dragType = DragType.DefaultGround;
        [VerticalGroup("Icon/Info"), ShowIf("@dragType==DragType.MultiAreas"), ValueDropdown("GetAreaList")]
        public int[] areas = new[] { 3 };
        [LabelText("是否显示"), VerticalGroup("Icon/Info"), InfoBox("取消勾选后，该装饰物不会出现在游戏商店中")]
        public bool isVisible = true;
        [LabelText("示意图"), VerticalGroup("Icon/Info")]
        public Sprite preview;

        private void OnCountValueChanged()
        {

            var newVecs = new Vector3[maxQuantity];
            for (int i = 0; i < newVecs.Length; i++)
            {
                if (i >= fixedPositions.Length)
                    break;
                newVecs[i] = fixedPositions[i];
            }

            fixedPositions = newVecs;

        }
        
        private ValueDropdownList<int> GetAreaList()
        {
            string[] areaNames = NavMesh.GetAreaNames(); // 获取所有区域名称
        
            var list = new ValueDropdownList<int>();
            foreach (string areaName in areaNames)
            {
                // 通过名称获取对应的索引
                int areaIndex = NavMesh.GetAreaFromName(areaName); 
                Debug.Log($"区域名称: {areaName}, 索引: {areaIndex}");
                list.Add(areaName, areaIndex);
            }
            
            return list;
        }
    }

    public enum DecorationType
    {
        [LabelText("可拖拽")]
        Draggable,
        [LabelText("固定类")]
        Fixed
    }

    [Serializable]
    public class ToolItem
    {
        [LabelText("名称"), VerticalGroup("Tool")]
        public string name;
        [LabelText("是否显示"), VerticalGroup("Tool"), InfoBox("取消勾选后，该工具不会出现在游戏商店中")]
        public bool isVisible = true;
        [TableList(ShowIndexLabels = true), VerticalGroup("Tool")]
        public ToolSelection[] selections;
    }

    [Serializable]
    public class ToolSelection
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("Content", Width = 50), HideLabel]
        public Sprite icon;
        [LabelText("工具类型"), HorizontalGroup("Content"), VerticalGroup("Content/Info")]
        public ToolType type;
        [LabelText("名称"), HorizontalGroup("Content"), VerticalGroup("Content/Info")]
        public string selectionName;
        [LabelText("描述"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), TextArea]
        public string description;
        [LabelText("描述的key"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), TextArea]
        public string descriptionKey;
        [LabelText("添加描述key到本地化配置"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), Button("添加Key"), GUIColor("buttonColor")]
        private void OnAddDescriptionKey()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(descriptionKey))
            {
                UnityEditor.EditorUtility.DisplayDialog("警告", "描述key不能为空", "ok");
                return;
            }

            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationConfig>("Assets/Prefabs/Config/LocalizationConfig.asset");
            if (config == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "未找到本地化配置文件", "ok");
                return;
            }
            
            foreach (var language in config.languageDic)
            {
                if (language.Value.words.ContainsKey(descriptionKey))
                {
                    UnityEditor.EditorUtility.DisplayDialog("警告", $"本地化配置中已经存在key: {descriptionKey}", "ok");
                    return;
                }
                language.Value.words.Add(descriptionKey, new Pattern());
            }
            UnityEditor.EditorUtility.SetDirty(config);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.DisplayDialog("提示", $"key[{descriptionKey}]已添加,请在本地化配置中配置语言翻译！", "ok");
#endif
        }
        [LabelText("价格"), HorizontalGroup("Content"), VerticalGroup("Content/Info")]
        public int price;
        [LabelText("食物大小"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), Range(0.01f, 5f), ShowIf("@type==ToolType.Food")]
        public float foodScale = 1f;
        [LabelText("成长值加成"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), ShowIf("@type==ToolType.Food")]
        public float addValue = 0.1f;
        [LabelText("增加鸟的容量大小"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), ShowIf("@type==ToolType.BirdMaxCount")]
        public int addCount;
        [LabelText("UI颜色列表"), HorizontalGroup("Content"), VerticalGroup("Content/Info"), ShowIf("@type==ToolType.Radio||type==ToolType.Note||type==ToolType.Tomato||type==ToolType.Illustrated")]
        public UIColorItem uiColorItem;
        
        private Color32 buttonColor = Color.green;
    }

    [Serializable]
    public class UIColorItem
    {
        [LabelText("UI颜色"), VerticalGroup("UI Color")]
        public Color32 uiColor;
        public Sprite[] uiSprites;
    }

    public enum ToolType
    {
        Food = 0,
        BirdMaxCount = 1,
        Radio = 2,
        Note = 3,
        Tomato = 4,
        Illustrated = 5,
        Cursor = 6
    }
}