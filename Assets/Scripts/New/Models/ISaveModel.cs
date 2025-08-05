using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface ISaveModel : IModel
    {
        AccountData AccountData { get; set; }
        SettingData SettingData { get; set; }
        MusicSettingData MusicSettingData { get; set; }
        BirdInfoData BirdInfoData { get; set; }
        NoteData NoteData { get; set; }
        ScheduleData ScheduleData { get; set; }
        DecorationData DecorationData { get; set; }
        IllustratedData IllustratedData { get; set; }
    }

    public class SaveModel : AbstractModel, ISaveModel
    {
        protected override void OnInit()
        {
            
        }

        public AccountData AccountData { get; set; }
        public SettingData SettingData { get; set; }
        public MusicSettingData MusicSettingData { get; set; }
        public BirdInfoData BirdInfoData { get; set; }
        public NoteData NoteData { get; set; }
        public ScheduleData ScheduleData { get; set; }
        public DecorationData DecorationData { get; set; }
        public IllustratedData IllustratedData { get; set; }
    }
    
    [Serializable]
    public class SavableData
    {
    }

    /// <summary>
    /// 账户存档数据
    /// </summary>
    [Serializable]
    public class AccountData : SavableData
    {
        public int coins = 100;
    }

    /// <summary>
    /// 设置存档数据
    /// </summary>
    [Serializable]
    public class SettingData : SavableData
    {
        public int screenMode = 2; // 0: 窗口模式, 1: 壁纸模式, 2: 全屏模式 (默认全屏)
    }

    /// <summary>
    /// 音频设置存档数据
    /// </summary>
    [Serializable]
    public class MusicSettingData : SavableData
    {
        public float bgmVolume = 0.5f;
        public List<float> environmentVolumes;
    }

    /// <summary>
    /// 鸟的存档数据
    /// </summary>
    [Serializable]
    public class BirdInfoData : SavableData
    {
        
    }

    /// <summary>
    /// 装饰品存档数据
    /// </summary>
    [Serializable]
    public class DecorationData : SavableData
    {
        public Dictionary<int, int> purchasedQuantities = new Dictionary<int, int>();
    }

    /// <summary>
    /// 日记存档数据
    /// </summary>
    [Serializable]
    public class NoteData : SavableData
    {
        public List<BookData> bookList = new List<BookData>();
    }

    /// <summary>
    /// 日记数据
    /// </summary>
    [Serializable]
    public class BookData
    {
        public string noteText;
    }

    /// <summary>
    /// 日程存档数据
    /// </summary>
    [Serializable]
    public class ScheduleData : SavableData
    {
        public List<ScheduleItemData> scheduleList = new List<ScheduleItemData>();
    }

    /// <summary>
    /// 日程数据
    /// </summary>
    [Serializable]
    public class ScheduleItemData
    {
        public string scheduleText;
        public bool isCompleted;
    }

    /// <summary>
    /// 图鉴存档
    /// </summary>
    [Serializable]
    public class IllustratedData : SavableData
    {
        public List<int> unlockedBirds = new List<int>();
    }
}