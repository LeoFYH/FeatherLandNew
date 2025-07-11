using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class BirdConfig : ScriptableObject
    {
        [TableList(ShowIndexLabels = true)]
        public BirdItem[] birds;
    }

    [Serializable]
    public class BirdItem
    {
        [PreviewField(ObjectFieldAlignment.Left)]
        public Sprite preview;
        public string birdName;
        public GameObject prefab;
    }
}