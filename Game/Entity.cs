using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Game.Logic;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RotMG.Game
{
    public enum StatType
    {
        MaxHP,
        HP,
        Size,
        MaxMP,
        MP,
        NextLevelEXP,
        EXP,
        Level,
        Inventory_0,
        Inventory_1,
        Inventory_2,
        Inventory_3,
        Inventory_4,
        Inventory_5,
        Inventory_6,
        Inventory_7,
        Inventory_8,
        Inventory_9,
        Inventory_10,
        Inventory_11,
        Attack,
        Defense,
        Speed,
        Vitality,
        Wisdom,
        Dexterity,
        Condition,
        NumStars,
        Name,
        Tex1,
        Tex2,
        MerchandiseType,
        MerchandisePrice,
        Credits,
        Active,
        AccountId,
        Fame,
        MerchandiseCurrency,
        Connect,
        MerchandiseCount,
        MerchandiseMinsLeft,
        MerchandiseDiscount,
        MerchandiseRankReq,
        MaxHPBoost,
        MaxMPBoost,
        AttackBoost,
        DefenseBoost,
        SpeedBoost,
        VitalityBoost,
        WisdomBoost,
        DexterityBoost,
        CharFame,
        NextClassQuestFame,
        LegendaryRank,
        SinkLevel,
        AltTexture,
        GuildName,
        GuildRank,
        Breath,
        HealthPotionStack,
        MagicPotionStack,
        Backpack_0,
        Backpack_1,
        Backpack_2,
        Backpack_3,
        Backpack_4,
        Backpack_5,
        Backpack_6,
        Backpack_7,
        HasBackpack,
        Texture,
        ItemData_0,
        ItemData_1,
        ItemData_2,
        ItemData_3,
        ItemData_4,
        ItemData_5,
        ItemData_6,
        ItemData_7,
        ItemData_8,
        ItemData_9,
        ItemData_10,
        ItemData_11,
        ItemData_12,
        ItemData_13,
        ItemData_14,
        ItemData_15,
        ItemData_16,
        ItemData_17,
        ItemData_18,
        ItemData_19
    }

    public class Entity : IDisposable
    {
        private const int MaxEfffects = 29;
        private const float MoveThreshold = 0.4f;

        public int Id;
        public ushort Type;
        public ObjectDesc Desc;
        public Position Position;
        public Chunk CurrentChunk;
        public World Parent;

        public BehaviorModel Behavior;
        public List<State> CurrentStates;
        public Dictionary<int, int> StateCooldown; //Used for cooldowns (could be merged with DynamicObjects but it's faster this way)
        public Dictionary<int, object> StateObject; //Used for things like WanderStates etc.
        public List<Position> History;
        public bool Dead;
        public bool Constant;
        public int? Lifetime;

        public int[] Effects;
        private ConditionEffects _conditionEffects;
        public ConditionEffects ConditionEffects
        {
            get { return _conditionEffects; }
            set { TrySetSV(StatType.Condition, (int)(_conditionEffects = value)); }
        }

        private int _hp;
        public int HP
        {
            get { return _hp; }
            set { TrySetSV(StatType.HP, _hp = value); }
        }

        private int _maxHp;
        public int MaxHP
        {
            get { return _maxHp; }
            set { TrySetSV(StatType.MaxHP, _maxHp = value); }
        }

        private int _size;
        public int Size
        {
            get { return _size; }
            set { TrySetSV(StatType.Size, _size = value); }
        }

        public int UpdateCount;
        public Dictionary<StatType, object> SVs;
        public Dictionary<StatType, object> NewSVs;

        public Entity(ushort type, int? lifetime = null)
        {
#if DEBUG
            if (this is StaticObject && lifetime != null)
                throw new Exception("Static objects must not have a lifetime");
#endif
            Type = type;
            Desc = Resources.Type2Object[type];
            SVs = new Dictionary<StatType, object>();
            NewSVs = new Dictionary<StatType, object>();
            Position = new Position(-1, -1);
            Effects = new int[MaxEfffects];

            HP = Desc.MaxHP;
            MaxHP = Desc.MaxHP;

            if (Desc.MinSize != 100 || Desc.MaxSize != 100)
                Size = MathUtils.NextInt(Desc.MinSize, Desc.MaxSize);

            Lifetime = lifetime;
            if (Lifetime != null)
                Constant = true;

#if DEBUG
            if (this is StaticObject && Lifetime != null)
            {
                throw new Exception("Statics should not define a lifetime");
            }
#endif
        }

        public bool TickEntity()
        {
            return Parent != null && !Dead;
        }

        public virtual bool HitByProjectile(Projectile projectile) 
        {
            return false;
        }

        public virtual void Init()
        {
            InitStates();

            if (this is Player || Behavior != null || Desc.Enemy)
                History = new List<Position>(Settings.TicksPerSecond * 10);
        }

        public virtual void Tick()
        {
            TickEffects();
            TickStates();

            if (History != null)
            {
                if (History.Count == History.Capacity)
                    History.RemoveAt(History.Capacity - 1);
                History.Insert(0, Position);
            }

            if (Lifetime != null)
            {
                Lifetime -= Settings.MillisecondsPerTick;
                if (Lifetime <= 0)
                {
                    OnLifeEnd();
                    Parent.RemoveEntity(this);
                }
            }
        }

        //Callback when Lifetime ends
        public virtual void OnLifeEnd()
        {

        }

        public Position TryGetHistory(int ticksBackwards)
        {
#if DEBUG
            if (History == null)
                throw new Exception("This entity does not support position history.");
#endif
            if (History.Count > ticksBackwards)
                return History[ticksBackwards];
            return Position;
        }

        public void RemoveConditionEffect(ConditionEffectIndex effect)
        {
            Effects[(int)effect] = 0;
        }

        public void ApplyConditionEffect(ConditionEffectIndex effect, int duration)
        {
#if DEBUG
            if (duration != -1 && (((float)duration / Settings.MillisecondsPerTick) != (duration / Settings.MillisecondsPerTick)))
                throw new Exception("Effect time out of sync with tick time.");
#endif
            if (effect == ConditionEffectIndex.Nothing || 
                (effect == ConditionEffectIndex.Stunned && HasConditionEffect(ConditionEffectIndex.StunImmune)))
                return;

            Effects[(int)effect] = duration;
        }

        public bool HasConditionEffect(ConditionEffectIndex effect)
        {
            return Effects[(int)effect] != 0;
        }

        public void TickEffects()
        {
            ConditionEffects newEffects = 0;
            for (int i = 0; i < Effects.Length; i++)
            {
                int time = Effects[i];

                //No effect to add
                if (time == 0)
                    continue;

                newEffects |= (ConditionEffects)((ulong)1 << (i - 1));

                if (time != -1) //Infinite duration
                {
                    Effects[i] -= Settings.MillisecondsPerTick;
#if DEBUG
                    if (Effects[i] < 0)
                        throw new Exception("Desynced effect duration");
#endif
                }
            }

            if (ConditionEffects != newEffects)
                ConditionEffects = newEffects;
        }

        public void ValidateAndMove(Position pos)
        {
#if DEBUG
            if (Parent == null)
                throw new Exception("World is undefined for this entity");
#endif
            pos = ResolveNewLocation(pos);
            Parent.MoveEntity(this, pos);
        }

        private Position ResolveNewLocation(Position pos)
        {
            if (HasConditionEffect(ConditionEffectIndex.Paralyzed))
                return pos;

            float dx = pos.X - Position.X;
            float dy = pos.Y - Position.Y;

            if (dx < MoveThreshold && 
                dx > -MoveThreshold && 
                dy < MoveThreshold &&
                dy > -MoveThreshold)
            {
                return CalcNewLocation(pos);
            }

            float ds = MoveThreshold / Math.Max(Math.Abs(dx), Math.Abs(dy));
            float tds = 0f;

            pos = Position;
            bool done = false;
            while (!done)
            {
                if (tds + ds >= 1)
                {
                    ds = 1 - tds;
                    done = true;
                }

                pos = CalcNewLocation(new Position(pos.X + dx * ds, pos.Y + dy * ds));
                tds = tds + ds;
            }

            return pos;
        }

        private Position CalcNewLocation(Position pos)
        {
            float fx = 0f;
            float fy = 0f;

            bool isFarX = (Position.X % .5f == 0 && pos.X != Position.X) || (int)(Position.X / .5f) != (int)(pos.X / .5f);
            bool isFarY = (Position.Y % .5f == 0 && pos.Y != Position.Y) || (int)(Position.Y / .5f) != (int)(pos.Y / .5f);

            if ((!isFarX && !isFarY) || RegionUnblocked(pos.X, pos.Y))
            {
                return pos;
            }

            if (isFarX)
            {
                fx = (pos.X > Position.X) ? (int)(pos.X * 2) / 2f : (int)(Position.X * 2) / 2f;
                if ((int)fx > (int)Position.X)
                    fx = fx - 0.01f;
            }

            if (isFarY)
            {
                fy = (pos.Y > Position.Y) ? (int)(pos.Y * 2) / 2f : (int)(Position.Y * 2) / 2f;
                if ((int)fy > (int)Position.Y)
                    fy = fy - 0.01f;
            }

            if (!isFarX)
            {
                pos.Y = fy;
                return pos;
            }

            if (!isFarY)
            {
                pos.X = fx;
                return pos;
            }

            float ax = (pos.X > Position.X) ? pos.X - fx : fx - pos.X;
            float ay = (pos.Y > Position.Y) ? pos.Y - fy : fy - pos.Y;
            if (ax > ay)
            {
                if (RegionUnblocked(pos.X, fy))
                {
                    pos.Y = fy;
                    return pos;
                }

                if (RegionUnblocked(fx, pos.Y))
                {
                    pos.X = fx;
                    return pos;
                }
            }
            else
            {
                if (RegionUnblocked(fx, pos.Y))
                {
                    pos.X = fx;
                    return pos;
                }
               
                if (RegionUnblocked(pos.X, fy))
                {
                    pos.Y = fy;
                    return pos;
                }
            }

            pos.X = fx;
            pos.Y = fy;
            return pos;
        }

        public bool RegionUnblocked(float x, float y)
        {
            if (TileOccupied(x, y))
                return false;

            float xFrac = x - (int)x;
            float yFrac = y - (int)y;

            if (xFrac < 0.5f)
            {
                if (TileFullOccupied(x - 1, y))
                    return false;

                if (yFrac < 0.5f)
                {
                    if (TileFullOccupied(x, y - 1) || TileFullOccupied(x - 1, y - 1))
                        return false;
                }
                else
                {
                    if (yFrac > 0.5f)
                        if (TileFullOccupied(x, y + 1) || TileFullOccupied(x - 1, y + 1))
                            return false;
                }

                return true;
            }

            if (xFrac > 0.5f)
            {
                if (TileFullOccupied(x + 1, y))
                    return false;

                if (yFrac < 0.5)
                {
                    if (TileFullOccupied(x, y - 1) || TileFullOccupied(x + 1, y - 1))
                        return false;
                }
                else
                {
                    if (yFrac > 0.5)
                        if (TileFullOccupied(x, y + 1) || TileFullOccupied(x + 1, y + 1))
                            return false;
                }

                return true;
            }

            if (yFrac < 0.5)
            {
                if (TileFullOccupied(x, y - 1))
                    return false;

                return true;
            }

            if (yFrac > 0.5)
                if (TileFullOccupied(x, y + 1))
                    return false;

            return true;
        }

        public bool TileOccupied(float x, float y)
        {
            Tile tile = Parent.GetTile((int)x, (int)y);
            if (tile == null)
                return true;

            TileDesc desc = Resources.Type2Tile[tile.Type];
            if (desc.NoWalk)
                return true;

            if (tile.StaticObject != null)
            {
                if (tile.StaticObject.Desc.EnemyOccupySquare)
                    return true;
            }

            return false;
        }

        public bool TileFullOccupied(float x, float y)
        {
            Tile tile = Parent.GetTile((int)x, (int)y);
            if (tile == null)
                return true;

            if (tile.StaticObject != null)
            {
                if (tile.StaticObject.Desc.FullOccupy)
                    return true;
            }

            return false;
        }

        public void InitStates()
        {
            Behavior = Manager.Behaviors.Resolve(Type);
            if (Behavior != null)
            {
#if DEBUG
                Program.Print(PrintType.Debug, $"Behavior resolved for <{Type}> <{Desc.DisplayId}>");
#endif
                StateCooldown = new Dictionary<int, int>();
                StateObject = new Dictionary<int, object>();
                CurrentStates = new List<State>();
                Dictionary<int, State> states = Behavior.States;
                while (states != null)
                {
                    if (states.Count > 0)
                    {
                        CurrentStates.Add(states.Values.First());
                        states = CurrentStates.Last().States;
                    }
                    else states = null;
                }

                foreach (State s in CurrentStates)
                {
                    foreach (Behavior b in s.Behaviors) b.Enter(this);
                    foreach (Transition t in s.Transitions) t.Enter(this);
                }

                foreach (Behavior b in Behavior.Behaviors)
                    b.Enter(this);
            }
        }

        public void TickStates()
        {
            if (Behavior != null)
            {
                //Don't tick behaviors if stasised.
                if (HasConditionEffect(ConditionEffectIndex.Stasis))
                    return;

                //Tick root behaviors
                foreach (Behavior behavior in Behavior.Behaviors)
                    behavior.Tick(this);

                //Loop through state behaviors
                for (int i = 0; i < CurrentStates.Count; i++)
                {
                    State state = CurrentStates[i];
                    foreach (Behavior behavior in state.Behaviors)
                        behavior.Tick(this);
                }

                //Loop through state transitions
                for (int i = 0; i < CurrentStates.Count; i++)
                {
                    State state = CurrentStates[i];
                    int targetState = -1;
                    foreach (Transition transition in state.Transitions)
                    {
                        if (transition.Tick(this))
                        {
                            targetState = transition.TargetState;
                            break;
                        }
                    }

                    //Switch state if needed
                    if (targetState != -1)
                    {

                        //Exit old behaviors/transitions
                        for (int k = i; k < CurrentStates.Count; k++) 
                        {
                            foreach (Behavior behavior in CurrentStates[k].Behaviors) behavior.Exit(this);
                            foreach (Transition transition in CurrentStates[k].Transitions) transition.Exit(this);
                        }

                        //Clear old substates
                        while (CurrentStates.Count > i)
                            CurrentStates.RemoveAt(i);

                        //Get new substates
                        int subIndex = i - 1;
                        Dictionary<int, State> states = i == 0 ? Behavior.States : CurrentStates[i - 1].States;
                        while (states != null)
                        {
                            subIndex++;
                            if (states.Count > 0)
                            {
                                if (subIndex == i) CurrentStates.Add(states[targetState]);
                                else CurrentStates.Add(states.Values.First());
                                states = CurrentStates.Last().States;
                            }
                            else states = null;
                        }

                        //Enter new behaviors/transitions
                        for (int k = i; k < CurrentStates.Count; k++)
                        {
                            foreach (Behavior behavior in CurrentStates[k].Behaviors) behavior.Enter(this);
                            foreach (Transition transition in CurrentStates[k].Transitions) transition.Enter(this);
                        }
                        break;
                    }
                }
            }
        }

        public void TrySetSV(StatType type, object value)
        {
            if (SVs.TryGetValue(type, out object current) && current.Equals(value)) //Don't send duplicate stats.
                return;

            SVs[type] = value;
            NewSVs[type] = value;
            UpdateCount++;
        }

        public void SetSV(StatType type, object value) //Can be useful if you need to resend the same stat value.
        {
            SVs[type] = value;
            NewSVs[type] = value;
            UpdateCount++;
        }

        public float GetHealthPercentage()
        {
            return (float)HP / MaxHP;
        }

        public virtual ObjectDefinition GetObjectDefinition()
        {
            return new ObjectDefinition
            {
                ObjectType = Type,
                ObjectStatus = GetObjectStatus(false)
            };
        }

        public virtual ObjectStatus GetObjectStatus(bool newTick)
        {
            return new ObjectStatus
            {
                Id = Id,
                Position = Position,
                Stats = newTick ? NewSVs : SVs
            };
        }

        public virtual ObjectDrop GetObjectDrop()
        {
            return new ObjectDrop
            {
                Id = Id,
                Explode = Dead
            };
        }

        public virtual void Dispose()
        {
            if (Behavior != null)
            {
                StateCooldown.Clear();
                StateObject.Clear();
                CurrentStates.Clear();
            }
            SVs.Clear();
            NewSVs.Clear();
            Parent = null;
        }

        public static Entity Resolve(ushort type)
        {
            ObjectDesc desc = Resources.Type2Object[type];

#if DEBUG
            if (desc.Player) 
                throw new Exception("Cannot dynamically resolve a player entity.");
#endif

            if (desc.ConnectedWall || desc.CaveWall) return new ConnectedObject(type);
            if (desc.Static) return new StaticObject(type);
            if (desc.Enemy) return new Enemy(type);
            return new Entity(type);
        }

        public override bool Equals(object obj)
        {
#if DEBUG
            if (obj == null || !(obj is Entity))
                throw new Exception("Invalid object comparison.");
#endif
            return Id == (obj as Entity).Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return $"<{Desc.DisplayId}> <{Parent.Name}:{Parent.Id}> <{Position.ToIntPoint()}>";
        }
    }
}
