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
        SystemLanguage GetUserLanguage();
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
            if (!SteamManager.Initialized) return;
            SteamAPI.RunCallbacks();
        }

        public void ShutDown()
        {
            if (!SteamManager.Initialized) return;
            SteamAPI.Shutdown();
        }

        public void AddBirdUnlocked(int birdId)
        {
            if (!SteamManager.Initialized) return;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            string key = GetBirdStatsKey(config.GetBirdName(birdId, mapIndex));
            if (string.IsNullOrEmpty(key))
                return;
            SteamUserStats.SetStat(key, 1);
        }

        public void FirstPlayTime()
        {
            if (!SteamManager.Initialized) return;
            if (PlayerPrefs.HasKey("UserPlayed"))
                return;
            int time = (int)(Time.time - timeStart);
            SteamUserStats.SetStat("TotalPlayTime", time);
        }

        public SystemLanguage GetUserLanguage()
        {
            if (!SteamManager.Initialized) return SystemLanguage.English;
            string language = SteamApps.GetCurrentGameLanguage();
            return Convert(language);
        }

        private SystemLanguage Convert(string steamLanguage)
        {
            if (string.IsNullOrEmpty(steamLanguage))
                return Application.systemLanguage;

            switch (steamLanguage.ToLower())
            {
                // 主要语言
                case "english": return SystemLanguage.English;
                case "schinese":
                case "chinesesimplified": return SystemLanguage.ChineseSimplified;
                case "tchinese":
                case "chinesetraditional": return SystemLanguage.ChineseTraditional;
                case "japanese": return SystemLanguage.Japanese;
                case "korean":
                case "koreana": return SystemLanguage.Korean;
                case "russian": return SystemLanguage.Russian;
                case "german": return SystemLanguage.German;
                case "french": return SystemLanguage.French;
                case "spanish":
                case "spanishlatin": return SystemLanguage.Spanish;
                case "portuguese":
                case "portuguesebrazil": return SystemLanguage.Portuguese;
                case "italian": return SystemLanguage.Italian;
                case "arabic": return SystemLanguage.Arabic;
                case "dutch": return SystemLanguage.Dutch;
                case "polish": return SystemLanguage.Polish;
                case "turkish": return SystemLanguage.Turkish;
                case "ukrainian": return SystemLanguage.Ukrainian;

                // 北欧语言
                case "swedish": return SystemLanguage.Swedish;
                case "norwegian": return SystemLanguage.Norwegian;
                case "danish": return SystemLanguage.Danish;
                case "finnish": return SystemLanguage.Finnish;
                case "icelandic": return SystemLanguage.Icelandic;

                // 其他欧洲语言
                case "czech": return SystemLanguage.Czech;
                case "hungarian": return SystemLanguage.Hungarian;
                case "romanian": return SystemLanguage.Romanian;
                case "bulgarian": return SystemLanguage.Bulgarian;
                case "greek": return SystemLanguage.Greek;

                // 亚洲语言
                case "thai": return SystemLanguage.Thai;
                case "vietnamese": return SystemLanguage.Vietnamese;
                case "indonesian": return SystemLanguage.Indonesian;

                // 特殊情况处理
                case "brazilian": return SystemLanguage.Portuguese; // 巴西葡萄牙语
                case "latam": return SystemLanguage.Spanish; // 拉丁美洲西班牙语

                default:
                    Debug.LogWarning($"未知的Steam语言代码: {steamLanguage}, 使用系统默认语言");
                    return Application.systemLanguage;
            }
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