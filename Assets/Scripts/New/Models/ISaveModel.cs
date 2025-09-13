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
        public int coins = 600;
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
            gameLanguage = GetSupportedLanguage(Application.systemLanguage);
            Debug.Log($"最终设置的游戏语言: {gameLanguage}");
        }
        
        /// <summary>
        /// 获取支持的语言，如果不支持则回退到英文
        /// </summary>
        /// <param name="systemLanguage">系统语言</param>
        /// <returns>支持的语言</returns>
        private SystemLanguage GetSupportedLanguage(SystemLanguage systemLanguage)
        {
            // 支持的语言列表
            SystemLanguage[] supportedLanguages = {
                SystemLanguage.English,
                SystemLanguage.Chinese,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional,
                SystemLanguage.Japanese,
                SystemLanguage.Korean,
                SystemLanguage.French,
                SystemLanguage.German,
                SystemLanguage.Spanish
            };
            
            // 检查系统语言是否在支持列表中
            foreach (var supportedLang in supportedLanguages)
            {
                if (systemLanguage == supportedLang)
                {
                    return systemLanguage;
                }
            }
            
            // 如果不支持，回退到英文
            Debug.LogWarning($"系统语言 {systemLanguage} 不受支持，回退到英文");
            return SystemLanguage.English;
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
}