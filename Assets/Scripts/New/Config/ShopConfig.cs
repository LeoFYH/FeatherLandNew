using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class ShopConfig : ScriptableObject
    {
        [Title("鸟蛋购买配置")]
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
        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite eggSp;
        [LabelText("价格"), LabelWidth(50)]
        public int price;
    }

    [Serializable]
    public class DecorationItem
    {
        [PreviewField(50, ObjectFieldAlignment.Left), VerticalGroup("Icon")]
        public Sprite icon;
        [LabelText("名称"), VerticalGroup("信息")]
        public string name;
        [LabelText("描述"), VerticalGroup("信息")]
        public string description;
        [LabelText("价格"), VerticalGroup("信息")]
        public int price;
    }

    [Serializable]
    public class ToolItem
    {
        [LabelText("名称")]
        public string name;
        [TableList(ShowIndexLabels = true)]
        public ToolSelection[] selections;
    }

    [Serializable]
    public class ToolSelection
    {
        [PreviewField(50, ObjectFieldAlignment.Left), VerticalGroup("Icon")]
        public Sprite icon;
        [LabelText("名称"), VerticalGroup("信息")]
        public string selectionName;
        [LabelText("描述"), VerticalGroup("信息")]
        public string description;
        [LabelText("价格"), VerticalGroup("信息")]
        public int price;
    }
}