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
    }
    
    [Serializable]
    public class SavableData
    {
    }

    [Serializable]
    public class AccountData : SavableData
    {
        public int coins = 100;
    }

    [Serializable]
    public class SettingData : SavableData
    {
        
    }

    [Serializable]
    public class MusicSettingData : SavableData
    {
        public float bgmVolume = 0.5f;
        public List<float> environmentVolumes;
    }

    [Serializable]
    public class BirdInfoData : SavableData
    {
        
    }
}