using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace ET
{
    [CreateAssetMenu(menuName = "ET/Brawl AI")]
    public sealed class BrawlAIScriptableObject : SerializedScriptableObject, IBTAssetExporter
    {
        public void ExportBehaviorTree() => BrawlAIEditor.Export(this);
        public int HeroId = 201;
        [Tooltip("练习赛中选择此英雄时生成的对手；0表示不参与练习赛配对导出。")]
        public int OpponentHeroId;
        [NonSerialized, OdinSerialize] public BrawlAIRoot Tree = new();
    }
}
