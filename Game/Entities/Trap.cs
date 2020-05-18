using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RotMG.Game.Entities
{
    public class Trap : Entity
    {
        public Player Player;
        public float Radius;
        public int Damage;
        public ConditionEffectDesc[] CondEffects;

        public Trap(Player player, float radius, int damage, ConditionEffectDesc[] effects) : base(0x070f, 10000)
        {
            Player = player;
            Radius = radius;
            Damage = damage;
            CondEffects = effects;
        }

        public override void Tick()
        {
            if (Player.Parent == null)
            {
                Parent.RemoveEntity(this);
                return;
            }

            int elapsed = 10000 - Lifetime.Value;
            if (elapsed % 1000 == 0)
            {
                byte[] ring = GameServer.ShowEffect(ShowEffectIndex.Ring,
                    Id, 0xff9000ff, new Position(Radius / 2, 0));
                foreach (Entity j in Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                    if (j is Player k && (k.Client.Account.Effects || k.Equals(Player)))
                        k.Client.Send(ring);
            }

            if (this.GetNearestEnemy(Radius) != null)
            {
                OnLifeEnd();
                Parent.RemoveEntity(this);
                return;
            }

            base.Tick();
        }

        public override void OnLifeEnd()
        {
            byte[] nova = GameServer.ShowEffect(ShowEffectIndex.Nova, Id, 0xff9000ff, new Position(Radius, 0));

            foreach (Entity j in Parent.EntityChunks.HitTest(Position, Radius))
                if (j is Enemy k && 
                    !k.HasConditionEffect(ConditionEffectIndex.Invincible) && 
                    !k.HasConditionEffect(ConditionEffectIndex.Stasis))
                    k.Damage(Player, Damage, CondEffects, false, true);

            foreach (Entity j in Parent.PlayerChunks.HitTest(Position, Player.SightRadius))
                if (j is Player k && (k.Client.Account.Effects || k.Equals(Player)))
                    k.Client.Send(nova);
        }
    }
}
