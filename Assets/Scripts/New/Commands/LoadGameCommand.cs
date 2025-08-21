using QFramework;
using UnityEngine;
using System.Collections;

namespace BirdGame
{
    /// <summary>
    /// 加载游戏
    /// </summary>
    public class LoadGameCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var loadingModel = this.GetModel<ILoadingModel>();
            this.GetSystem<IMonoSystem>().StartCoroutine(this.GetSystem<IAssetSystem>().PreloadEssentialAssets(v =>
            {
                loadingModel.LoadingText.Value = "Loading Assets.";
                loadingModel.Progress.Value = v;
            }, () =>
            {
                loadingModel.LoadingText.Value = "Loading Radio Config.";
                this.GetSystem<IAssetSystem>().LoadAssetAsync<RadioConfig>("RadioConfig", OnRadioConfigComplete,
                    progress =>
                    {
                        OnProgress("Loading Radio Config", progress / 5f);
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
                    OnProgress("Loading Shop Config", (progress + 1f) / 5f);
                });
        }

        private void OnShopConfigComplete(ShopConfig config)
        {
            this.GetModel<IConfigModel>().ShopConfig = config;
            
            // 初始化默认食物为第一个食物
            InitializeDefaultFood();
            
            this.GetSystem<IAssetSystem>().LoadAssetAsync<BirdConfig>("BirdConfig", OnBirdConfigComplete,
                progress =>
                {
                    OnProgress("Loading Bird Config", (progress + 2f) / 5f);
                });
        }

        private void OnBirdConfigComplete(BirdConfig config)
        {
            this.GetModel<IConfigModel>().BirdConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<LocalizationConfig>("LocalizationConfig", OnLocalizationConfigComplete,
                progress =>
                {
                    OnProgress("Loading Localization Config", (progress + 3f) / 5f);
                });
        }

        private void OnLocalizationConfigComplete(LocalizationConfig config)
        {
            this.GetModel<IConfigModel>().LocalizationConfig = config;
            
            // 调试：检查加载的语言配置
            Debug.Log($"LocalizationConfig加载完成，包含 {config.languageDic.Count} 种语言:");
            foreach (var language in config.languageDic)
            {
                Debug.Log($"  - {language.Key}: {language.Value.words.Count} 个翻译条目");
            }
            
            this.GetSystem<ISceneSystem>().LoadScene(0, progress =>
            {
                OnProgress("Loading Scene", (progress + 5f) / 6f);
            }, () =>
            {
                this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);
                
                // 游戏加载完成，显示教程弹窗
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.TutorialPopup);
            });
        }

        /// <summary>
        /// 初始化默认食物为第一个食物
        /// </summary>
        private void InitializeDefaultFood()
        {
            var gameModel = this.GetModel<IGameModel>();
            var configModel = this.GetModel<IConfigModel>();
            
            // 查找食物工具配置
            for (int i = 0; i < configModel.ShopConfig.tools.Length; i++)
            {
                var toolItem = configModel.ShopConfig.tools[i];
                if (toolItem.name.ToLower() == "food")
                {
                    // 如果食物数组不为空，设置第一个为默认食物
                    if (toolItem.selections != null && toolItem.selections.Length > 0)
                    {
                        var firstFood = toolItem.selections[0];
                        gameModel.CurrentFoodType = firstFood.selectionName;
                        
                        // 将第一个食物添加到已购买列表（作为默认食物）
                        if (!gameModel.PurchasedFoods.Contains(firstFood.selectionName))
                        {
                            gameModel.PurchasedFoods.Add(firstFood.selectionName);
                        }
                        
                        Debug.Log($"默认食物已设置为: {firstFood.selectionName}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 触发环境音效自动播放
        /// </summary>
        private IEnumerator TriggerEnvironmentAudioAfterConfigLoad()
        {
            yield return new WaitForSeconds(0.5f); // 等待一小段时间，确保配置加载完成
            
            // 只初始化环境音效，不播放歌曲
            var audioSystem = this.GetSystem<IAudioSystem>();
            if (audioSystem != null)
            {
                // 初始化环境音效（Bird音效0.5音量，其他0音量）
                audioSystem.InitEnvironments();
                
                // Debug.Log($"🌍 RadioConfig加载完成，环境音效已自动播放！");
                // Debug.Log("🐦 Bird环境音效音量设为100%，🌪️ Wind环境音效音量设为100%，其他环境音效设为0");
            }
        }
    }
}