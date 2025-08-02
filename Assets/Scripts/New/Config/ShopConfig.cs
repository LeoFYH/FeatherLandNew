using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BirdGame
{
    public class ShopConfig : ScriptableObject
    {
        [Title("鸟蛋购买配置"), Space(10)]
        [TableList(ShowIndexLabels = true)]
        public EggItem[] eggs;

        [Title("饰品配置")]
        [TableList(ShowIndexLabels = true)]
        public DecorationItem[] decorations;

        [Title("工具配置")] 
        [TableList(ShowIndexLabels = true)]
        public ToolItem[] tools;
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
        [Range(0, 1f), VerticalGroup("info/content"), LabelText("概率"), InfoBox("概率不能为0！", InfoMessageType.Error, VisibleIf = "@probability==0f")]
        public float probability = 0.5f;


        private void OnDrawTexture()
        {
            if(preview == null)
                return;
            RefreshBirdTexture();
        }

        private void RefreshBirdTexture()
        {
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            preview = config.birds[birdType].preview.texture;
        }

        private ValueDropdownList<int> GetBirdList()
        {
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");

            var list = new ValueDropdownList<int>();
            for (int i = 0; i < config.birds.Length; i++)
            {
                list.Add(config.birds[i].birdName, i);
            }

            return list;
        }
    }

    [Serializable]
    public class DecorationItem
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("Icon", Width = 50), HideLabel]
        public Sprite icon;
        [LabelText("名称"), HorizontalGroup("Icon"), VerticalGroup("Icon/Info")]
        public string name;
        [LabelText("描述"), VerticalGroup("Icon/Info")]
        public string description;
        [LabelText("价格"), VerticalGroup("Icon/Info")]
        public int price;
        [LabelText("大小"), VerticalGroup("Icon/Info"), Range(0.1f, 2f)]
        public float scale = 1f;
        [LabelText("最大购买数量"), VerticalGroup("Icon/Info"), InfoBox("设置为0表示无限制", InfoMessageType.Info)]
        public int maxQuantity = 0;
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("Scene", Width = 50), HideLabel]
        [LabelText("场景Sprite"), HorizontalGroup("Scene"), VerticalGroup("Scene/Info")]
        public Sprite sceneSprite;
    }

    [Serializable]
    public class ToolItem
    {
        [LabelText("名称"), VerticalGroup("Tool")]
        public string name;
        [TableList(ShowIndexLabels = true), VerticalGroup("Tool")]
        public ToolSelection[] selections;
    }

    [Serializable]
    public class ToolSelection
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("Content", Width = 50), HideLabel]
        public Sprite icon;
        [LabelText("名称"), HorizontalGroup("Content"), VerticalGroup("Content/Info")]
        public string selectionName;
        [LabelText("描述"), VerticalGroup("Content/Info")]
        public string description;
        [LabelText("价格"), VerticalGroup("Content/Info")]
        public int price;
    }
}