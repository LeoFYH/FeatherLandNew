using System.Collections.Generic;
using UnityEngine;

namespace BirdGame
{
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Config/TutorialConfig")]
    public class TutorialConfig : ScriptableObject
    {
        [System.Serializable]
        public class TutorialStep
        {
            [Header("步骤信息")]
            public string stepId;                    // 步骤唯一ID
            public string stepName;                  // 步骤名称
            public int stepOrder;                    // 步骤顺序
            
            [Header("目标对象")]
            public string targetButtonName;          // 目标按钮的GameObject名称
            public Vector3 iconOffset;               // 图标相对于目标的偏移位置
            
            [Header("图标设置")]
            public Sprite iconSprite;                // 教学图标
            public Vector3 iconScale = Vector3.one;  // 图标缩放
            public bool iconPulsing = true;          // 是否图标闪烁效果
            
            [Header("交互设置")]
            public bool isRequired = true;           // 是否必须点击才能继续
            public string clickEventName;            // 点击事件名称（用于验证）
            
            [Header("提示文本")]
            public string tipText;                   // 提示文本
            public Vector3 tipTextPosition;          // 提示文本位置
        }
        
        [Header("教学配置")]
        public string tutorialId;                    // 教学唯一ID
        public string tutorialName;                  // 教学名称
        public List<TutorialStep> steps = new List<TutorialStep>();
        
        [Header("全局设置")]
        public bool autoStart = false;              // 是否自动开始
        public bool canSkip = false;                // 是否可以跳过
        public float iconAnimationSpeed = 1f;        // 图标动画速度
    }
} 