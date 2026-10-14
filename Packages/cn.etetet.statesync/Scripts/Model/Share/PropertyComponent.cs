using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public sealed class PropertyComponent : Entity, IAwake<Dictionary<PropertyId, double>>
    {
        // double 保存精确 int 和 float 输入；业务接口校验属性类型。
        public readonly Dictionary<PropertyId, double> BaseValues = new();
        public readonly Dictionary<PropertyId, double> Values = new();
        public readonly Dictionary<long, PropertyModifier> Modifiers = new();
    }
}
