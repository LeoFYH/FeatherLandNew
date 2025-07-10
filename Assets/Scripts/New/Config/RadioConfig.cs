using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class RadioConfig : ScriptableObject
    {
        [TableList(ShowIndexLabels = true)]
        public AudioItem[] audios;
    }

    [Serializable]
    public class AudioItem
    {
        [LabelText("音乐名称")]
        public string songName;
        [LabelText("音乐文件")]
        public AudioClip songFile;
    }
}