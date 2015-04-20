#region

using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace GodlikeSkillz
{
    internal class Corki : Champion
    {
        public static Spell E;
        public static Spell Q;
        public static Spell R1;
        public static Spell R2;

        public Corki()
        {
            Utils.PrintMessage("Corki loaded");

            Q = new Spell(SpellSlot.Q, 825f);
            E = new Spell(SpellSlot.E, 625f);
            R1 = new Spell(SpellSlot.R, 1275f);
            R2 = new Spell(SpellSlot.R, 1350f);

            Q.SetSkillshot(0.25f, 120f, 1000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0f, (float)(45 * Math.PI / 180), 1500, false, SkillshotType.SkillshotCone);
            R1.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);
            R2.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, E, R1 };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (R1.IsReady() && GetValue<bool>("UseRM"))
            {
                var bigRocket = HasBigRocket();
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(bigRocket ? R2.Range : R1.Range) &&
                                    R1.GetDamage(hero) * (bigRocket ? 1.5f : 1f) > hero.Health))
                {
                    CastR(hero, GetValue<Slider>("RHit").Value);
                }
            }

            if ((!ComboActive && !HarassActive)) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));
            var rLim = GetValue<Slider>("Rlim" + (ComboActive ? "C" : "H")).Value;

            if (useQ && Q.IsReady())
            {
                var t = (Obj_AI_Hero)Orbwalker.GetTarget() ??
                        TargetSelector.GetTarget(725f, TargetSelector.DamageType.Physical);
                if (t != null && (AttackReadiness > 0.2 && AttackReadiness < 0.8))
                    if (Q.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                        return;
            }
            if (useR && R1.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Ammo > rLim)
            {
                var bigRocket = HasBigRocket();
                var t = (Obj_AI_Hero)Orbwalker.GetTarget() ??
                        TargetSelector.GetTarget(bigRocket ? R2.Range : R1.Range, TargetSelector.DamageType.Magical);
                if (t != null && (Player.Distance(t) > Orbwalking.GetRealAutoAttackRange(t) || (AttackReadiness > 0.15 && AttackReadiness < 0.9)))
                    CastR(t, GetValue<Slider>("RHit").Value);
            }
        }

        public override void Orbwalking_OnAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || !unit.IsMe)
                return;

            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));

            if (useE && E.IsReady())
                E.Cast(t); 
        }

        private static void CastR(Obj_AI_Base target, int hitChanceNum)
        {
            var bigRocket = HasBigRocket();
            switch (hitChanceNum)
            {
                case 0:
                    if (bigRocket)
                        CastSpellAoEShot(R2, target, 75);
                    else
                        CastSpellAoEShot(R1, target, 150);
                    break;
                case 1:
                    {
                        R1.Collision = false;
                        R2.Collision = false;
                        var chance = Q.GetPrediction(target).Hitchance;
                        if (chance >= HitChance.VeryHigh)
                        {
                            R1.Collision = true;
                            R2.Collision = true;
                            if (bigRocket)
                                CastSpellAoEShot(R2, target, 150);
                            else
                                CastSpellAoEShot(R1, target, 75);
                        }
                    }
                    break;
                case 2:
                    {
                        if (target.Path.Count() >= 2)
                        {
                            return;
                        }
                        R1.Collision = false;
                        R2.Collision = false;
                        var chance = Q.GetPrediction(target).Hitchance;
                        if (chance >= HitChance.VeryHigh)
                        {
                            R1.Collision = true;
                            R2.Collision = true;
                            if (bigRocket)
                                CastSpellAoEShot(R2, target, 150);
                            else
                                CastSpellAoEShot(R1, target, 75);
                        }                      
                    }
                    break;
            }
        }

        public static bool HasBigRocket()
        {
            return Player.Buffs.Any(buff => buff.DisplayName.ToLower() == "corkimissilebarragecounterbig");
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RHit" + Id, "R Hitchance").SetValue(new Slider(2, 0, 2)));
            config.AddItem(new MenuItem("RlimC" + Id, "Keep R Stacks").SetValue(new Slider(0, 0, 7)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(false));
            config.AddItem(new MenuItem("UseRH" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimH" + Id, "Keep R Stacks").SetValue(new Slider(3, 0, 7)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 255))));
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
