using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 售后诊断用：模拟系统音频设备重置（触发 AudioSettings.OnAudioConfigurationChanged），
    /// 用于验证 AudioSystem 的设备韧性恢复逻辑（玩家“游戏完全无声”问题）。
    /// </summary>
    public static class AudioDeviceDebugMenu
    {
        [MenuItem("Tools/Debug/模拟音频设备重置 (Audio Reset)")]
        public static void SimulateAudioReset()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Audio] 请在 Play 模式下使用：该操作模拟运行时的音频设备重置");
                return;
            }

            Debug.Log("[Audio] （调试）手动触发 AudioSettings.Reset 模拟设备重置");
            AudioSettings.Reset(AudioSettings.GetConfiguration());
        }
    }
}
