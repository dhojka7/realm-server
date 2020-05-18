using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RotMG.Game.Entities
{
    public partial class Player : Entity, IContainer
    {
        private const int MaxLatencyMS = 2000;
        public const int MaxPotions = 6;

        public static int[] Stars = 
        {
            20,
            150,
            400,
            800,
            2000
        };

        public Client Client;

        private int _accountId;
        public int AccountId
        {
            get { return _accountId; }
            set { TrySetSV(StatType.AccountId, _accountId = value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { TrySetSV(StatType.Name, _name = value); }
        }

        private int _exp;
        public int EXP
        {
            get { return _exp; }
            set { TrySetSV(StatType.EXP, _exp = value); }
        }

        private int _nextLevelExp;
        public int NextLevelEXP
        {
            get { return _nextLevelExp; }
            set { SetPrivateSV(StatType.NextLevelEXP, _nextLevelExp = value); }
        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set { TrySetSV(StatType.Level, _level = value); }
        }

        private int _charFame;
        public int CharFame
        {
            get { return _charFame; }
            set { TrySetSV(StatType.CharFame, _charFame = value); }
        }

        private int _fame;
        public int Fame
        {
            get { return _fame; }
            set { SetPrivateSV(StatType.Fame, _fame = value); }
        }

        private int _nextClassQuestFame;
        public int NextClassQuestFame
        {
            get { return _nextClassQuestFame; }
            set { SetPrivateSV(StatType.NextClassQuestFame, _nextClassQuestFame = value); }
        }

        private int _numStars;
        public int NumStars
        {
            get { return _numStars; }
            set { TrySetSV(StatType.NumStars, _numStars = value); }
        }

        private string _guildName;
        public string GuildName
        {
            get { return _guildName; }
            set { TrySetSV(StatType.GuildName, _guildName = value); }
        }

        private int _guildRank;
        public int GuildRank
        {
            get { return _guildRank; }
            set { TrySetSV(StatType.GuildRank, _guildRank = value); }
        }

        private int _credits;
        public int Credits
        {
            get { return _credits; }
            set { SetPrivateSV(StatType.Credits, _credits = value); }
        }

        private int _tex1;
        public int Tex1
        {
            get { return _tex1; }
            set { TrySetSV(StatType.Tex1, _tex1 = value); }
        }

        private int _tex2;
        public int Tex2
        {
            get { return _tex2; }
            set { TrySetSV(StatType.Tex2, _tex2 = value); }
        }

        private int _skinType;
        public int SkinType
        {
            get { return _skinType; }
            set { TrySetSV(StatType.Texture, _skinType = value); }
        }

        private bool _hasBackpack;
        public bool HasBackpack
        {
            get { return _hasBackpack; }
            set { SetPrivateSV(StatType.HasBackpack, (_hasBackpack = value).GetHashCode()); }
        }

        private int _mp;
        public int MP
        {
            get { return _mp; }
            set { TrySetSV(StatType.MP, _mp = value); }
        }

        private int _maxMp;
        public int MaxMP
        {
            get { return _maxMp; }
            set { TrySetSV(StatType.MaxMP, _maxMp = value); }
        }

        private int _oxygen;
        public int Oxygen
        {
            get { return _oxygen; }
            set { SetPrivateSV(StatType.Breath, _oxygen = value); }
        }

        private int _healthPotions;
        public int HealthPotions
        {
            get { return _healthPotions; }
            set { SetPrivateSV(StatType.HealthPotionStack, _healthPotions = value); }
        }

        private int _magicPotions;
        public int MagicPotions
        {
            get { return _magicPotions; }
            set { SetPrivateSV(StatType.MagicPotionStack, _magicPotions = value); }
        }

        private int _sinkLevel;
        public int SinkLevel
        {
            get { return _sinkLevel; }
            set { TrySetSV(StatType.SinkLevel, _sinkLevel = value); }
        }

        public FameStatsInfo FameStats;

        public Player(Client client) : base((ushort)client.Character.ClassType)
        {
            PrivateSVs = new Dictionary<StatType, object>();

            Client = client;
            HP = client.Character.HP;
            MP = client.Character.MP;
            AccountId = client.Account.Id;
            Name = client.Account.Name;
            Level = client.Character.Level;

            if (client.Character.HealthPotions != 0) HealthPotions = client.Character.HealthPotions;
            if (client.Character.MagicPotions != 0) MagicPotions = client.Character.MagicPotions;
            if (client.Character.HasBackpack) HasBackpack = client.Character.HasBackpack;
            if (client.Character.SkinType != 0) SkinType = client.Character.SkinType;
            if (client.Character.Tex1 != 0) Tex1 = client.Character.Tex1;
            if (client.Character.Tex2 != 0) Tex2 = client.Character.Tex2;
            if (client.Account.Stats.Credits != 0) Credits = client.Account.Stats.Credits;
            if (client.Account.Stats.Fame != 0) Fame = client.Account.Stats.Fame;

            if (!string.IsNullOrWhiteSpace(client.Account.GuildName))
            {
                GuildName = client.Account.GuildName;
                GuildRank = client.Account.GuildRank;
            }

            int stars = Database.GetStars(client.Account);
            if (stars != 0) NumStars = stars;

            if (Database.IsLegend(client.Account.Id))
                SetSV(StatType.LegendaryRank, 0);

            FameStats = client.Character.FameStats;
            InitInventory(client.Character);
            InitStats(client.Character);
            InitLevel(client.Character);

            RecalculateEquipBonuses();
        }

        public void SaveToCharacter()
        {
            Client.Character.HP = HP;
            Client.Character.MP = MP;
            Client.Character.Level = Level;
            Client.Character.HealthPotions = HealthPotions;
            Client.Character.MagicPotions = MagicPotions;
            Client.Character.HasBackpack = HasBackpack;
            Client.Character.SkinType = SkinType;
            Client.Character.Tex1 = Tex1;
            Client.Character.Tex2 = Tex2;
            Client.Character.Experience = EXP;
            Client.Character.Fame = CharFame;
            Client.Character.Inventory = Inventory.ToArray();
            Client.Character.ItemDatas = ItemDatas.ToArray();
            Client.Character.Stats = Stats.ToArray();
        }

        public override void Init()
        {
            TileUpdates = new int[Parent.Width, Parent.Height];
            EntityUpdates = new Dictionary<int, int>();
            Entities = new HashSet<Entity>();
            CalculatedSightCircle = new HashSet<IntPoint>();
            AwaitingProjectiles = new Queue<List<Projectile>>();
            AckedProjectiles = new Dictionary<int, ProjectileAck>();
            ShotProjectiles = new Dictionary<int, Projectile>();
            AwaitingAoes = new Queue<AoeAck>();
            ShootAEs = new Queue<ushort>();
            AwaitingGoto = new Queue<int>();

            Client.Send(GameServer.AccountList(0, Client.Account.LockedIds));
            Client.Send(GameServer.AccountList(1, Client.Account.IgnoredIds));

            ApplyConditionEffect(ConditionEffectIndex.Invulnerable, 3000);
            ApplyConditionEffect(ConditionEffectIndex.Invisible, 3000);
        }

        public void Heal(int amount, bool magic)
        {
            int heal = 0;
            if (magic)
            {
                int mp = MP;
                MP = Math.Max(1, Math.Min(GetStat(0), MP + amount));
                heal = MP - mp;
            }
            else
            {
                int hp = HP;
                HP = Math.Max(0, Math.Min(GetStat(0), HP + amount));
                heal = HP - hp;
            }

            if (heal <= 0) 
                return;

            byte[] notification = GameServer.Notification(Id, $"+{heal}", magic ? 0xff6084e0 : 0xff00ff00);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
            {
                if (en is Player player && 
                    (player.Client.Account.Notifications || player.Equals(this)))
                {
                    player.Client.Send(notification);
                }
            }
        }

        public void Death(string killer)
        {
#if DEBUG
            if (Parent.Name.Equals("Dreamland"))
                return;
#endif
            if (Dead) 
                return;

            Client.Active = false;
            Dead = true;

            SaveToCharacter();
            Database.Death(killer, Client.Account, Client.Character);

            byte[] death = GameServer.Death(Client.Account.Id, Client.Character.Id, killer);
            Client.Send(death);

            byte[] text = GameServer.Text("", 0, -1, 0, "", $"{Name} died at level {Level} killed by {killer}!");
            byte[] sound = GameServer.PlaySound("quack");

            foreach (Player p in Parent.Players.Values)
            {
                p.Client.Send(text);
                if (p.Client.Account.Sounds)
                    p.Client.Send(sound);
            }

            ushort type;
            int time;
            switch (GetMaxedStats())
            {
                case 8: type = 0x0735; time = 600000; break;
                case 7: type = 0x0734; time = 600000; break;
                case 6: type = 0x072b; time = 600000; break;
                case 5: type = 0x072a; time = 600000; break;
                case 4: type = 0x0729; time = 600000; break;
                case 3: type = 0x0728; time = 600000; break;
                case 2: type = 0x0727; time = 600000; break;
                case 1: type = 0x0726; time = 600000; break;
                default:
                    type = 0x0725; time = 300000;
                    if (Level < 20) { type = 0x0724; time = 60000; }
                    if (Level <= 1) { type = 0x0723; time = 30000; }
                    break;
            }

            Entity grave = new Entity(type, time);
            grave.TrySetSV(StatType.Name, Name);
            Parent.AddEntity(grave, Position);
            Parent.RemoveEntity(this);

            Manager.AddTimedAction(1500, () => 
            {
                Client.Disconnect();
            });
        }

        public bool Damage(string hitter, int damage, ConditionEffectDesc[] effects, bool pierces)
        {
#if DEBUG
            if (HasConditionEffect(ConditionEffectIndex.Invincible))
                throw new Exception("Entity should not be damaged if invincible");
            if (effects == null)
                throw new Exception("Null effects");
            if (string.IsNullOrWhiteSpace(hitter))
                throw new Exception("Undefined hitter");
#endif

            //Projectiles never have null effects. But other sources of damage might.
            foreach (ConditionEffectDesc eff in effects)
                ApplyConditionEffect(eff.Effect, eff.DurationMS);

            //Force pierce if armor broken
            if (HasConditionEffect(ConditionEffectIndex.ArmorBroken))
                pierces = true;

            //Calculate damage with defense
            int damageWithDefense = this.GetDefenseDamage(damage, Stats[3] + Boosts[3], pierces);

            //Nullify damage if invulnerable
            if (HasConditionEffect(ConditionEffectIndex.Invulnerable))
                damageWithDefense = 0;

            HP -= damageWithDefense;
            FameStats.DamageTaken += damageWithDefense;
            if (HP <= 0)
            {
                Death(hitter);
                return true;
            }

            byte[] packet = GameServer.Damage(Id, effects.Select(k => k.Effect).ToArray(), damageWithDefense);
            foreach (Entity en in Parent.PlayerChunks.HitTest(Position, SightRadius))
                if (en is Player player && player.Client.Account.AllyDamage && !player.Equals(this))
                    player.Client.Send(packet);
            return false;
        }

        public override void Tick()
        {
            if (TooLongSinceLastValidation())
            {
                Client.Disconnect();
                return;
            }

            if (Manager.TotalTime % 60000 == 0)
                FameStats.MinutesActive++;

            TickRegens();
            TickProjectiles();
            base.Tick();
        }

        private int _serverStartTime = -1;
        private int _serverTime = -1;
        private int _clientStartTime = -1;
        private int _clientTime = -1;
        public bool ValidTime(int clientTime)
        {
            int serverTime = Manager.TotalTimeUnsynced;
            if (_serverTime == -1)
            {
                _clientTime = clientTime;
                _clientStartTime = clientTime;
                _serverTime = serverTime;
                _serverStartTime = serverTime;
                return true;
            }

            if (clientTime < _clientTime)
                return false;

            int clientDiff = clientTime - _clientTime;
            int serverDiff = serverTime - _serverTime;
            int startDiff = Math.Abs((serverTime - _serverStartTime) - (clientTime - _clientStartTime));

            if (clientDiff < 0 || serverDiff < 0 || clientDiff > MaxLatencyMS || serverDiff > MaxLatencyMS)
                return false;

            if (startDiff > MaxLatencyMS)
                return false;

            _clientTime = clientTime;
            _serverTime = serverTime;
            return true;
        }

        public bool TooLongSinceLastValidation()
        {
            if (_serverTime == -1)
                return false;
            int serverDiff = Manager.TotalTimeUnsynced - _serverTime;
            return serverDiff > MaxLatencyMS;
        }

        public override string ToString()
        {
            return $"<{Name}> <{Parent.Name}:{Parent.Id}> <{Position.ToIntPoint()}>";
        }

        public override void Dispose()
        {
            TileUpdates = null;
            EntityUpdates.Clear();
            Entities.Clear();
            CalculatedSightCircle.Clear();
            AwaitingProjectiles.Clear();
            AckedProjectiles.Clear();
            ShotProjectiles.Clear();
            AwaitingAoes.Clear();
            ShootAEs.Clear();
            AwaitingGoto.Clear();
            PrivateSVs.Clear();
            base.Dispose();
        }
    }
}
