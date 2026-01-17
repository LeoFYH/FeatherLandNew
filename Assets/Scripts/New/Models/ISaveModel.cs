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
        public float coins = 600;
        public List<SceneDecorationInfo> sceneDecorationInfos = new List<SceneDecorationInfo>();
        public List<ToolInfo> tools = new List<ToolInfo>();
        public int addedMaxBirdValue = 0;
    }

    [Serializable]
    public class SceneDecorationInfo
    {
        public List<DecorationInfo> decorations = new List<DecorationInfo>();
    }

    [Serializable]
    public class DecorationInfo
    {
        public int count;
        public List<Vector3> position = new List<Vector3>();
        /// <summary>
        /// 记录已使用的 fixedPositions 索引，用于在删除后重新购买时使用正确的索引
        /// </summary>
        public List<int> usedFixedPositionIndices = new List<int>();
    }

    [Serializable]
    public class ToolInfo
    {
        public int equipedId = 0;
        public List<int> unlockedList = new List<int>();
    }

    /// <summary>
    /// 设置存档数据
    /// </summary>
    [Serializable]
    public class SettingData : SavableData
    {
        public int screenMode = 2; // 0: 窗口模式, 1: 壁纸模式, 2: 全屏模式 (默认全屏)
        public SystemLanguage gameLanguage;
        public bool isShowedTutorial;

        public SettingData()
        {
            screenMode = 2;
            Debug.Log($"当前Windows系统语言: {Application.systemLanguage}");
            gameLanguage = SystemLanguage.Unknown;
        }
    }

    /// <summary>
    /// 音频设置存档数据
    /// </summary>
    [Serializable]
    public class MusicSettingData : SavableData
    {
        public float bgmVolume = 0.5f;
        public List<float> environmentVolumes = new List<float>();
        public List<bool> likes = new List<bool>();
    }

    /// <summary>
    /// 鸟的存档数据
    /// </summary>
    [Serializable]
    public class BirdInfoData : SavableData
    {
        public int currentMap = 0;
        public List<MapBirdList> mapBirds = new List<MapBirdList>();
    }

    [Serializable]
    public class MapBirdList
    {
        public List<SerializableBirdData> birdList = new List<SerializableBirdData>();
        public List<int> eggList = new List<int>();
    }

    [Serializable]
    public class IllustratedData : SavableData
    {
        public List<int> birds = new List<int>();
    }

    /// <summary>
    /// 可序列化的鸟数据
    /// </summary>
    [Serializable]
    public class SerializableBirdData
    {
        public int birdType;
        public string customName;
        public bool isSmall;
        public float currentExp;
        public int currentFavorability;
        public int totalFavorability;
        public float petTime;
        public Vector3 position;
        public int walkArea;
        
        // 个体化数值（必须保存以保持一致性）
        public float individualEarningSmall;
        public float individualEarningBig;
        public float individualPriceSmall;
        public float individualPriceBig;
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
        public DateTime StartTime = DateTime.Now;
        public bool isCompleted;
    }
}