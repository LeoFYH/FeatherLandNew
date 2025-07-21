using System;
using Sirenix.OdinInspector;
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
        [LabelText("价格"), LabelWidth(50), HorizontalGroup("content")]
        public int price;
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