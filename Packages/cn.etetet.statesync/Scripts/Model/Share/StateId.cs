namespace ET
{
    // 稳定编号，不要求连续；逻辑状态与客户端动画状态机无关。
    public enum StateId
    {
        Stunned = 100,
        Airborne = 200,
        Invincible = 300,
        Dead = 400,
        Spawning = 500,
    }
}
