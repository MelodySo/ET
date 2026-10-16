using UnityEngine;

namespace ET.Client
{
    /// <summary>平面完整范围预览，不执行碰撞、不改变朝向、不发送施法请求。</summary>
    public static class BrawlSkillAimVisualHelper
    {
        private const string PrefabAddress = "BrawlSkillAimPreview";
        private const float GroundOffset = 0.04f;

        public static async ETTask Initialize(BrawlTrainingSession session)
        {
            foreach (BrawlSkillSlot slot in new[] { BrawlSkillSlot.NormalAttack, BrawlSkillSlot.Super })
            {
                GameObject root = await BrawlModelHelper.InstantiateModel(session, PrefabAddress,
                    session.RuntimeRoot.transform, $"SkillAim_{slot}", Vector3.zero);
                if (session.CancelRequested || session.Closing)
                {
                    if (root != null) UnityEngine.Object.Destroy(root);
                    return;
                }
                root.SetActive(false);
                Mesh mesh = new() { name = $"SkillAim_{slot}_Runtime" };
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                session.SkillAimVisuals.Add(slot, new BrawlSkillAimVisual
                {
                    Root = root, Mesh = mesh, Renderer = root.GetComponent<MeshRenderer>(),
                    Properties = new MaterialPropertyBlock(),
                });
            }
            session.SkillAimUnitId = session.MyUnitId;
        }

        public static void Show(BrawlTrainingSession session, BrawlSkillAimPreviewData data)
        {
            if (!session.SkillAimVisuals.TryGetValue(data.Slot, out BrawlSkillAimVisual view)) return;
            if (session.Closing || session.CancelRequested || data.Config == null ||
                !Finite(data.Direction.x) || !Finite(data.Direction.y) || data.Direction.sqrMagnitude == 0)
            {
                Hide(session, data.Slot);
                return;
            }
            BrawlSkillConfig config = data.Config;
            // 施放范围与技能范围不是同一枚举：施放类型2是抛物线，不能解释成扇形。
            if (config.SkillCastRangeType != 0 && config.SkillCastRangeType != 1)
            {
                Hide(session, data.Slot);
                return;
            }
            int shape = config.SkillCastRangeType == 0 ? config.SkillRangeType : config.SkillCastRangeType;
            float range = config.SkillCastRangeType == 0 ? config.SkillRangeParam1 : config.SkillCastParam1;
            float widthOrAngle = config.SkillCastRangeType == 0 ? config.SkillRangeParam2 : config.SkillCastParam2;
            if (!Finite(range) || range <= 0 || !Finite(widthOrAngle) ||
                (shape == (int)BrawlSkillRangeType.Rectangle && widthOrAngle <= 0) ||
                (shape == (int)BrawlSkillRangeType.Sector && (widthOrAngle <= 0 || widthOrAngle > 360)) ||
                (shape != (int)BrawlSkillRangeType.Rectangle && shape != (int)BrawlSkillRangeType.Sector && shape != (int)BrawlSkillRangeType.Circle))
            {
                Hide(session, data.Slot);
                return;
            }
            if (view.Shape != shape || view.Range != range || view.WidthOrAngle != widthOrAngle)
            {
                view.Shape = shape;
                view.Range = range;
                view.WidthOrAngle = widthOrAngle;
                // 仅四顶点承载平面；Shader 在真实米制坐标内裁出扇形／圆并绘制等宽边缘。
                float halfWidth = shape == (int)BrawlSkillRangeType.Rectangle ? widthOrAngle / 2 : range;
                float back = shape == (int)BrawlSkillRangeType.Rectangle ? 0 : -range;
                view.Mesh.Clear();
                view.Mesh.vertices = new[]
                {
                    new Vector3(-halfWidth, 0, back), new Vector3(halfWidth, 0, back),
                    new Vector3(-halfWidth, 0, range), new Vector3(halfWidth, 0, range),
                };
                view.Mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                view.Mesh.RecalculateBounds();
                view.Properties.SetFloat("_Shape", shape);
                view.Properties.SetFloat("_Range", range);
                view.Properties.SetFloat("_Width", widthOrAngle);
                view.Properties.SetFloat("_HalfAngle", widthOrAngle * Mathf.Deg2Rad / 2);
                view.Properties.SetColor("_Color", data.Slot == BrawlSkillSlot.Super
                    ? new Color(1f, 0.82f, 0.2f, 1f) : new Color(0.8f, 0.95f, 1f, 1f));
                view.Renderer.SetPropertyBlock(view.Properties);
            }
            view.Root.transform.SetPositionAndRotation(data.Origin + Vector3.up * GroundOffset,
                Quaternion.LookRotation(new Vector3(data.Direction.x, 0, data.Direction.y), Vector3.up));
            view.Root.SetActive(true);
        }

        public static void Hide(BrawlTrainingSession session, BrawlSkillSlot slot)
        {
            if (session.SkillAimVisuals.TryGetValue(slot, out BrawlSkillAimVisual view) && view.Root != null)
                view.Root.SetActive(false);
        }

        public static void Clear(BrawlTrainingSession session)
        {
            session.SkillAimPreview = null;
            foreach (BrawlSkillAimVisual view in session.SkillAimVisuals.Values)
            {
                if (view.Root != null) UnityEngine.Object.Destroy(view.Root);
                if (view.Mesh != null) UnityEngine.Object.Destroy(view.Mesh);
            }
            session.SkillAimVisuals.Clear();
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
