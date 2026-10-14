using UnityEngine;

namespace ET.Client
{
    public static class BrawlSkillInputHelper
    {
        public static void Cast(BrawlTrainingSession session, BrawlSkillSlot slot, Vector2 direction)
        {
            if (session.Loading || session.Closing || session.CancelRequested || !session.IsPractice) return;
            if ((slot != BrawlSkillSlot.NormalAttack && slot != BrawlSkillSlot.Super) || direction.sqrMagnitude <= 0) return;
            ClientSenderComponent sender = session.PracticeSender;
            if (sender == null || session.PracticeEntryId == 0) return;
            C2G_BrawlCastSkill message = C2G_BrawlCastSkill.Create();
            message.EntryId = session.PracticeEntryId;
            message.Sequence = ++session.AttackSequence;
            message.X = direction.x;
            message.Z = direction.y;
            message.Slot = (int)slot;
            sender.Send(message);
        }
    }
}

