using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using QFramework;
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
using Steamworks;
#endif
using UnityEngine;

namespace BirdGame
{
    public interface IAchievementSystem : ISystem
    {
        void CheckStartupAchievements();
        void OnEggHatched();
        void OnBirdSpeciesDiscovered(int birdId);
        void OnManualFeed();
        void OnBirdGrewToAdult();
        void OnMapUnlocked(int mapCount);
        void OnDecorationBought();
        void CheckBirdSlotAchievements();
        void OnTodoUsed();
        void OnDiaryCreated();
        void OnMusicPlayed();
        void OnPomodoroCompleted();
    }

    public class AchievementSystem : AbstractSystem, IAchievementSystem
    {
        // ==================== 34个成就ID ====================
        // 收藏 (10)
        const string INTO_THE_WILD = "ACH_COL_INTO_THE_WILD";
        const string MOUNTAIN_WANDERER = "ACH_COL_MOUNTAIN_WANDERER";
        const string DESERT_DRIFTER = "ACH_COL_DESERT_DRIFTER";
        const string PADDY_FIELD_GUARDIAN = "ACH_COL_PADDY_FIELD_GUARDIAN";
        const string WAVE_CHASER = "ACH_COL_WAVE_CHASER";
        const string TUNDRA_WALKER = "ACH_COL_TUNDRA_WALKER";
        const string WETLAND_KEEPER = "ACH_COL_WETLAND_KEEPER";
        const string FEATHER_ENCYCLOPEDIA = "ACH_COL_FEATHER_ENCYCLOPEDIA";
        const string WINGS_OF_THE_WORLD = "ACH_COL_WINGS_OF_THE_WORLD";
        const string BEFORE_EXTINCTION = "ACH_COL_BEFORE_EXTINCTION";
        // 探索 (7)
        const string FIRST_HATCH = "ACH_EXP_FIRST_HATCH";
        const string EGG_ADDICT = "ACH_EXP_EGG_ADDICT";
        const string EGG_SAGE = "ACH_EXP_EGG_SAGE";
        const string RARE_HUNTER = "ACH_EXP_RARE_HUNTER";
        const string ENDANGERED_GUARDIAN = "ACH_EXP_ENDANGERED_GUARDIAN";
        const string DODOS_RETURN = "ACH_EXP_DODOS_RETURN";
        const string PHOENIX_RISING = "ACH_EXP_PHOENIX_RISING";
        // 养育 (5)
        const string BIRD_PARENT = "ACH_NUR_BIRD_PARENT";
        const string TIRELESS_FEEDER = "ACH_NUR_TIRELESS_FEEDER";
        const string FULL_PLUMAGE = "ACH_NUR_FULL_PLUMAGE";
        const string FULL_NEST = "ACH_NUR_FULL_NEST";
        const string ALL_NESTS_FULL = "ACH_NUR_ALL_NESTS_FULL";
        // 财富 (5)
        const string FIRST_GOLD = "ACH_WEA_FIRST_GOLD";
        const string COMFORTABLE_NEST_EGG = "ACH_WEA_COMFORTABLE_NEST_EGG";
        const string FEATHER_TYCOON = "ACH_WEA_FEATHER_TYCOON";
        const string DECORATOR = "ACH_WEA_DECORATOR";
        const string MASTER_OF_AESTHETICS = "ACH_WEA_MASTER_OF_AESTHETICS";
        // 隐藏 (3)
        const string DAWN_CHORUS = "ACH_HID_DAWN_CHORUS";
        const string NIGHT_OWL = "ACH_HID_NIGHT_OWL";
        const string MIGRATORY_SOUL = "ACH_HID_MIGRATORY_SOUL";
        // 功能 (4)
        const string TASK_STARTER = "ACH_FEA_TASK_STARTER";
        const string FIRST_ENTRY = "ACH_FEA_FIRST_ENTRY";
        const string FOREST_MELODY = "ACH_FEA_FOREST_MELODY";
        const string IN_THE_ZONE = "ACH_FEA_IN_THE_ZONE";

        private AchievementData data;
        private float previousCoins;
        private int totalBirdSpeciesCount; // 全图鉴总数
        private HashSet<int> extinctBirdIds = new HashSet<int>(); // 所有灭绝鸟ID
        private Coroutine timeAchievementCheckCoroutine;
        private static readonly WaitForSecondsRealtime TimeAchievementCheckInterval = new WaitForSecondsRealtime(30f);
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
        private Coroutine steamAchievementSyncCoroutine;
        private bool steamStatsReady;
        private static readonly WaitForSecondsRealtime SteamInitializationCheckInterval = new WaitForSecondsRealtime(1f);
#endif

        protected override void OnInit()
        {
        }

        // ==================== 启动检查 ====================
        public void CheckStartupAchievements()
        {
            data = this.GetModel<ISaveModel>().AchievementData;
            if (data == null)
            {
                data = new AchievementData();
                this.GetModel<ISaveModel>().AchievementData = data;
            }
            if (data.unlockedAchievements == null)
                data.unlockedAchievements = new List<string>();

            // 缓存鸟配置信息
            CacheBirdConfigData();

            // 时间成就：启动时立即检查，并在运行期间持续检查
            CheckTimeAchievements();
            StartTimeAchievementCheck();

            // 连续登录按本地自然日计算。旧存档仍使用 yyyy-MM-dd，无需迁移或清零。
            UpdateConsecutiveLoginDays();
            if (data.consecutiveLoginDays >= 30)
                Unlock(MIGRATORY_SOUL);

            // 森林（首场景）
            var mapBirds = this.GetModel<ISaveModel>().BirdInfoData.mapBirds;
            if (mapBirds != null && mapBirds.Count >= 1)
                Unlock(INTO_THE_WILD);

            // 金币监听
            var accountModel = this.GetModel<IAccountModel>();
            previousCoins = accountModel.Coins.Value;
            accountModel.Coins.Register(newValue =>
            {
                float delta = newValue - previousCoins;
                if (delta > 0 && data != null)
                {
                    data.totalGoldEarned += delta;
                    CheckGoldAchievements();
                }
                previousCoins = newValue;
            });

            // 补发：用已有存档数据检查
            RetroactiveCheck();

            this.GetSystem<ISaveSystem>().SaveData();
            StartSteamAchievementSync();
        }

        // ==================== 触发方法 ====================

        public void OnEggHatched()
        {
            if (data == null) return;
            data.totalEggsHatched++;
            if (data.totalEggsHatched >= 1) Unlock(FIRST_HATCH);
            if (data.totalEggsHatched >= 50) Unlock(EGG_ADDICT);
            if (data.totalEggsHatched >= 200) Unlock(EGG_SAGE);
        }

        public void OnBirdSpeciesDiscovered(int birdId)
        {
            if (data == null) return;
            var config = this.GetModel<IConfigModel>().BirdConfig;

            // 查找鸟的稀有度和名称
            string rarity = "";
            string birdClassName = "";
            FindBirdInfo(config, birdId, out birdClassName, out rarity);

            // 稀有度成就
            if (rarity == "Rare") Unlock(RARE_HUNTER);
            if (rarity == "Endangered") Unlock(ENDANGERED_GUARDIAN);

            // 特定鸟成就
            if (birdClassName == "Dodo") Unlock(DODOS_RETURN);
            if (birdClassName == "Phoenix") Unlock(PHOENIX_RISING);

            // 图鉴数量
            var illustrated = this.GetModel<ISaveModel>().IllustratedData;
            if (illustrated.birds.Count >= 50) Unlock(FEATHER_ENCYCLOPEDIA);
            if (totalBirdSpeciesCount > 0 && illustrated.birds.Count >= totalBirdSpeciesCount)
                Unlock(WINGS_OF_THE_WORLD);

            // 灭绝鸟全收集
            CheckExtinctCollection();
        }

        public void OnManualFeed()
        {
            if (data == null) return;
            data.totalFeedCount++;
            if (data.totalFeedCount >= 1) Unlock(BIRD_PARENT);
            if (data.totalFeedCount >= 100) Unlock(TIRELESS_FEEDER);
        }

        public void OnBirdGrewToAdult()
        {
            Unlock(FULL_PLUMAGE);
        }

        public void OnMapUnlocked(int mapCount)
        {
            switch (mapCount)
            {
                case 2: Unlock(MOUNTAIN_WANDERER); break;
                case 3: Unlock(DESERT_DRIFTER); break;
                case 4: Unlock(PADDY_FIELD_GUARDIAN); break;
                case 5: Unlock(WAVE_CHASER); break;
                case 6: Unlock(TUNDRA_WALKER); break;
                case 7: Unlock(WETLAND_KEEPER); break;
            }
        }

        public void OnDecorationBought()
        {
            if (data == null) return;
            data.totalDecorationsBought++;
            if (data.totalDecorationsBought >= 10) Unlock(DECORATOR);
            CheckAllDecorationsOwned();
        }

        public void CheckBirdSlotAchievements()
        {
            var saveModel = this.GetModel<ISaveModel>();
            var mapBirds = saveModel.BirdInfoData.mapBirds;
            if (mapBirds == null) return;

            int maxCount = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount;
            bool anyFull = false;
            bool allFull = mapBirds.Count >= 7;

            for (int i = 0; i < mapBirds.Count; i++)
            {
                bool isFull = mapBirds[i].birdList != null && mapBirds[i].birdList.Count >= maxCount;
                if (isFull) anyFull = true;
                else allFull = false;
            }

            if (anyFull) Unlock(FULL_NEST);
            if (allFull && mapBirds.Count >= 7) Unlock(ALL_NESTS_FULL);
        }

        public void OnTodoUsed() => Unlock(TASK_STARTER);
        public void OnDiaryCreated() => Unlock(FIRST_ENTRY);
        public void OnMusicPlayed() => Unlock(FOREST_MELODY);
        public void OnPomodoroCompleted() => Unlock(IN_THE_ZONE);

        // ==================== 内部方法 ====================

        private void StartTimeAchievementCheck()
        {
            if (timeAchievementCheckCoroutine != null ||
                (IsUnlocked(DAWN_CHORUS) && IsUnlocked(NIGHT_OWL)))
            {
                return;
            }

            timeAchievementCheckCoroutine =
                this.GetSystem<IMonoSystem>().StartCoroutine(MonitorTimeAchievements());
        }

        private IEnumerator MonitorTimeAchievements()
        {
            while (!IsUnlocked(DAWN_CHORUS) || !IsUnlocked(NIGHT_OWL))
            {
                yield return TimeAchievementCheckInterval;

                int unlockedCount = data?.unlockedAchievements?.Count ?? 0;
                CheckTimeAchievements();

                // 运行期间解锁后立即保存，避免退出前丢失本地记录
                if (data?.unlockedAchievements != null &&
                    data.unlockedAchievements.Count > unlockedCount)
                {
                    this.GetSystem<ISaveSystem>().SaveData();
                }
            }

            timeAchievementCheckCoroutine = null;
        }

        private void CheckTimeAchievements()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 5 && hour < 9)
                Unlock(DAWN_CHORUS);
            if (hour >= 23 || hour < 3)
                Unlock(NIGHT_OWL);
        }

        private bool IsUnlocked(string id)
        {
            return data?.unlockedAchievements != null &&
                   data.unlockedAchievements.Contains(id);
        }

        private void UpdateConsecutiveLoginDays()
        {
            DateTime today = DateTime.Today;
            string todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (!DateTime.TryParseExact(data.lastLoginDate, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastLogin))
            {
                data.consecutiveLoginDays = 1;
                data.lastLoginDate = todayText;
                return;
            }

            int elapsedDays = (today - lastLogin.Date).Days;
            if (elapsedDays == 0)
            {
                // 修复极少数旧存档中“日期是今天但连续天数为 0”的状态。
                data.consecutiveLoginDays = Math.Max(1, data.consecutiveLoginDays);
                return;
            }

            if (elapsedDays == 1)
            {
                data.consecutiveLoginDays = Math.Max(1, data.consecutiveLoginDays) + 1;
                data.lastLoginDate = todayText;
                return;
            }

            if (elapsedDays > 1)
            {
                data.consecutiveLoginDays = 1;
                data.lastLoginDate = todayText;
                return;
            }

            // 系统时间向过去跳变时不清空已经累计的天数，也不把回拨日期计为新的一天。
            Debug.LogWarning($"[Achievement] 检测到系统日期早于上次登录日期，保留连续登录进度。上次={data.lastLoginDate}, 当前={todayText}");
        }

        private void Unlock(string id)
        {
            if (data == null)
                return;

            if (data.unlockedAchievements == null)
                data.unlockedAchievements = new List<string>();

            bool isNewLocalUnlock = !data.unlockedAchievements.Contains(id);
            if (isNewLocalUnlock)
                data.unlockedAchievements.Add(id);

#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            // 本地“已解锁”不能代表 Steam 已收到。旧逻辑在离线解锁后会永远提前 return，
            // 导致候鸟之心等成就在 Steam 恢复后也无法补发。
            if (!steamStatsReady)
            {
                if (isNewLocalUnlock)
                    Debug.Log($"[Achievement] 本地已达成，等待同步到 Steam: {id}");
                StartSteamAchievementSync();
                return;
            }

            if (TrySetSteamAchievement(id, out bool changed) && changed)
                StoreSteamAchievements();
#else
            if (isNewLocalUnlock)
                Debug.Log($"[Achievement] Local unlock: {id}");
#endif
        }

        private void StartSteamAchievementSync()
        {
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            if (steamStatsReady || steamAchievementSyncCoroutine != null)
                return;

            steamAchievementSyncCoroutine =
                this.GetSystem<IMonoSystem>().StartCoroutine(WaitForSteamAndSyncAchievements());
#endif
        }

#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
        private IEnumerator WaitForSteamAndSyncAchievements()
        {
            // 先让 StartCoroutine 的返回值写入字段，避免协程首帧结束后留下“假运行”状态。
            yield return null;

            const int maxWaitSeconds = 30;
            for (int i = 0; i < maxWaitSeconds && !SteamManager.Initialized; i++)
                yield return SteamInitializationCheckInterval;

            steamAchievementSyncCoroutine = null;
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[Achievement] Steam 未初始化，本次仅保留本地成就，下次启动会自动补发。");
                yield break;
            }

            // 当前 Steamworks.NET 版本会在游戏进程启动前自动同步当前用户统计，
            // SteamManager 初始化完成后即可读取并补发本地成就。
            steamStatsReady = true;
            SyncLocalAchievementsToSteam();
        }

        private void SyncLocalAchievementsToSteam()
        {
            if (data?.unlockedAchievements == null || data.unlockedAchievements.Count == 0)
                return;

            bool changed = false;
            for (int i = 0; i < data.unlockedAchievements.Count; i++)
            {
                string id = data.unlockedAchievements[i];
                if (string.IsNullOrEmpty(id))
                    continue;

                if (TrySetSteamAchievement(id, out bool achievementChanged))
                    changed |= achievementChanged;
            }

            if (changed)
                StoreSteamAchievements();
        }

        private bool TrySetSteamAchievement(string id, out bool changed)
        {
            changed = false;
            if (!SteamUserStats.GetAchievement(id, out bool alreadyUnlocked))
            {
                Debug.LogWarning($"[Achievement] Steam 无法读取成就 ID: {id}");
                return false;
            }

            if (alreadyUnlocked)
                return true;

            if (!SteamUserStats.SetAchievement(id))
            {
                Debug.LogWarning($"[Achievement] Steam 无法设置成就 ID: {id}");
                return false;
            }

            changed = true;
            Debug.Log($"[Achievement] 已提交到 Steam: {id}");
            return true;
        }

        private void StoreSteamAchievements()
        {
            if (!SteamUserStats.StoreStats())
                Debug.LogWarning("[Achievement] Steam StoreStats 失败，下次启动会再次同步。");
        }
#endif

        private void CheckGoldAchievements()
        {
            if (data.totalGoldEarned >= 1000) Unlock(FIRST_GOLD);
            if (data.totalGoldEarned >= 50000) Unlock(COMFORTABLE_NEST_EGG);
            if (data.totalGoldEarned >= 500000) Unlock(FEATHER_TYCOON);
        }

        private void CheckExtinctCollection()
        {
            if (extinctBirdIds.Count == 0) return;
            var illustrated = this.GetModel<ISaveModel>().IllustratedData.birds;
            foreach (int id in extinctBirdIds)
            {
                if (!illustrated.Contains(id)) return;
            }
            Unlock(BEFORE_EXTINCTION);
        }

        private void CheckAllDecorationsOwned()
        {
            var saveModel = this.GetModel<ISaveModel>();
            var configModel = this.GetModel<IConfigModel>();
            var accountData = saveModel.AccountData;
            var shopConfig = configModel.ShopConfig;

            // 检查所有场景的所有装饰是否全部购满
            for (int map = 0; map < shopConfig.sceneDecorations.Count; map++)
            {
                if (map >= accountData.sceneDecorationInfos.Count) return;
                var decos = shopConfig.sceneDecorations[map].decorations;
                for (int d = 0; d < decos.Length; d++)
                {
                    if (d >= accountData.sceneDecorationInfos[map].decorations.Count) return;
                    var info = accountData.sceneDecorationInfos[map].decorations[d];
                    int total = decos[d].fixedPositions.Length;
                    if (info.count < total) return;
                }
            }
            Unlock(MASTER_OF_AESTHETICS);
        }

        /// <summary>
        /// 缓存鸟配置：统计总种数、收集灭绝鸟ID
        /// </summary>
        private void CacheBirdConfigData()
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            HashSet<string> speciesNames = new HashSet<string>();
            extinctBirdIds.Clear();

            foreach (var scene in config.sceneBirds)
            {
                if (scene.birdClasses == null) continue;
                foreach (var birdClass in scene.birdClasses)
                {
                    if (!birdClass.canView) continue;
                    speciesNames.Add(birdClass.birdName);
                    foreach (var bird in birdClass.birds)
                    {
                        if (bird != null && bird.reality == "Extinct")
                            extinctBirdIds.Add(bird.id);
                    }
                }
            }
            totalBirdSpeciesCount = speciesNames.Count;
            Debug.Log($"[Achievement] 总鸟种数: {totalBirdSpeciesCount}, 灭绝鸟数: {extinctBirdIds.Count}");
        }

        /// <summary>
        /// 查找鸟的类名和稀有度
        /// </summary>
        private void FindBirdInfo(BirdConfig config, int birdId, out string className, out string rarity)
        {
            className = "";
            rarity = "";
            foreach (var scene in config.sceneBirds)
            {
                if (scene.birdClasses == null) continue;
                foreach (var birdClass in scene.birdClasses)
                {
                    foreach (var bird in birdClass.birds)
                    {
                        if (bird != null && bird.id == birdId)
                        {
                            className = birdClass.birdName;
                            rarity = bird.reality;
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 补发检查：基于已有存档数据，补发遗漏的成就
        /// </summary>
        private void RetroactiveCheck()
        {
            var saveModel = this.GetModel<ISaveModel>();
            var mapBirds = saveModel.BirdInfoData.mapBirds;

            // 地图成就
            if (mapBirds != null)
            {
                int count = mapBirds.Count;
                if (count >= 2) Unlock(MOUNTAIN_WANDERER);
                if (count >= 3) Unlock(DESERT_DRIFTER);
                if (count >= 4) Unlock(PADDY_FIELD_GUARDIAN);
                if (count >= 5) Unlock(WAVE_CHASER);
                if (count >= 6) Unlock(TUNDRA_WALKER);
                if (count >= 7) Unlock(WETLAND_KEEPER);
            }

            // 图鉴成就
            var illustrated = saveModel.IllustratedData;
            if (illustrated != null && illustrated.birds != null)
            {
                if (illustrated.birds.Count >= 50) Unlock(FEATHER_ENCYCLOPEDIA);
                if (totalBirdSpeciesCount > 0 && illustrated.birds.Count >= totalBirdSpeciesCount)
                    Unlock(WINGS_OF_THE_WORLD);
                CheckExtinctCollection();
            }

            // 金币
            CheckGoldAchievements();

            // 满巢
            CheckBirdSlotAchievements();
        }
    }
}
