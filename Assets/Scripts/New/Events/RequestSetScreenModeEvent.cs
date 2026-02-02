namespace BirdGame
{
    /// <summary>
    /// 请求切换屏幕模式事件（由 GameEntry 监听并执行 SetScreenMode）
    /// </summary>
    public struct RequestSetScreenModeEvent
    {
        public int mode;       // 0=窗口模式, 1=壁纸模式, 2=全屏模式
        public bool forceChange; // 是否强制切换（忽略冷却，用于退出等场景）
    }
}
