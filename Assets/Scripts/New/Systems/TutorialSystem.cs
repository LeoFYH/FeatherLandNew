using QFramework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class TutorialSystem : AbstractSystem, ITutorialSystem
    {
        // 运行时数据
        private TutorialConfig currentConfig;
        private int currentStepIndex = 0;
        private bool isActive = false;
        private bool isPaused = false;
        
        // UI组件
        private GameObject tutorialIcon;
        private GameObject tutorialTip;
        private Coroutine iconAnimationCoroutine;
        
        // 事件回调
        private Action<string> onStepCompleted;
        private Action<string> onTutorialCompleted;
        
        protected override void OnInit()
        {
            // 初始化教学系统
            LoadTutorialConfigs();
        }
        
        private void LoadTutorialConfigs()
        {
            // 这里可以通过AssetSystem加载配置
            // 暂时留空，后续可以通过配置文件加载
        }
        
        #region 教学控制
        
        public void StartTutorial(string tutorialId)
        {
            if (isActive) return;
            
            // 检查是否已完成
            if (IsTutorialCompleted(tutorialId))
            {
                Debug.Log($"教学 {tutorialId} 已完成");
                return;
            }
            
            // 加载配置
            LoadTutorialConfig(tutorialId);
            
            if (currentConfig != null)
            {
                isActive = true;
                isPaused = false;
                currentStepIndex = 0;
                
                Debug.Log($"开始教学: {currentConfig.tutorialName}");
                ShowCurrentStep();
            }
            else
            {
                Debug.LogError($"未找到教学配置: {tutorialId}");
            }
        }
        
        public void StopTutorial()
        {
            if (!isActive) return;
            
            HideCurrentStep();
            isActive = false;
            isPaused = false;
            currentStepIndex = 0;
            currentConfig = null;
            
            Debug.Log("教学已停止");
        }
        
        public void PauseTutorial()
        {
            if (!isActive || isPaused) return;
            
            isPaused = true;
            HideCurrentStep();
            Debug.Log("教学已暂停");
        }
        
        public void ResumeTutorial()
        {
            if (!isActive || !isPaused) return;
            
            isPaused = false;
            ShowCurrentStep();
            Debug.Log("教学已恢复");
        }
        
        public void SkipTutorial()
        {
            if (!isActive) return;
            
            SaveTutorialProgress();
            StopTutorial();
            Debug.Log("教学已跳过");
        }
        
        #endregion
        
        #region 步骤控制
        
        public void NextStep()
        {
            if (!isActive || isPaused) return;
            
            if (currentStepIndex < currentConfig.steps.Count - 1)
            {
                currentStepIndex++;
                ShowCurrentStep();
                onStepCompleted?.Invoke(GetCurrentStepId());
            }
            else
            {
                // 教学完成
                SaveTutorialProgress();
                onTutorialCompleted?.Invoke(currentConfig.tutorialId);
                StopTutorial();
                Debug.Log("教学完成");
            }
        }
        
        public void PreviousStep()
        {
            if (!isActive || isPaused) return;
            
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                ShowCurrentStep();
            }
        }
        
        public void JumpToStep(int stepIndex)
        {
            if (!isActive || isPaused) return;
            
            if (stepIndex >= 0 && stepIndex < currentConfig.steps.Count)
            {
                currentStepIndex = stepIndex;
                ShowCurrentStep();
            }
        }
        
        #endregion
        
        #region 状态查询
        
        public bool IsTutorialActive() => isActive && !isPaused;
        
        public bool IsTutorialPaused() => isPaused;
        
        public int GetCurrentStepIndex() => currentStepIndex;
        
        public string GetCurrentStepId()
        {
            if (currentConfig != null && currentStepIndex < currentConfig.steps.Count)
            {
                return currentConfig.steps[currentStepIndex].stepId;
            }
            return string.Empty;
        }
        
        #endregion
        
        #region 交互验证
        
        public void OnTargetClicked(string targetName)
        {
            if (!isActive || isPaused) return;
            
            var currentStep = GetCurrentStep();
            if (currentStep != null && currentStep.targetButtonName == targetName)
            {
                Debug.Log($"目标 {targetName} 被点击");
                NextStep();
            }
        }
        
        public void OnTargetHovered(string targetName)
        {
            if (!isActive || isPaused) return;
            
            var currentStep = GetCurrentStep();
            if (currentStep != null && currentStep.targetButtonName == targetName)
            {
                // 可以在这里添加悬停效果
                Debug.Log($"目标 {targetName} 被悬停");
            }
        }
        
        #endregion
        
        #region 进度管理
        
        public void SaveTutorialProgress()
        {
            if (currentConfig != null)
            {
                string key = $"Tutorial_{currentConfig.tutorialId}_Completed";
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                Debug.Log($"教学进度已保存: {currentConfig.tutorialId}");
            }
        }
        
        public bool IsTutorialCompleted(string tutorialId)
        {
            string key = $"Tutorial_{tutorialId}_Completed";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }
        
        public void ResetTutorialProgress(string tutorialId)
        {
            string key = $"Tutorial_{tutorialId}_Completed";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"教学进度已重置: {tutorialId}");
        }
        
        #endregion
        
        #region 私有方法
        
        private void LoadTutorialConfig(string tutorialId)
        {
            // 从ConfigModel中获取TutorialConfig
            var configModel = this.GetModel<IConfigModel>();
            Debug.Log($"正在加载教学配置，TutorialId: {tutorialId}");
            Debug.Log($"ConfigModel.TutorialConfig: {configModel.TutorialConfig}");
            
            if (configModel.TutorialConfig != null)
            {
                currentConfig = configModel.TutorialConfig;
                Debug.Log($"教学配置已加载: {currentConfig.tutorialName}");
            }
            else
            {
                Debug.LogError("TutorialConfig未加载，请检查AssetSystem是否正确加载了配置文件");
            }
        }
        
        private TutorialConfig.TutorialStep GetCurrentStep()
        {
            if (currentConfig != null && currentStepIndex < currentConfig.steps.Count)
            {
                return currentConfig.steps[currentStepIndex];
            }
            return null;
        }
        
        private void ShowCurrentStep()
        {
            var step = GetCurrentStep();
            if (step == null) return;
            
            HideCurrentStep();
            
            // 创建教学图标
            CreateTutorialIcon(step);
            
            // 创建提示文本
            CreateTutorialTip(step);
            
            Debug.Log($"显示教学步骤: {step.stepName}");
        }
        
        private void HideCurrentStep()
        {
            if (tutorialIcon != null)
            {
                GameObject.Destroy(tutorialIcon);
                tutorialIcon = null;
            }
            
            if (tutorialTip != null)
            {
                GameObject.Destroy(tutorialTip);
                tutorialTip = null;
            }
            
            if (iconAnimationCoroutine != null)
            {
                this.GetSystem<IMonoSystem>().StopCoroutine(iconAnimationCoroutine);
                iconAnimationCoroutine = null;
            }
        }
        
        private void CreateTutorialIcon(TutorialConfig.TutorialStep step)
        {
            // 创建图标GameObject
            tutorialIcon = new GameObject("TutorialIcon");
            
            // 添加SpriteRenderer
            SpriteRenderer spriteRenderer = tutorialIcon.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = step.iconSprite;
            spriteRenderer.sortingOrder = 1000; // 确保在最前面
            
            // 设置位置
            GameObject targetObject = GameObject.Find(step.targetButtonName);
            if (targetObject != null)
            {
                Vector3 targetPosition = targetObject.transform.position + step.iconOffset;
                tutorialIcon.transform.position = targetPosition;
            }
            
            // 设置缩放
            tutorialIcon.transform.localScale = step.iconScale;
            
            // 添加闪烁效果
            if (step.iconPulsing)
            {
                iconAnimationCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(IconPulsingAnimation());
            }
        }
        
        private void CreateTutorialTip(TutorialConfig.TutorialStep step)
        {
            if (string.IsNullOrEmpty(step.tipText)) return;
            
            // 创建提示文本GameObject
            tutorialTip = new GameObject("TutorialTip");
            
            // 添加Canvas
            Canvas canvas = tutorialTip.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;
            
            // 添加CanvasScaler
            CanvasScaler scaler = tutorialTip.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加Text
            GameObject textObj = new GameObject("TipText");
            textObj.transform.SetParent(tutorialTip.transform);
            
            Text text = textObj.AddComponent<Text>();
            text.text = step.tipText;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // 设置位置
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = step.tipTextPosition;
            rectTransform.sizeDelta = new Vector2(400, 100);
        }
        
        private IEnumerator IconPulsingAnimation()
        {
            if (tutorialIcon == null) yield break;
            
            Vector3 originalScale = tutorialIcon.transform.localScale;
            float pulseSpeed = currentConfig.iconAnimationSpeed;
            
            while (tutorialIcon != null)
            {
                float scale = 1f + 0.2f * Mathf.Sin(Time.time * pulseSpeed * 2f);
                tutorialIcon.transform.localScale = originalScale * scale;
                yield return null;
            }
        }
        
        #endregion
    }
} 