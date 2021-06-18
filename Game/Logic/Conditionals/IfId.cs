using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Conditionals
{
    public class IfId : Conditional
    {
        public readonly string TargetId;

        public IfId(string targetId, params Behavior[] behaviors) : base(behaviors)
        {
            TargetId = targetId;
        }

        public override bool ConditionMet(Entity host)
        {
            return host.Desc.Id == TargetId;
        }
    }
}
