#region
using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace GodlikeSkillz
{
    internal class Kogmaw : Champion
    {
        public Spell E;
        public Spell Q;
        public Spell R;
        public int UltimateBuffStacks = 0;
        public Spell W;

        public Kogmaw()
        {
            Utils.PrintMessage("KogMaw loaded.");

            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1360f);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 120f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, W, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position,
                        spell.Slot == SpellSlot.W ? Orbwalking.GetRealAutoAttackRange(null) + 65 + W.Range : spell.Range,
                        menuItem.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            UltimateBuffStacks = GetUltimateBuffStacks();
            W.Range = 110 + 20 * Player.Spellbook.GetSpell(SpellSlot.W).Level;
            R.Range = 900 + 300 * Player.Spellbook.GetSpell(SpellSlot.R).Level;

            if (R.IsReady() && GetValue<bool>("UseRM"))
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero => hero.IsValidTarget(R.Range) && R.GetDamage(hero) > hero.Health))
                    R.Cast(hero, false, true);

            if ((!ComboActive && !HarassActive)) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));
            var rLim = GetValue<Slider>("Rlim" + (ComboActive ? "C" : "H")).Value;

            if (useW && W.IsReady())
            {
                var targ = TargetSelector.GetTarget(
                    Orbwalking.GetRealAutoAttackRange(null) + W.Range, TargetSelector.DamageType.Physical);
                if (targ != null)
                {
                    if (targ.Name == Orbwalker.GetTarget().Name && (Player.HealthPercentage() < 70 || targ.HealthPercentage() < 70))
                    {
                        W.CastOnUnit(Player);
                    }
                    else
                    {
                        W.CastOnUnit(Player);
                    }
                }                
            }

            if (useR && R.IsReady() && UltimateBuffStacks < rLim
                && (Player.ManaPercentage() >= GetValue<Slider>("RlimM").Value || UltimateBuffStacks <= 1))
            {
                var t = Orbwalker.GetTarget() as Obj_AI_Hero ??
                        TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t != null && (ObjectManager.Player.Distance(t) > Orbwalking.GetRealAutoAttackRange(t) || (AttackReadiness > 0.2 && AttackReadiness < 0.9)))
                    R.Cast(t, false, true);
            }

            if (AttackReadiness < 0.2 || AttackReadiness > 0.8)
                return;

            if (useQ && Q.IsReady())
            {
                var t = Orbwalker.GetTarget() as Obj_AI_Hero;
                if (t != null)
                    if (Q.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                        return;
            }

            if (useE && E.IsReady() && Player.ManaPercentage() >= GetValue<Slider>("RlimM").Value)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    E.CastIfWillHit(t,2);
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (target != null && (!ComboActive && !HarassActive) || !unit.IsMe || !(target is Obj_AI_Hero))
                return;

        }

        private static int GetUltimateBuffStacks()
        {
            return (from buff in Player.Buffs
                    where buff.DisplayName.ToLower() == "kogmawlivingartillery"
                    select buff.Count).FirstOrDefault();
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimC" + Id, "R Limiter").SetValue(new Slider(3, 10, 1)));
            config.AddItem(new MenuItem("RlimM" + Id, "R Mana Limiter").SetValue(new Slider(25, 0, 50)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(false));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(false));
            config.AddItem(new MenuItem("UseRH" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimH" + Id, "R Limiter").SetValue(new Slider(1, 10, 1)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true,
                    System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false,
                    System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseRM" + Id, "Use R To Killsteal").SetValue(true));
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
