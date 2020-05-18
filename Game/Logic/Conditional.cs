using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic
{
    public abstract class Conditional : Behavior
    {
        public readonly Behavior[] Behaviors;

        public Conditional(params Behavior[] behaviors) 
        {
            Behaviors = behaviors;
        }

        public virtual bool ConditionMet(Entity host)
        {
            return false;
        }

        public override void Enter(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Enter(host);
        }

        public override bool Tick(Entity host)
        {
            if (ConditionMet(host))
            {
                for (int k = 0; k < Behaviors.Length; k++)
                    Behaviors[k].Tick(host);
                return true;
            }
            return false;
        }

        public override void Exit(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Exit(host);
        }

        public override void Death(Entity host)
        {
            if (ConditionMet(host))
            {
                for (int k = 0; k < Behaviors.Length; k++)
                    Behaviors[k].Death(host);
            }
        }

    }
}
