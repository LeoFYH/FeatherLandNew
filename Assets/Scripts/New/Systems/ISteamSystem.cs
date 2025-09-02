using QFramework;
using Steamworks;
using UnityEngine;

namespace BirdGame
{
    public interface ISteamSystem : ISystem
    {
        void RunCallbacks();
        void ShutDown();
        void AddBirdUnlocked(int birdId);
        void FirstPlayTime();
    }

    public class SteamSystem : AbstractSystem, ISteamSystem
    {
        private float timeStart;
        
        protected override void OnInit()
        {
            try
            {
                // 尝试初始化 SteamAPI
                if (SteamAPI.Init())
                {
                    Debug.Log("SteamAPI initialized successfully!");
                    // 获取用户名称（测试用）
                    string playerName = SteamFriends.GetPersonaName();
                    Debug.Log("Player's name: " + playerName);
                }
                else
                {
                    Debug.LogError("SteamAPI.Init() failed! Make sure:");
                    Debug.LogError("1. Steam client is running.");
                    Debug.LogError("2. The game was launched through Steam.");
                    Debug.LogError("3. You have a valid steam_appid.txt file.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("SteamAPI failed to initialize: " + e.Message);
            }

            timeStart = Time.time;
        }

        public void RunCallbacks()
        {
            SteamAPI.RunCallbacks();
        }

        public void ShutDown()
        {
            SteamAPI.Shutdown();
        }

        public void AddBirdUnlocked(int birdId)
        {
            if (!SteamManager.Initialized) return;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            string key = GetBirdStatsKey(config.GetBirdName(birdId, mapIndex));
            if(string.IsNullOrEmpty(key))
                return;
            SteamUserStats.SetStat(key, 1);
        }

        public void FirstPlayTime()
        {
            if(PlayerPrefs.HasKey("UserPlayed"))
                return;
            int time = (int) (Time.time - timeStart);
            SteamUserStats.SetStat("TotalPlayTime", time);
        }

        private string GetBirdStatsKey(string birdName)
        {
            switch (birdName)
            {
                case "Cockatiel": return "stat_11";
                case "Budgerigar": return "stat_12";
                case "Lorikeet": return "stat_13";
                case "Cockatoo": return "stat_14";
                case "Grey Parrot": return "stat_15";
                case "Kakapo": return "stat_16";
                case "Northern Cardinal": return "stat_17";
                case "Kiwi": return "stat_18";
                case "Dodo": return "stat_19";
            }

            return "";
        }
    }
}