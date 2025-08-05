using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class BirdConfig : ScriptableObject
    {
        [Title("鸟的配置"), Space(10)] 
        [LabelText("显示鸟走路的路线")]
        public bool isDrawPathLine;
        [TableList(ShowIndexLabels = true)]
        public BirdItem[] birds;
    }

    [Serializable]
    public class BirdItem
    {
        [PreviewField(ObjectFieldAlignment.Left, Height = 50), HorizontalGroup("content", Width = 50), HideLabel]
        public Sprite preview;
        [HorizontalGroup("content"), VerticalGroup("content/Info"), LabelText("名称")]
        public string birdName;
        [VerticalGroup("content/Info"), LabelText("鸟的预制体")]
        public GameObject prefab;
        [VerticalGroup("content/Info"), BoxGroup("content/Info/信息"), LabelText("稀有度"), Range(1, 5)]
        public int reality = 1;
        [BoxGroup("content/Info/信息"), LabelText("每分钟收入")]
        public int eraning;
        [BoxGroup("content/Info/信息"), LabelText("价格(小)")]
        public int priceForSmall;
        [BoxGroup("content/Info/信息"), LabelText("价格(大)")]
        public int priceForBig;
        [BoxGroup("content/Info/信息"), LabelText("成长所需食物数量")]
        public int eatForBig;
        [BoxGroup("content/Info/信息"), LabelText("皮肤"), TableList(ShowIndexLabels = true)]
        public BirdSkinItem[] birdSkinItems;
        [BoxGroup("content/Info/信息"), LabelText("描述"), TextArea]
        public string description;
        [VerticalGroup("content/Info"), BoxGroup("content/Info/习性"), LabelText("习性"), TextArea]
        public string habitat;
        [BoxGroup("content/Info/习性"), HideLabel, PreviewField]
        public Sprite scenePreview;
    }

    [Serializable]
    public class BirdSkinItem
    {
        [PreviewField, HideLabel]
        public Sprite skinView;
    }
}