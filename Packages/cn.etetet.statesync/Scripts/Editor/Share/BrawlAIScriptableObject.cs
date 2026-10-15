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
        [NonSerialized, OdinSerialize] public BrawlAIRoot Tree = new();
    }
}