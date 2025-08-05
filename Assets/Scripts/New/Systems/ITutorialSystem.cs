using QFramework;
using System;

namespace BirdGame
{
    public interface ITutorialSystem : ISystem
    {
        // 教学控制
        void StartTutorial(string tutorialId);
        void StopTutorial();
        void PauseTutorial();
        void ResumeTutorial();
        void SkipTutorial();
        
        // 步骤控制
        void NextStep();
        void PreviousStep();
        void JumpToStep(int stepIndex);
        
        // 状态查询
        bool IsTutorialActive();
        bool IsTutorialPaused();
        int GetCurrentStepIndex();
        string GetCurrentStepId();
        
        // 交互验证
        void OnTargetClicked(string targetName);
        void OnTargetHovered(string targetName);
        
        // 进度管理
        void SaveTutorialProgress();
        bool IsTutorialCompleted(string tutorialId);
        void ResetTutorialProgress(string tutorialId);
    }
} 