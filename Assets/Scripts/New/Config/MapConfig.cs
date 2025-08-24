using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
    public class MapConfig : ScriptableObject
    {
        [TableList(ShowIndexLabels = true)]
        public MapItemConfig[] maps;
    }

    [Serializable]
    public class MapItemConfig
    {
        [LabelText("栖息地名称")]
        public string mapName;
        [PreviewField, HideLabel]
        public Sprite mapPreview;
    }
}