using UnityEngine;

namespace ET.Client
{
    public static class BrawlSkillInputHelper
    {
        public static void Cast(BrawlTrainingSession session, BrawlSkillSlot slot, Vector2 direction)
        {
            if (session.Loading || session.Closing || session.CancelRequested || !session.IsPractice) return;
            // 大招共用完整手势和预览入口，但本轮不发送、不执行大招。
            if (slot != BrawlSkillSlot.NormalAttack || direction.sqrMagnitude <= 0) return;
            ClientSenderComponent sender = session.PracticeSender;
            if (sender == null || session.PracticeEntryId == 0) return;
            C2G_BrawlNormalAttack message = C2G_BrawlNormalAttack.Create();
            message.EntryId = session.PracticeEntryId;
            message.Sequence = ++session.AttackSequence;
            message.X = direction.x;
            message.Z = direction.y;
            sender.Send(message);
        }
    }
}

