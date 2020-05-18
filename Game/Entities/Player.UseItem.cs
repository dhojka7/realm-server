using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml.Schema;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        public const float UseCooldownThreshold = 1.1f;
        public const int MaxAbilityDist = 14;

        public Queue<ushort> ShootAEs;
        public int UseDuration;
        public int UseTime;

        public void TryUseItem(int time, SlotData slot, Position target)
        {
            if (!ValidTime(time))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid time useitem");
#endif
                Client.Disconnect();
                return;
            }

            if (slot.SlotId == HealthPotionSlotId)
            {
                if (HealthPotions > 0 && !HasConditionEffect(ConditionEffectIndex.Sick))
                {
                    Heal(100, false);
                    HealthPotions--;
                }
                return;
            }
            else if (slot.SlotId == MagicPotionSlotId)
            {
                if (MagicPotions > 0 && !HasConditionEffect(ConditionEffectIndex.Quiet))
                {
                    Heal(100, true);
                    MagicPotions--;
                }
                return;
            }

            Entity en = Parent.GetEntity(slot.ObjectId);
            if (slot.SlotId != 1)
                (en as IContainer)?.UpdateInventorySlot(slot.SlotId);
            if (en == null || !(en is IContainer))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined entity");
#endif
                return;
            }

            if (en is Player && !en.Equals(this))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Trying to use items from another players inventory");
#endif
                return;
            }

            if (en is Container c)
            {
                if ((en as Container).OwnerId != -1 && (en as Container).OwnerId != Id)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Trying to use items from another players container/bag");
#endif
                    return;
                }

                if (en.Position.Distance(this) > ContainerMinimumDistance)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Too far away from container");
#endif
                    return;
                }
            }

            IContainer con = en as IContainer;
            ItemDesc desc = null;
            if (con.Inventory[slot.SlotId] != -1)
                desc = Resources.Type2Item[(ushort)con.Inventory[slot.SlotId]];

            if (desc == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid use item");
#endif
                return;
            }

            bool isAbility = slot.SlotId == 1;
            if (isAbility)
            {
                if (slot.ObjectId != Id)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Trying to use ability from a container?");
#endif
                    return;
                }

                if (UseTime + (UseDuration * (1f / UseCooldownThreshold)) > time)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Used ability too soon");
#endif
                    return;
                }

                if (MP - desc.MpCost < 0)
                {
#if DEBUG
                    Program.Print(PrintType.Error, "Not enough MP");
#endif
                    return;
                }
            }

            bool inRange = Position.Distance(target) <= MaxAbilityDist && Parent.GetTileF(target.X, target.Y) != null;
            Action callback = null;
            foreach (ActivateEffectDesc eff in desc.ActivateEffects)
            {
                switch (eff.Index)
                {
                    case ActivateEffectIndex.Shuriken: //Could be optimized too, it's not great..
                        {
                            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, Id, 0xffeba134, new Position(2.5f, 0));

                            foreach (Entity j in Parent.EntityChunks.HitTest(Position, 2.5f))
                            {
                                if (j is Enemy k && 
                                    !k.HasConditionEffect(ConditionEffectIndex.Invincible) && 
                                    !k.HasConditionEffect(ConditionEffectIndex.Stasis))
                                {
                                    k.ApplyConditionEffect(ConditionEffectIndex.Dazed, 1000);
                                }
                            }

                            List<byte[]> stars = new List<byte[]>();
                            HashSet<Entity> seeked = new HashSet<Entity>();
                            int startId = NextAEProjectileId;
                            NextAEProjectileId += eff.Amount;

                            float angle = Position.Angle(target);
                            float cone = MathF.PI / 8;
                            for (int i = 0; i < eff.Amount; i++)
                            {
                                Entity t = this.GetNearestEnemy(8, angle, cone, target, seeked) ?? this.GetNearestEnemy(6, seeked);
                                if (t != null) seeked.Add(t);
                                int d = GetNextDamage(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[slot.SlotId]);
                                float a = t == null ? MathUtils.NextAngle() : Position.Angle(t.Position);
                                List<Projectile> p = new List<Projectile>()
                                {
                                     new Projectile(this, desc.Projectile, startId + i, time, a, Position, d)
                                };

                                stars.Add(GameServer.ServerPlayerShoot(startId + i, Id, desc.Type, Position, a, 0, p));
                                AwaitProjectiles(p);
                            }

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                            {
                                if (j is Player k)
                                {
                                    if (k.Client.Account.Effects || k.Equals(this))
                                        k.Client.Send(nova);
                                    if (k.Client.Account.AllyShots || k.Equals(this))
                                        foreach (byte[] s in stars)
                                            k.Client.Send(s);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.VampireBlast: //Maybe optimize this...?
                        if (inRange)
                        {
                            byte[] line = GameServer.ShowEffect(ShowEffectIndex.Line, Id, 0xFFFF0000 , target);
                            byte[] burst = GameServer.ShowEffect(ShowEffectIndex.Burst, Id, 0xFFFF0000, target, new Position(target.X + eff.Radius, target.Y));
                            int lifeSucked = 0;

                            List<Entity> enemies = new List<Entity>();
                            List<Entity> players = new List<Entity>();
                            List<byte[]> flows = new List<byte[]>();

                            foreach (Entity j in Parent.EntityChunks.HitTest(target, eff.Radius))
                            {
                                if (j is Enemy k && 
                                    !k.HasConditionEffect(ConditionEffectIndex.Invincible) && 
                                    !k.HasConditionEffect(ConditionEffectIndex.Stasis))
                                {
                                    k.Damage(this, eff.TotalDamage, eff.Effects, true, true);
                                    lifeSucked += eff.TotalDamage;
                                    enemies.Add(k);
                                }
                            }

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, eff.Radius))
                            {
                                if (j is Player k)
                                {
                                    players.Add(k);
                                    k.Heal(lifeSucked, false);
                                }
                            }

                            if (enemies.Count > 0)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    Entity a = enemies[MathUtils.Next(enemies.Count)];
                                    Entity b = players[MathUtils.Next(players.Count)];
                                    flows.Add(GameServer.ShowEffect(ShowEffectIndex.Flow, b.Id, 0xffffffff, a.Position));
                                }
                            }

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                            {
                                if (j is Player k)
                                {
                                    if (k.Client.Account.Effects)
                                    {
                                        k.Client.Send(line);
                                        foreach (byte[] p in flows)
                                            k.Client.Send(p);
                                    }

                                    if (k.Client.Account.Effects || k.Equals(this))
                                        k.Client.Send(burst);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.StasisBlast:
                        if (inRange)
                        {
                            byte[] blast = GameServer.ShowEffect(ShowEffectIndex.Collapse, Id, 0xffffffff, 
                                target, 
                                new Position(target.X + 3, target.Y));
                            List<byte[]> notifications = new List<byte[]>();

                            foreach (Entity j in Parent.EntityChunks.HitTest(target, 3))
                            {
                                if (j is Enemy k)
                                {
                                    if (k.HasConditionEffect(ConditionEffectIndex.StasisImmune))
                                    {
                                        notifications.Add(GameServer.Notification(k.Id, "Immune", 0xff00ff00));
                                        continue;
                                    }

                                    if (k.HasConditionEffect(ConditionEffectIndex.Stasis))
                                        continue;

                                    notifications.Add(GameServer.Notification(k.Id, "Stasis", 0xffff0000));
                                    k.ApplyConditionEffect(ConditionEffectIndex.Stasis, eff.DurationMS);
                                    k.ApplyConditionEffect(ConditionEffectIndex.StasisImmune, eff.DurationMS + 3000);
                                }
                            }

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                            {
                                if (j is Player k)
                                {
                                    if (k.Client.Account.Effects || k.Equals(this))
                                        k.Client.Send(blast);
                                    if (k.Client.Account.Notifications || k.Equals(this))
                                        foreach (byte[] n in notifications)
                                            k.Client.Send(n);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.Trap:
                        if (inRange)
                        {
                            byte[] @throw = GameServer.ShowEffect(ShowEffectIndex.Throw, Id, 0xff9000ff, target);
                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                if (j is Player k && (k.Client.Account.Effects || k.Equals(this)))
                                    k.Client.Send(@throw);

                            Manager.AddTimedAction(1500, () =>
                            {
                                if (Parent != null)
                                {
                                    Parent.AddEntity(new Trap(this, eff.Radius, eff.TotalDamage, eff.Effects), target);
                                }
                            });
                        }
                        break;
                    case ActivateEffectIndex.Lightning:
                        {
                            float angle = Position.Angle(target);
                            float cone = MathF.PI / 4;
                            Entity start = this.GetNearestEnemy(MaxAbilityDist, angle, cone, target);

                            if (start == null)
                            {
                                float[] angles = new float[3] { angle, angle - cone, angle + cone };
                                byte[][] lines = new byte[3][];
                                for (int i = 0; i < 3; i++)
                                {
                                    float x = (int)(MaxAbilityDist * MathF.Cos(angles[i])) + Position.X;
                                    float y = (int)(MaxAbilityDist * MathF.Sin(angles[i])) + Position.Y;
                                    lines[i] = GameServer.ShowEffect(ShowEffectIndex.Line, Id, 0xffff0088, new Position(x, y), new Position(350, 0));
                                }

                                foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                {
                                    if (j is Player k && k.Client.Account.Effects)
                                    {
                                        k.Client.Send(lines[0]);
                                        k.Client.Send(lines[1]);
                                        k.Client.Send(lines[2]);
                                    }
                                }
                            }
                            else
                            {
                                Entity prev = this;
                                Entity current = start;
                                HashSet<Entity> targets = new HashSet<Entity>();
                                List<byte[]> pkts = new List<byte[]>();
                                targets.Add(current);
                                (current as Enemy).Damage(this, eff.TotalDamage, eff.Effects, false, true);
                                for (int i = 1; i < eff.MaxTargets + 1; i++)
                                {
                                    pkts.Add(GameServer.ShowEffect(ShowEffectIndex.Lightning, prev.Id, 0xffff0088,
                                        new Position(current.Position.X, current.Position.Y),
                                        new Position(350, 0)));

                                    if (i == eff.MaxTargets) 
                                        break;

                                    Entity next = current.GetNearestEnemy(10, targets);
                                    if (next == null)
                                        break;

                                    targets.Add(next);
                                    (next as Enemy).Damage(this, eff.TotalDamage, eff.Effects, false, true);
                                    prev = current;
                                    current = next;
                                }

                                foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                    if (j is Player k && k.Client.Account.Effects)
                                        foreach (byte[] p in pkts)
                                        {
                                            Console.WriteLine(p.Length);
                                            k.Client.Send(p);
                                        }
                            }
                        }
                        break;
                    case ActivateEffectIndex.PoisonGrenade:
                        if (inRange)
                        {
                            Placeholder placeholder = new Placeholder();
                            Parent.AddEntity(placeholder, target);

                            byte[] @throw = GameServer.ShowEffect(ShowEffectIndex.Throw, Id, 0xffddff00, target);
                            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, placeholder.Id, 0xffddff00, new Position(eff.Radius, 0));

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                if (j is Player k && (k.Client.Account.Effects || k.Equals(this)))
                                    k.Client.Send(@throw);

                            Manager.AddTimedAction(1500, () =>
                            {
                                if (placeholder.Parent != null)
                                {
                                    if (Parent != null)
                                    {
                                        foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                            if (j is Player k && (k.Client.Account.Effects || k.Equals(this)))
                                                k.Client.Send(nova);
                                        foreach (Entity j in Parent.EntityChunks.HitTest(placeholder.Position, eff.Radius))
                                            if (j is Enemy e)
                                                e.ApplyPoison(this, new ConditionEffectDesc[0], (int)(eff.TotalDamage / (eff.DurationMS / 1000f)), eff.TotalDamage);
                                    }
                                    placeholder.Parent.RemoveEntity(placeholder);
                                }
                            });
                        }
                        break;
                    case ActivateEffectIndex.HealNova:
                        {
                            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, Id, 0xffffffff, new Position(eff.Range, 0));
                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, Math.Max(eff.Range, SightRadius)))
                            {
                                if (j is Player k)
                                {
                                    if (Position.Distance(j) <= eff.Range)
                                        k.Heal(eff.Amount, false);
                                    if (k.Client.Account.Effects || k.Equals(this))
                                        k.Client.Send(nova);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.ConditionEffectAura:
                        {
                            uint color = eff.Effect == ConditionEffectIndex.Damaging ? 0xffff0000 : 0xffffffff;
                            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, Id, color, new Position(eff.Range, 0));
                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, Math.Max(eff.Range, SightRadius)))
                            {
                                if (j is Player k)
                                {
                                    if (Position.Distance(j) <= eff.Range)
                                        k.ApplyConditionEffect(eff.Effect, eff.DurationMS);
                                    if (k.Client.Account.Effects || k.Equals(this))
                                        k.Client.Send(nova);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.ConditionEffectSelf:
                        {
                            ApplyConditionEffect(eff.Effect, eff.DurationMS);

                            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, Id, 0xffffffff, new Position(1, 0));
                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                                if (j is Player k && k.Client.Account.Effects)
                                    k.Client.Send(nova);
                        }
                        break;
                    case ActivateEffectIndex.Dye:
                        if (desc.Tex1 != 0)
                            Tex1 = desc.Tex1;
                        if (desc.Tex2 != 0)
                            Tex2 = desc.Tex2;
                        break;
                    case ActivateEffectIndex.Shoot:
                        if (!HasConditionEffect(ConditionEffectIndex.Stunned))
                            ShootAEs.Enqueue(desc.Type);
                        break;
                    case ActivateEffectIndex.Teleport:
                        if (inRange)
                            Teleport(time, target);
                        break;
                    case ActivateEffectIndex.Decoy:
                        Parent.AddEntity(new Decoy(this, Position.Angle(target), eff.DurationMS), Position);
                        break;
                    case ActivateEffectIndex.BulletNova:
                        if (inRange)
                        {
                            List<Projectile> projs = new List<Projectile>(20);
                            int novaCount = 20;
                            int startId = NextAEProjectileId;
                            float angleInc = (MathF.PI * 2) / novaCount;
                            NextAEProjectileId += novaCount;
                            for (int i = 0; i < novaCount; i++)
                            {
                                int d = GetNextDamage(desc.Projectile.MinDamage, desc.Projectile.MaxDamage, ItemDatas[slot.SlotId]);
                                Projectile p = new Projectile(this, desc.Projectile, startId + i, time, angleInc * i, target, d);
                                projs.Add(p);
                            }

                            AwaitProjectiles(projs);

                            byte[] line = GameServer.ShowEffect(ShowEffectIndex.Line, Id, 0xFFFF00AA, target);
                            byte[] nova = GameServer.ServerPlayerShoot(startId, Id, desc.Type, target, 0, angleInc, projs);

                            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, SightRadius))
                            {
                                if (j is Player k)
                                {
                                    if (k.Client.Account.Effects)
                                        k.Client.Send(line);
                                    if (k.Client.Account.AllyShots || k.Equals(this))
                                        k.Client.Send(nova);
                                }
                            }
                        }
                        break;
                    case ActivateEffectIndex.Backpack:
                        if (HasBackpack)
                            callback = () =>
                            {
                                con.Inventory[slot.SlotId] = desc.Type;
                                con.UpdateInventorySlot(slot.SlotId);
                                SendError("You already have a backpack.");
                            };
                        else
                        {
                            HasBackpack = true;
                            SendInfo("8 more spaces. Woohoo!");
                        }
                        break;
#if DEBUG
                    default:
                        Program.Print(PrintType.Error, $"Unhandled AE <{eff.Index.ToString()}>");
                        break;
#endif
                }
            }

            if (isAbility)
            {
                MP -= desc.MpCost;
                UseTime = time;
                float cooldownMod = ItemDesc.GetStat(ItemDatas[1], ItemData.Cooldown, ItemDesc.CooldownMultiplier);
                int cooldown = desc.CooldownMS;
                cooldown = cooldown + ((int)(cooldown * -cooldownMod));
                UseDuration = cooldown;
                FameStats.AbilitiesUsed++;
            }

            if (desc.Potion)
                FameStats.PotionsDrank++;

            if (desc.Consumable)
            {
                con.Inventory[slot.SlotId] = -1;
                con.UpdateInventorySlot(slot.SlotId);
            }

            callback?.Invoke();
        }
    }
}
