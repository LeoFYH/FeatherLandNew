using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace BirdGame
{
    public class RadioConfig : ScriptableObject
    {
        [Title("音乐播放器库"), Space(10),TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public AudioItem[] musicItems;
        [TableList(ShowIndexLabels = true)]
        public AudioItem[] environments;
        
        [Title("音效"), Space(10)] 
        public AudioItem click;
        public AudioItem dropFood;
        public AudioItem stroke;
        public AudioItem growUp;

        [Title("Clock提示音乐库"), Space(10), TableList(ShowIndexLabels = true, AlwaysExpanded = true)] 
        public AudioItem[] alertClips;
    }

    [Serializable]
    public class AudioItem
    {
        [LabelText("音乐名称")]
        public string songName;
        [LabelText("音乐文件")]
        public AudioClip songFile;
        public string key;
        public AudioMixerGroup group;
    }
}