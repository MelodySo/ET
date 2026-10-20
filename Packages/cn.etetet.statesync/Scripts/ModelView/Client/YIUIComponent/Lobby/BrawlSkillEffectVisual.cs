using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlSkillEffectVisual
    {
        public GameObject Root;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Properties;
        public float StartedAt;
        public float Duration;
    }
}
