using QFramework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;

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
            var saveModel = this.GetModel<ISaveModel>();
            var birdModel = this.GetModel<IBirdModel>();
            if (saveModel.AccountData.tools.Count <= 1)
            {
                birdModel.AddedBirdCount = 0;
            }
            else if( saveModel.AccountData.tools[1].unlockedList == null || saveModel.AccountData.tools[1].unlockedList.Count ==0)
            {
                birdModel.AddedBirdCount = 0;
            }
            else
            {
                birdModel.AddedBirdCount = 0;
                for (int i = 0; i < saveModel.AccountData.tools[1].unlockedList.Count; i++)
                {
                    birdModel.AddedBirdCount += 10;
                }
            }
            
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            this.GetSystem<ISceneSystem>().LoadScene(mapIndex, progress =>
            {
                OnProgress("Loading Scene", progress);
            }, OnAllLoaded);
        }

        private void OnAllLoaded()
        {
            this.GetModel<IGameModel>().IsGameLoaded = true;
            
            this.SendCommand<FixLanguageCommand>();
            
            // 根据存档生成鸟
            this.GetSystem<IBirdSystem>().GenerateBirdsFromSave();
                
            this.GetSystem<IGameSystem>().CreateDecorations();
            
            // 设置默认的晴天环境音（初始化时不使用淡入淡出，直接切换）
            this.GetSystem<IAudioSystem>().SetEnvironmentVolumesByWeather(0, useFade: false);
                
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);

            this.GetSystem<IMonoSystem>().StartCoroutine(EnableUiInteractionsNextFrame());
        }

        private IEnumerator EnableUiInteractionsNextFrame()
        {
            yield return null;
            this.GetSystem<IGameSystem>().SendEvent<EnableHoverScaleEvent>();
            this.GetSystem<IGameSystem>().SendEvent<EnableButtonEvent>();
        }

        /// <summary>
        /// 初始化默认食物为第一个食物
        /// </summary>
        private void InitializeDefaultFood()
        {
            var configModel = this.GetModel<IConfigModel>();
            var saveModel = this.GetModel<ISaveModel>();
            if (saveModel.AccountData.tools == null)
                saveModel.AccountData.tools = new List<ToolInfo>();
            // 查找食物工具配置
            for (int i = 0; i < configModel.ShopConfig.tools.Length; i++)
            {
                if (saveModel.AccountData.tools.Count <= i)
                {
                    saveModel.AccountData.tools.Add(new ToolInfo());
                }

                if (saveModel.AccountData.tools[i].unlockedList == null)
                {
                    saveModel.AccountData.tools[i].unlockedList = new List<int>() { 0 };
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
    }
}