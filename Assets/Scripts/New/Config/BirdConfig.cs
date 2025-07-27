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
    }
}