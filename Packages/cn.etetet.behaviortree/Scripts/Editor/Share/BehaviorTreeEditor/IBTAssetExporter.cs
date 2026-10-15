namespace ET
{
    /// <summary>Allows a tree asset to export its own configuration without a reverse package dependency.</summary>
    public interface IBTAssetExporter
    {
        void ExportBehaviorTree();
    }
}