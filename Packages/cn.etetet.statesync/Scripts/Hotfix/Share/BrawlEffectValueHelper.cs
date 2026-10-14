using System;

namespace ET
{
    public static class BrawlEffectValueHelper
    {
        public static float Calculate(int value, int growth, int level)
        {
            if (level < 1) throw new ArgumentException("Hero level must be at least 1.");
            double result = value + (level - 1d) * growth;
            if (result < 0 || result > float.MaxValue) throw new ArgumentException("Effect value is invalid.");
            return (float)result;
        }
    }
}
