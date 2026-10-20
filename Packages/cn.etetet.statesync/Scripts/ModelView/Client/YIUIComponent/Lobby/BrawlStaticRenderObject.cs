using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlStaticRenderObject
    {
        public GameObject Root;
        public Matrix4x4 Matrix;
        public readonly List<MeshRenderer> Renderers = new();
    }
}
