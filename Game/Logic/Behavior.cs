using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace RotMG.Game.Logic
{
    public abstract class Behavior : IBehavior
    {
        public readonly int Id;

        public Behavior()
        {
            Id = ++BehaviorDb.NextId;
        }

        public virtual void Enter(Entity host) { }
        public virtual bool Tick(Entity host) => true;
        public virtual void Exit(Entity host) { }
        public virtual void Death(Entity host) { }
    }
}
