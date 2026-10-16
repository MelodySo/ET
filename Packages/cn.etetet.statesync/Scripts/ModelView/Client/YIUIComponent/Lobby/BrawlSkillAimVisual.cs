using UnityEngine;

namespace ET.Client
{
    /// <summary>单个技能槽的本地预览资源，由训练会话持有。</summary>
    [EnableClass]
    public sealed class BrawlSkillAimVisual
    {
        public GameObject Root;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Properties;
        public int Shape;
        public float Range;
        public float WidthOrAngle;
    }
}
