using UnityEngine;
namespace ET.Client
{
    /// <summary>Opt-in editor verification state, owned by one practice session.</summary>
    [EnableClass]
    public sealed class BrawlAIPlayProbe
    {
        public Vector2 Direction;
        public ETTask<bool> Pending;
    }
}