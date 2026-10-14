using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlProjectileView
    {
        public GameObject Model;
        public BrawlFaction Faction;
        public long SpawnTick;
        public long DespawnTick = -1;
        public long LastSeenSequence;
        public readonly List<BrawlPositionSample> Samples = new();
    }
}
