#region
using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using GodlikeSkillz.Evade;
using Color = System.Drawing.Color;
#endregion

namespace GodlikeSkillz
{
    internal class Zed : Champion
    {
        public static Spell W;
        public static Spell E;
        public static Spell Q;
        public static Spell R;
        public static Obj_AI_Minion WShadow;
        public static Vector3 WShadowPos;
        public static float WTimer;
        public static bool WNext;
        public static Obj_AI_Minion RShadow;
        public static Vector3 RShadowPos;
        public static float RTimer;
        public static bool RNext;
        public static bool ECast;

        public Zed()
        {
            Utils.PrintMessage("Zed loaded.");

            Q = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 300);
            R = new Spell(SpellSlot.R, 625);

            Q.SetSkillshot(0.25f, 50f, 1700, false, SkillshotType.SkillshotLine);
            //Orbwalk = false;
            UsesMana = false;

            GameObject.OnCreate += OnCreateObj;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
        }

        public override void Orbwalking_OnAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && unit.IsMe)
            {
                E.Cast();
                /*var tiamatId = ItemData.Tiamat_Melee_Only.Id;
                    var hydraId = ItemData.Ravenous_Hydra_Melee_Only.Id;
                    var hasTiamat = Items.HasItem(tiamatId);
                    var hasHydra = Items.HasItem(hydraId);

                    if (hasHydra || hasTiamat)
                    {
                        var itemId = hasTiamat ? tiamatId : hydraId;
                        Items.UseItem(itemId);
                    }*/

            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, W, E };
            foreach (Spell spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);

            }
            /*if (GetValue<KeyBind>("Combo").Active)
            {
                Render.Circle.DrawCircle(GetEnemy.Position, 50, Color.Red);
            }*/
            //if(WShadowPos == new Vector3())
            //Render.Circle.DrawCircle(WShadowPos, 50, Color.Red);
            //Render.Circle.DrawCircle(WShadow.Position, 55, Color.Blue);
            //Render.Circle.DrawCircle(PredictionFrom(Player, GetEnemy, Q.Delay, Q.Width, Q.Speed).CastPosition, 50, Color.Green);
            //CastQ(GetEnemy);
        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender != null)
            {
                if (sender.Name == "Shadow")
                {
                    if (WNext)
                    {
                        WShadow = sender as Obj_AI_Minion;
                        WNext = false;
                    }
                    else if (RNext)
                    {
                        RShadow = sender as Obj_AI_Minion;
                        RNext = false;
                    }
                }
            }
        }

        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs castedSpell)
        {
            if (!unit.IsMe)
                return;

            if (castedSpell.SData.Name == "ItemTiamatCleave")
            {
                ECast = true;
            }
            if (castedSpell.SData.Name == "ZedShadowDash")
            {
                WNext = true;
                WShadowPos = Player.Distance(castedSpell.End) < 400 ? Player.Position.Extend(castedSpell.End,400) : castedSpell.End;
            }
            if (castedSpell.SData.Name == "ZedPBAOEDummy")
            {
                var target = TargetSelector.GetTarget(300, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var tiamatId = ItemData.Tiamat_Melee_Only.Id;
                    var hydraId = ItemData.Ravenous_Hydra_Melee_Only.Id;
                    var hasTiamat = Items.HasItem(tiamatId);
                    var hasHydra = Items.HasItem(hydraId);

                    if (hasHydra || hasTiamat)
                    {
                        var itemId = hasTiamat ? tiamatId : hydraId;
                        Items.UseItem(itemId);
                    }
                }
            }
            if (castedSpell.SData.Name == "zedult")
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, castedSpell.Target);
                RTimer = Game.Time + 7;
                RNext = true;
            }

        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (WShadow != null && Game.Time > WTimer) WShadow = null;
            if (RShadow != null && Game.Time > RTimer) RShadow = null;

            if (WShadow != null)
                WShadowPos = WShadow.Position;
            else if (!WNext)
                WShadowPos = new Vector3();

            RShadowPos = RShadow != null ? RShadow.Position : new Vector3();

            if (ECast)
            {
                E.Cast();
            }
            if (!E.IsReady())
            {
                ECast = false;
            }
            UseItems(DeathMark());
            if (GetValue<bool>("UseQC") && DeathMark() != null && DeathMark().Buffs.Find(b => b.Name == "zedulttargetmark") != null)
            {
                QSnipe(DeathMark());
            }
            if (GetValue<KeyBind>("Combo").Active)
            {
                Combo(GetEnemy);               
            }
            if(GetValue<bool>("UseEC"))
            {
                CastE();
            }
            if (ShadowStage == ShadowCastStage.Second)
            {
                if (Game.Time > WTimer)
                {
                    WTimer = Game.Time + 5;
                }
            }
        }

        private static void QSnipe(Obj_AI_Hero enemy)
        {
            var buff = enemy.Buffs.Find(b => b.Name == "zedulttargetmark");
            var time = (int) Math.Floor(buff.EndTime - Game.Time);
            var go = true;
            var closer =
                AntiGapcloser.Spells.Where(spell => spell.ChampionName == enemy.ChampionName)
                    .FirstOrDefault(gap => enemy.Spellbook.GetSpell(gap.Slot).CooldownExpires - Game.Time < 0 || enemy.Spellbook.GetSpell(gap.Slot).CooldownExpires - Game.Time > enemy.Spellbook.GetSpell(gap.Slot).Cooldown - 0.25);
            if (closer.SpellName != null)
                go = false;
            if (go || buff.EndTime - Game.Time <= 1)
            {
                CastQ(enemy, time, 2);
            }
        }

        private void Combo(Obj_AI_Hero enemy)
        {
            CastQ(enemy, GetValue<Slider>("QHit").Value, 1);
        }

        private static void CastE()
        {
            if (!E.IsReady()) return;
            if (
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.IsValidTarget() &&
                            ((hero.Distance(ObjectManager.Player.ServerPosition) <= E.Range && ShadowStage == ShadowCastStage.Cooldown) ||
                             (WShadowPos != new Vector3() && hero.Distance(WShadowPos) <= E.Range) ||
                             (RShadow != null && hero.Distance(RShadow.ServerPosition) <= E.Range))) <= 0)
            {
                return;
            }
            if (Math.Min(Player.Mana + (Player.PARRegenRate * Math.Max(Q.Instance.CooldownExpires - Game.Time, 0)), 200) > (Q.Instance.ManaCost + E.Instance.ManaCost))
            {
                E.Cast();
            }
        }

        public static void UseItems(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget())
                return;
            Items.UseItem(ItemData.Youmuus_Ghostblade.Id);
            if (target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 450)
            {
                var hasCutGlass = Items.HasItem(3144);
                var hasBotrk = Items.HasItem(3153);

                if (hasBotrk || hasCutGlass)
                {
                    var itemId = hasCutGlass ? 3144 : 3153;
                    Items.UseItem(itemId, target);
                }
            }
            if (target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 300 && E.IsReady())
            {
                var tiamatId = ItemData.Tiamat_Melee_Only.Id;
                var hydraId = ItemData.Ravenous_Hydra_Melee_Only.Id;
                var hasTiamat = Items.HasItem(tiamatId);
                var hasHydra = Items.HasItem(hydraId);

                if (hasHydra || hasTiamat)
                {
                    var itemId = hasTiamat ? tiamatId : hydraId;
                    Items.UseItem(itemId);
                }
            }
        }

        private static void CastQ(Obj_AI_Base target, int hitChanceNum, int hits)
        {
            if (!Q.IsReady()) return;
            var count = 0;
            var castpos = new Vector3();
            Vector3[] origin = { Player.ServerPosition, WShadowPos, RShadowPos};
            foreach (Vector3 from in origin)
            {
                if (from != new Vector3())
                {                   
                    //var pred1 = PredictionFrom(from, target, Q.Delay, Q.Width, Q.Speed);
                    var pred1 = Prediction.GetPrediction(
                new PredictionInput{From = from, Unit = target, Delay = Q.Delay, Range = Q.Range, Radius = Q.Width, Speed = Q.Speed});
                    if (from.Distance(pred1.CastPosition) <= Q.Range)
                    {
                        if (hitChanceNum == 0 || (hitChanceNum == 1 && pred1.Hitchance == HitChance.VeryHigh) ||
                            (hitChanceNum == 2 &&
                             (pred1.Hitchance == HitChance.VeryHigh || pred1.Hitchance == HitChance.Dashing ||
                              pred1.Hitchance == HitChance.Immobile)))
                        {
                            count++;
                            castpos += pred1.CastPosition;
                        }
                    }
                }
            }
            castpos = castpos / count;
            if (count >= hits)
            {
                Q.Cast(castpos);
            }
        }

        public static Obj_AI_Hero DeathMark()
        {
            return HeroManager.Enemies.FirstOrDefault(enemyVisible => enemyVisible.IsValidTarget() && enemyVisible.HasBuff("zedulttargetmark", true));
        }

        static Obj_AI_Hero GetEnemy
        {
            get
            {
                Obj_AI_Hero[] targ = { null };
                foreach (var tar in HeroManager.Enemies.Where(tar => tar.IsValidTarget() && (tar.Distance(Player) < Q.Range || (WShadowPos != new Vector3() && tar.Distance(WShadowPos) < Q.Range) || (RShadow != null && tar.Distance(RShadow) < Q.Range))).Where(tar => targ[0] == null || PhysHealth(tar) < PhysHealth(targ[0])))
                {                
                    targ[0] = tar;
                }
                return DeathMark() ?? targ[0];
            }
        }
      
        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!W.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);
            }
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

            public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("Combo" + Id, "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("QHit" + Id, "Q Hitchance").SetValue(new Slider(1, 0, 2)));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(155, 155, 0, 155))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 0))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 0))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            return true;
        }
    }
}