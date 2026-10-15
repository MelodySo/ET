namespace ET.Server
{
    public struct BrawlProjectileContact
    {
        public float Fraction;
        public long UnitId;
        public int TerrainIndex;
        public Unity.Mathematics.float2 Normal;
        public bool Blocked;
        public bool Effective;
        public bool Wall;
    }
}
