using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Behaviors
{
    public class Prioritize : Behavior
    {
        public readonly Behavior[] Behaviors;

        public Prioritize(params Behavior[] behaviors)
        {
            Behaviors = behaviors;
        }

        public override void Enter(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Enter(host);
        }

        public override bool Tick(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                if (Behaviors[k].Tick(host))
                    return true;
            return false;
        }

        public override void Exit(Entity host)
        {
            for (int k = 0; k < Behaviors.Length; k++)
                Behaviors[k].Exit(host);
        }
    }
}
