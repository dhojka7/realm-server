using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic
{
    public abstract class Transition : IBehavior
    {
        public readonly int Id;

        public Transition(string targetState)
        {
            StringTargetState = targetState.ToLower();
            Id = ++BehaviorDb.NextId;
        }

        public string StringTargetState; //Only used for parsing.
        public int TargetState;

        public virtual void Enter(Entity host) { }
        public virtual bool Tick(Entity host) => false;
        public virtual void Exit(Entity host) { }
    }
}
