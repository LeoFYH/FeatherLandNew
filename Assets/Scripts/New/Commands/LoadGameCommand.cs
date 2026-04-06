using QFramework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;
using Cysharp.Threading.Tasks; // ✅ 添加UniTask支持

namespace BirdGame
{
    /// <summary>
    /// 加载游戏
    /// </summary>
    public class LoadGameCommand : AbstractCommand
    {
        private const float configCount = 5f;

        protected override void OnExecute()
        {
            var loadingModel = this.GetModel<ILoadingModel>();


            this.GetSystem<IMonoSystem>().StartCoroutine(this.GetSystem<IAssetSystem>().PreloadEssentialAssets(v =>
            {
                loadingModel.LoadingText.Value = "Loading Assets.";
                loadingModel.Progress.Value = v;
            }, () =>
            {
                this.GetSystem<IAssetSystem>().LoadAssetAsync<Material>("BirdMaterial", mat =>
                {
                    {
                        this.GetModel<IBirdModel>().BirdMaterial = mat;
                        mat.color = Color.white;
                        loadingModel.LoadingText.Value = "Loading Radio Config.";
                        this.GetSystem<IAssetSystem>().LoadAssetAsync<RadioConfig>("RadioConfig", OnRadioConfigComplete,
                            progress => { OnProgress("Loading Radio Config", progress / configCount); });
                    }
                });
            }));
        }

        private void OnProgress(string title, float value)
        {
            var loadingModel = this.GetModel<ILoadingModel>();
            loadingModel.Progress.Value = value;
            loadingModel.LoadingText.Value = title;
        }

        private void OnRadioConfigComplete(RadioConfig config)
        {
            this.GetModel<IConfigModel>().RadioConfig = config;
            
            // RadioConfig加载完成后，触发环境音效自动播放
            this.GetSystem<IMonoSystem>().StartCoroutine(TriggerEnvironmentAudioAfterConfigLoad());
            
            this.GetSystem<IAssetSystem>().LoadAssetAsync<ShopConfig>("ShopConfig", OnShopConfigComplete,
                progress =>
                {
                    OnProgress("Loading Shop Config", (progress + 1f) / configCount);
                });
        }

        private void OnShopConfigComplete(ShopConfig config)
        {
            this.GetModel<IConfigModel>().ShopConfig = config;
            
            this.GetSystem<IAssetSystem>().LoadAssetAsync<BirdConfig>("BirdConfig", OnBirdConfigComplete,
                progress =>
                {
                    OnProgress("Loading Bird Config", (progress + 2f) / configCount);
                });
        }

        private void OnBirdConfigComplete(BirdConfig config)
        {
            this.GetModel<IConfigModel>().BirdConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<LocalizationConfig>("LocalizationConfig", OnLocalizationConfigComplete,
                progress =>
                {
                    OnProgress("Loading Localization Config", (progress + 3f) / configCount);
                });
        }

        //加载鸟类写在这里面
        private void OnLocalizationConfigComplete(LocalizationConfig config)
        {
            this.GetModel<IConfigModel>().LocalizationConfig = config;
            
            // 调试：检查加载的语言配置
            Debug.Log($"LocalizationConfig加载完成，包含 {config.languageDic.Count} 种语言:");
            foreach (var language in config.languageDic)
            {
                Debug.Log($"  - {language.Key}: {language.Value.words.Count} 个翻译条目");
            }

            this.GetSystem<IAssetSystem>().LoadAssetAsync<MapConfig>("MapConfig", OnMapConfigComplete, progress =>
            {
                OnProgress("Loading Map Config", (progress + 4f) / configCount);
            });
        }

        private void OnMapConfigComplete(MapConfig config)
        {
            this.GetModel<IConfigModel>().MapConfig = config;
            this.GetSystem<ISaveSystem>().InitData();
            this.GetSystem<IGameSystem>().InitAccount();
            InitializeDefaultFood();
            // var saveModel = this.GetModel<ISaveModel>();
            // var birdModel = this.GetModel<IBirdModel>();
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            // if (this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList == null)
            // {
            //     this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList = new List<int>();
            // }
            //
            // while (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Count)
            // {
            //     this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Add(0);
            // }
            //
            // int addedCount = this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList[mapIndex];
            // if (saveModel.AccountData.tools.Count <= 1)
            // {
            //     birdModel.AddedBirdCount = 0;
            // }
            // else if( saveModel.AccountData.tools[1].unlockedList == null || saveModel.AccountData.tools[1].unlockedList.Count ==0)
            // {
            //     birdModel.AddedBirdCount = 0;
            // }
            // else
            // {
            //     birdModel.AddedBirdCount = 0;
            //     for (int i = 0; i < saveModel.AccountData.tools[1].unlockedList.Count; i++)
            //     {
            //         birdModel.AddedBirdCount += 10;
            //     }
            // }
            //
            
            this.GetSystem<ISceneSystem>().LoadScene(mapIndex, progress =>
            {
                OnProgress("Loading Scene", progress);
            }, OnAllLoaded);
        }

        private void OnAllLoaded()
        {
            this.GetModel<IGameModel>().IsGameLoaded = true;
            
            this.SendCommand<FixLanguageCommand>();
            
            // ✅ 优化：预加载常用资源，避免运行时首次加载卡顿
            PreloadCommonAssetsAsync();
            
            // 根据存档生成鸟
            this.GetSystem<IBirdSystem>().GenerateBirdsFromSave();
                
            this.GetSystem<IGameSystem>().CreateDecorations();
            
            // ✅ 修复：不再在加载时强制设置天气音量，而是从存档加载用户设置的环境音量
            // 环境音只在场景加载时（LoadGameCommand）或天气变化时（WeatherManager）才会同步
            // 这里的 InitEnvironments 会从 MusicSettingData.environmentVolumes 中读取用户保存的音量
            this.GetSystem<IAudioSystem>().InitEnvironments();
            
            // 检查是否是第一次启动游戏，如果是则自动播放第一首歌
            if (!PlayerPrefs.HasKey("PlayedFirstSong"))
            {
                PlayerPrefs.SetString("PlayedFirstSong", "true");
                var radioModel = this.GetModel<IRadioModel>();
                var configModel = this.GetModel<IConfigModel>();
                
                // 确保RadioConfig已加载且音乐列表不为空
                if (configModel.RadioConfig != null && configModel.RadioConfig.musicItems != null && configModel.RadioConfig.musicItems.Length > 0)
                {
                    // 确保第一首歌的音乐文件存在
                    if (configModel.RadioConfig.musicItems[0].songFile != null)
                    {
                        radioModel.SongIndex = 0;
                        this.GetSystem<IAudioSystem>().PlaySong();
                        Debug.Log("首次启动，自动播放第一首歌");
                    }
                }
            }
                
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);

            this.GetSystem<IMonoSystem>().StartCoroutine(EnableUiInteractionsNextFrame());

            // 释放非当前场景和不常用popup的预加载资源，大幅降低内存占用
            ReleaseUnusedPreloadedAssets();
            // 清空非当前地图鸟的重资源引用（clickAudio只用于当前地图的鸟）
            StripNonCurrentMapBirdAssets();
            // 延迟执行完整清理，确保异步卸载完成
            this.GetSystem<IMonoSystem>().StartCoroutine(DeferredMemoryCleanup());
        }

        private IEnumerator EnableUiInteractionsNextFrame()
        {
            yield return null;
            this.GetSystem<IGameSystem>().SendEvent<EnableHoverScaleEvent>();
            this.GetSystem<IGameSystem>().SendEvent<EnableButtonEvent>();
        }

        /// <summary>
        /// 释放预加载但当前不需要的资源
        /// PreloadEssentialAssets 会把所有 "preload" 标签资源加载到 HandleDic，
        /// 但玩家同时只在一个地图，其余6个场景的纹理/精灵白占内存。
        /// Popup也是按需加载即可，首次打开会从Addressables重新加载（本地加载很快）。
        /// </summary>
        private void ReleaseUnusedPreloadedAssets()
        {
            int currentMap = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var assetSystem = this.GetSystem<IAssetSystem>();

            // 1. 释放非当前地图的场景prefab（每个场景含天气/背景等大量纹理）
            for (int i = 0; i <= 6; i++)
            {
                if (i != currentMap)
                {
                    assetSystem.ReleaseAsset($"Scene{i}");
                }
            }

            // 2. 释放所有预加载的装饰场景prefab（CreateDecorations按单个GUID加载，这些容器prefab不需要）
            string[] decorationScenes = {
                "Assets/Prefabs/Decorations/Scene1",
                "Assets/Prefabs/Decorations/Scene2",
                "Assets/Prefabs/Decorations/Scene4",
                "Assets/Prefabs/Decorations/Scene5",
                "Assets/Prefabs/Decorations/Scene6",
                "Assets/Prefabs/Decorations/Scene7"
            };
            foreach (var dec in decorationScenes)
            {
                assetSystem.ReleaseAsset(dec);
            }

            // 3. 释放开蛋动画（仅在开蛋时需要，按需重新加载）
            assetSystem.ReleaseAsset("OpenEggAnim");

            // 4. 释放不常用的UI Popup prefab（按需从Addressables重新加载，本地加载很快）
            string[] deferredPopups = {
                "TutorialPopup", "RadioPopup", "IllustratedPopup",
                "MapPopup", "NotePopup", "ShopPopup", "SettingPopup",
                "HatchingBirdPopup", "ClockPopup", "MapInfo",
                "ThanksPopup", "AddCoinPopup", "InfoPopup",
                "BuyFailPopup", "BuyConfirmPopup", "MouseMenu"
            };
            foreach (var popup in deferredPopups)
            {
                assetSystem.ReleaseAsset(popup);
            }

            Debug.Log($"已释放非当前场景(保留Scene{currentMap})、装饰容器、开蛋动画和16个Popup的预加载资源");
        }

        /// <summary>
        /// 清空非当前地图鸟的重资源引用。
        /// BirdConfig作为ScriptableObject直接引用了所有地图80+鸟的Sprite和AudioClip，
        /// 但运行时只需要当前地图的鸟的clickAudio，其他地图的这些资源白占内存。
        /// 置空后GC可以回收这些Sprite/AudioClip对象。
        /// </summary>
        private void StripNonCurrentMapBirdAssets()
        {
            int currentMap = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            if (config?.sceneBirds == null) return;

            for (int mapIdx = 0; mapIdx < config.sceneBirds.Count; mapIdx++)
            {
                if (mapIdx == currentMap) continue;
                var sceneBird = config.sceneBirds[mapIdx];
                if (sceneBird?.birdClasses == null) continue;
                foreach (var birdClass in sceneBird.birdClasses)
                {
                    if (birdClass?.birds == null) continue;
                    foreach (var bird in birdClass.birds)
                    {
                        if (bird == null) continue;
                        bird.clickAudio = null;  // 非当前地图的鸟不会播放点击音效
                        bird.scenePreview = null; // 图鉴场景预览 — 打开图鉴时图片为空但不影响功能
                    }
                }
            }
            Debug.Log($"已清空非当前地图(map{currentMap})鸟的clickAudio和scenePreview引用");
        }

        /// <summary>
        /// 延迟执行完整内存清理，确保异步操作完成后再回收
        /// </summary>
        private IEnumerator DeferredMemoryCleanup()
        {
            // 等待1帧让Addressables异步释放完成
            yield return null;
            // 第一轮：回收托管对象引用
            System.GC.Collect();
            // 异步卸载无引用的原生资源（纹理/网格/AudioClip等）
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            // 第二轮：清理卸载过程中产生的托管垃圾
            System.GC.Collect();
            Debug.Log("延迟内存清理完成");
        }

        /// <summary>
        /// 初始化默认食物为第一个食物
        /// </summary>
        private void InitializeDefaultFood()
        {
            var configModel = this.GetModel<IConfigModel>();
            var saveModel = this.GetModel<ISaveModel>();
            if (saveModel.AccountData.sceneTools == null)
                saveModel.AccountData.sceneTools = new List<SceneToolInfo>();
            saveModel.AccountData.sceneTools.Add(new SceneToolInfo());
            // 查找食物工具配置
            for (int i = 0; i < configModel.ShopConfig.tools.Length; i++)
            {
                if (saveModel.AccountData.sceneTools[0].tools.Count <= i)
                {
                    saveModel.AccountData.sceneTools[0].tools.Add(new ToolInfo());
                }

                if (saveModel.AccountData.sceneTools[0].tools[i].unlockedList == null)
                {
                    saveModel.AccountData.sceneTools[0].tools[i].unlockedList = new List<int>() { 0 };
                }

                // var toolItem = configModel.ShopConfig.tools[i];
                // if (toolItem.name.ToLower() == "food")
                // {
                //     // 如果食物数组不为空，设置第一个为默认食物
                //     if (toolItem.selections != null && toolItem.selections.Length > 0)
                //     {
                //         var firstFood = toolItem.selections[0];
                //         gameModel.CurrentFoodType = firstFood.selectionName;
                //         
                //         // 将第一个食物添加到已购买列表（作为默认食物）
                //         if (!gameModel.PurchasedFoods.Contains(firstFood.selectionName))
                //         {
                //             gameModel.PurchasedFoods.Add(firstFood.selectionName);
                //         }
                //         
                //         Debug.Log($"默认食物已设置为: {firstFood.selectionName}");
                //     }
                //     break;
                // }
            }
        }

        /// <summary>
        /// 触发环境音效自动播放
        /// </summary>
        private IEnumerator TriggerEnvironmentAudioAfterConfigLoad()
        {
            yield return new WaitForSeconds(1f); // 增加等待时间，确保所有系统初始化完成

            // 检查MusicSettingData是否已加载
            while (this.GetModel<ISaveModel>().MusicSettingData == null)
            {
                //Debug.LogError("MusicSettingData未加载，跳过环境音效初始化");
                yield return new WaitForFixedUpdate();
            }
            
            try
            {
                // 初始化环境音效（Bird音效0.5音量，其他0音量）
                this.GetSystem<IAudioSystem>().InitEnvironments();
                Debug.Log("🌍 环境音效初始化完成！");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"环境音效初始化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// ✅ 优化：异步预加载常用资源
        /// </summary>
        private async void PreloadCommonAssetsAsync()
        {
            try
            {
                var preloadSystem = this.GetSystem<IAssetPreloadSystem>();
                await preloadSystem.PreloadCommonAssets();
                Debug.Log("✅ 常用资源预加载完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"资源预加载失败: {e.Message}");
            }
        }
    }
}