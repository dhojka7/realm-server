using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RotMG.Game.Entities
{
    public class Decoy : Entity
    {
        private const int DecoyMoveTime = 1600;

        public int Duration;
        public Position Direction;

        public Decoy(Player player, float angle, int duration) : base(0x0715, duration)
        {
#if DEBUG
            if (duration < DecoyMoveTime)
            {
                throw new Exception("Nope.");
            }
#endif
            Duration = duration;
            Direction = (new Position(
                MathF.Cos(angle),
                MathF.Sin(angle)) / Settings.TicksPerSecond) * 5;

            if (player.Tex1 != 0)
                SetSV(StatType.Tex1, player.Tex1);
            if (player.Tex2 != 0)
                SetSV(StatType.Tex2, player.Tex2);
        }

        public override void Tick()
        {
            int elapsed = Duration - Lifetime.Value;
            if (elapsed <= DecoyMoveTime)
                ValidateAndMove(Position + Direction);

            base.Tick();
        }

        public override void OnLifeEnd()
        {
            byte[] nova = GameServer.ShowEffect(
                ShowEffectIndex.Nova,
                Id,
                0xffff0000,
                new Position(1, 0));

            foreach (Player player in Parent.Players.Values)
            {
                if (player.Client.Account.Effects)
                    player.Client.Send(nova);
            }
        }
    }
}
