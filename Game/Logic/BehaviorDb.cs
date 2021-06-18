using RotMG.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RotMG.Game.Logic
{
    public interface IBehavior { }

    public interface IBehaviorDatabase
    {
        public void Init(BehaviorDb db);
    }

    public class BehaviorModel 
    {
        public Dictionary<int, State> States;
        public List<Behavior> Behaviors;
        public List<Loot> Loots;

        public BehaviorModel(params IBehavior[] behaviors)
        {
            States = new Dictionary<int, State>();
            Behaviors = new List<Behavior>();
            Loots = new List<Loot>();
            foreach (IBehavior bh in behaviors)
            {
                if (bh is Loot) Loots.Add(bh as Loot);
                if (bh is Behavior) Behaviors.Add(bh as Behavior);
                if (bh is State)
                {
                    State state = bh as State;
                    States.Add(state.Id, state);
                }
            }

            foreach (State s1 in States.Values)
                foreach (Transition t in s1.Transitions)
                    foreach (State s2 in States.Values)
                        if (s2.StringId == t.StringTargetState)
                            t.TargetState = s2.Id;

            foreach (State s1 in States.Values)
                foreach (State s2 in s1.States.Values)
                    s2.FindStateTransitions();
        }
    }

    public class BehaviorDb
    {
        public static int NextId;
        public Dictionary<int, BehaviorModel> Models;

        public BehaviorDb()
        {
            Models = new Dictionary<int, BehaviorModel>();
            IEnumerable<Type> results = from type in Assembly.GetCallingAssembly().GetTypes()
                          where typeof(IBehaviorDatabase).IsAssignableFrom(type) && !type.IsInterface
                          select type;

            foreach (Type k in results)
            {
#if DEBUG
                Program.Print(PrintType.Debug, $"Initializing Behavior <{k.ToString()}>");
#endif
                IBehaviorDatabase bd = (IBehaviorDatabase)Activator.CreateInstance(k);
                bd.Init(this);
            }
        }

        public void Init(string id, params IBehavior[] behaviors)
        {
            int type = Resources.Id2Object[id].Type;
#if DEBUG
            if (Models.ContainsKey(type))
                throw new Exception("Behavior already resolved for this entity.");
#endif

            Models[type] = new BehaviorModel(behaviors);
        }

        public void Init(string[] ids, params IBehavior[] behaviors)
        {
#if DEBUG
            if (ids == null || ids.Length == 0)
                throw new Exception("pls");
#endif
            foreach (var id in ids)
                Init(id, behaviors);
        }

        public BehaviorModel Resolve(ushort type)
        {
            if (Models.TryGetValue((int)type, out BehaviorModel model))
                return model;
            return null;
        }
    }
}
