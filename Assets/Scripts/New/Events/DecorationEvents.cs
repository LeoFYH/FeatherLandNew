namespace BirdGame
{
    /// <summary>
    /// 场景装饰发生增删(购买/右键删除)后广播,商店装饰条目据此刷新按钮状态。
    /// 此前删除装饰不发事件也不动金币,商店开着时条目无从刷新,
    /// 卖掉后仍灰显"已装备"直到重开商店(2026-07-13)。
    /// </summary>
    public struct OnDecorationChangedEvent
    {
    }
}
