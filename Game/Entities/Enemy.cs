using RotMG.Common;
using RotMG.Game.Logic;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Entities
{
    public class Enemy : Entity
    {
        public Dictionary<int, int> DamageStorage;

        public Enemy(ushort type) : base(type)
        {
            DamageStorage = new Dictionary<int, int>();
        }

        public void ApplyPoison(Player hitter, ConditionEffectDesc[] effects, int damage, int damageLeft)
        {
            if (HasConditionEffect(ConditionEffectIndex.Invincible) || 
                HasConditionEffect(ConditionEffectIndex.Stasis))
                return;

            byte[] poison = GameServer.ShowEffect(ShowEffectIndex.Poison, Id, 0xffddff00);
            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                if (j is Player k && k.Client.Account.Effects)
                    k.Client.Send(poison);

            Damage(hitter, damage, effects, true, true);
            if (damageLeft <= 0) return;
            Manager.AddTimedAction(1000, () =>
            {
                damageLeft -= damage;
                if (damageLeft < 0)
                    damage = Math.Abs(damageLeft);

                if (hitter.Parent != null && Parent != null) //These have to be here in case enemy dies before poison is applied
                    ApplyPoison(hitter, effects, damage, damageLeft);
            });
        }

        public void Death(Player killer)
        {
#if DEBUG
            if (killer == null)
                throw new Exception("Undefined killer");
#endif

            int baseExp = (int)Math.Ceiling(MaxHP / 10f);
            if (baseExp != 0)
            {
                List<Entity> l;
                foreach (Entity en in l = Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                {
                    if (!(en is Player player)) 
                        continue;
                    int exp = baseExp;
                    if (exp > Player.GetNextLevelEXP(player.Level) / 10)
                        exp = Player.GetNextLevelEXP(player.Level) / 10;
                    if (player.GainEXP(exp))
                        foreach (Entity p in l)
                            if (!p.Equals(player)) 
                                (p as Player).FameStats.LevelUpAssists++;
                }
            }

            int position = 0;
            foreach (KeyValuePair<int, int> i in DamageStorage.OrderByDescending(k => k.Value))
            {
                Entity en = Parent.GetEntity(i.Key);
                if (en == null) continue;
                Player player = en as Player;

                if (Desc.Quest)
                {
                    player.HealthPotions = Math.Min(Player.MaxPotions, player.HealthPotions + 1);
                    player.MagicPotions = Math.Min(Player.MaxPotions, player.MagicPotions + 1);
                }
                else
                {
                    if (MathUtils.Chance(.05f))
                        player.HealthPotions = Math.Min(Player.MaxPotions, player.HealthPotions + 1);
                    if (MathUtils.Chance(.05f))
                        player.MagicPotions = Math.Min(Player.MaxPotions, player.MagicPotions + 1);
                }

                if (!player.Equals(killer))
                {
                    player.FameStats.MonsterAssists++;
                    if (Desc.Cube) player.FameStats.CubeAssists++;
                    if (Desc.Oryx) player.FameStats.OryxAssists++;
                    if (Desc.God) player.FameStats.GodAssists++;
                }

                if (Behavior != null && Behavior.Loots.Count > 0)
                {
                    List<int> items = new List<int>();
                    foreach (Loot l in Behavior.Loots)
                    {
                        float t = Math.Min(1f, i.Value / MaxHP);
                        int j = l.TryObtainItem(this, player, position, t);
                        if (j != -1) items.Add(j);
                        if (items.Count == Container.MaxSlots) break;
                    }

                    if (items.Count > 0)
                    {
                        int bagType = 1;
                        for (int k = 0; k < items.Count; k++)
                        {
                            ItemDesc d = Resources.Type2Item[(ushort)items[k]];
                            if (d.BagType > bagType)
                                bagType = d.BagType;
                        }

                        if (bagType == 2) player.FameStats.CyanBags++;
                        else if (bagType == 3) player.FameStats.BlueBags++;
                        else if (bagType == 4) player.FameStats.WhiteBags++;

                        Container c = new Container(Container.FromBagType(bagType), player.Id, 40000 * bagType);
                        for (int k = 0; k < items.Count; k++)
                        {
                            Tuple<bool, ItemData> roll = Resources.Type2Item[(ushort)items[k]].Roll();
                            c.Inventory[k] = items[k];
                            c.ItemDatas[k] = roll.Item1 ? (int)roll.Item2 : -1;
                            c.UpdateInventorySlot(k);
                        }
                        Parent.AddEntity(c, Position + MathUtils.Position(0.2f, 0.2f));
                    }
                }

                position++;
            }

            killer.FameStats.MonsterKills++;
            if (Desc.Cube) killer.FameStats.CubeKills++;
            if (Desc.Oryx) killer.FameStats.OryxKills++;
            if (Desc.God) killer.FameStats.GodKills++;

            if (Behavior != null)
            {
                foreach (Behavior b in Behavior.Behaviors)
                    b.Death(this);
                foreach (State s in CurrentStates)
                    foreach (Behavior b in s.Behaviors)
                        b.Death(this);
            }

            Dead = true;
            Parent.RemoveEntity(this);
        }

        public bool Damage(Player hitter, int damage, ConditionEffectDesc[] effects, bool pierces, bool showToHitter = false)
        {
#if DEBUG
            if (HasConditionEffect(ConditionEffectIndex.Invincible))
                throw new Exception("Entity should not be damaged if invincible");
            if (HasConditionEffect(ConditionEffectIndex.Stasis))
                throw new Exception("Entity should not be damaged if stasised");
            if (effects == null)
                throw new Exception("Null effects");
            if (hitter == null)
                throw new Exception("Undefined hitter");
#endif


            foreach (ConditionEffectDesc eff in effects)
                ApplyConditionEffect(eff.Effect, eff.DurationMS);

            if (HasConditionEffect(ConditionEffectIndex.ArmorBroken))
                pierces = true;

            int damageWithDefense = this.GetDefenseDamage(damage, Desc.Defense, pierces);

            if (HasConditionEffect(ConditionEffectIndex.Invulnerable))
                damageWithDefense = 0;

            HP -= damageWithDefense;

            if (DamageStorage.ContainsKey(hitter.Id))
                DamageStorage[hitter.Id] += damageWithDefense;
            else DamageStorage.Add(hitter.Id, damageWithDefense);

            hitter.FameStats.DamageDealt += damageWithDefense;

            byte[] packet = GameServer.Damage(Id, new ConditionEffectIndex[0], damageWithDefense);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                if (en is Player player && player.Client.Account.AllyDamage && !player.Equals(hitter))
                    player.Client.Send(packet);

            if (showToHitter)
                hitter.Client.Send(packet);

            if (HP <= 0)
            {
                Death(hitter);
                return true;
            }
            return false;
        }

        public override bool HitByProjectile(Projectile projectile)
        {
#if DEBUG
            if (projectile.Owner == null || !(projectile.Owner is Player))
                throw new Exception("Projectile owner is not player");
#endif
            (projectile.Owner as Player).FameStats.ShotsThatDamage++;
            return Damage(projectile.Owner as Player, projectile.Damage, projectile.Desc.Effects, projectile.Desc.ArmorPiercing);
        }

        public override void Dispose()
        {
            DamageStorage.Clear();
            base.Dispose();
        }
    }
}
