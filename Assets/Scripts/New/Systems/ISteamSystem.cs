using QFramework;
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
using Steamworks;
#endif
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
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            // SteamManager 在 Awake 中统一负责 SteamAPI 的初始化、回调和关闭。
            // 这里再次 Init/RunCallbacks/Shutdown 会造成同一会话重复管理 SteamAPI。
            if (!SteamManager.Initialized)
                Debug.LogWarning("SteamManager is not initialized. Steam features will retry when available.");
#endif

            timeStart = Time.time;
        }

        public void RunCallbacks()
        {
            // SteamManager.Update() 已统一执行 SteamAPI.RunCallbacks()。
        }

        public void ShutDown()
        {
            // SteamManager.OnDestroy() 已统一执行 SteamAPI.Shutdown()。
        }

        public void AddBirdUnlocked(int birdId)
        {
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            if (!SteamManager.Initialized) return;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            string key = GetBirdStatsKey(config.GetBirdName(birdId, mapIndex));
            if (string.IsNullOrEmpty(key))
                return;
            SteamUserStats.SetStat(key, 1);
#endif
        }

        public void FirstPlayTime()
        {
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            if (!SteamManager.Initialized) return;
            if (PlayerPrefs.HasKey("UserPlayed"))
                return;
            int time = (int)(Time.time - timeStart);
            SteamUserStats.SetStat("TotalPlayTime", time);
#endif
        }

        public SystemLanguage GetUserLanguage()
        {
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            if (!SteamManager.Initialized) return SystemLanguage.English;
            string language = SteamApps.GetCurrentGameLanguage();
            return Convert(language);
#else
            return Application.systemLanguage;
#endif
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
