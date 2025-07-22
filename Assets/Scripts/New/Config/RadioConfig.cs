using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class RadioConfig : ScriptableObject
    {
        [Title("音乐播放器库"), Space(10),TableList(ShowIndexLabels = true)]
        public RecordItem[] recordItems;
        [TableList(ShowIndexLabels = true)]
        public AudioItem[] environments;
        
        [Title("音效"), Space(10)]
        public AudioClip click;
        public AudioClip dropFood;
        public AudioClip stroke;

        [Title("Clock提示音乐库"), Space(10), TableList(ShowIndexLabels = true)] 
        public AudioItem[] alertClips;
    }

    [Serializable]
    public class RecordItem
    {
        [PreviewField(50, ObjectFieldAlignment.Left), HorizontalGroup("content", Width = 70), VerticalGroup("content/preview"), HideLabel]
        public Sprite icon;
        [PreviewField(70, ObjectFieldAlignment.Left), VerticalGroup("content/preview"), HideLabel]
        public Sprite recordImage;
        [TableList(ShowIndexLabels = true, ScrollViewHeight = 100), HorizontalGroup("content")]
        public AudioItem[] musics;
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