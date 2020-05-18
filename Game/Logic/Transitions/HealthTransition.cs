using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Transitions
{
    public class HealthTransition : Transition
    {
        public float Threshold;

        public HealthTransition(float threshold, string targetState) : base(targetState)
        {
            Threshold = threshold;
        }

        public override bool Tick(Entity host)
        {
            float hpp = host.GetHealthPercentage();
            if (hpp <= Threshold)
                return true;
            return false;
        }
    }
}
