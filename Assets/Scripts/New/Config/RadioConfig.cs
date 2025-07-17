using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class RadioConfig : ScriptableObject
    {
        [Title("音乐播放器库"),TableList(ShowIndexLabels = true)]
        public AudioItem[] musics;
        [TableList(ShowIndexLabels = true)]
        public AudioItem[] environments;
        
        [Title("音效")]
        public AudioClip click;
        public AudioClip dropFood;
        public AudioClip stroke;
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