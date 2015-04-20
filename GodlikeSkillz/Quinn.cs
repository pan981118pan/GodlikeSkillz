#region
using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace GodlikeSkillz
{
    internal class Quinn : Champion
    {
        public static float ValorMinDamage = 0;
        public static float ValorMaxDamage = 0;
        public Spell E;
        public Spell Q;
        public Spell R;
        public Obj_AI_Base YTarget;
        public Obj_AI_Base ETarget;

        public Quinn()
        {
            Utils.PrintMessage("Quinn loaded.");

            Q = new Spell(SpellSlot.Q, 1000);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 650);

            Q.SetSkillshot(0.25f, 160f, 1150, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.25f, 2000f);
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, E };
            foreach (Spell spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);

                if (menuItem.Active && spell.Level > 0 && IsValorMode())
                    Render.Circle.DrawCircle(Player.Position, R.Range, menuItem.Color);
            }
            if (ComboActive && IsValorMode())
            {
                var cursorItem = GetValue<Circle>("Cursor");
                var targetItem = GetValue<Circle>("Target");
                if (cursorItem.Active)
                    Render.Circle.DrawCircle(Game.CursorPos, GetValue<Slider>("Cirsize").Value, cursorItem.Color);
                if (targetItem.Active)
                    Render.Circle.DrawCircle(YTarget.Position, 75, targetItem.Color);
            }
        }

        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe)
            {
                return;
            }
            if (spell.SData.Name.Contains("summoner"))
            {
                return;
            }
            if (spell.SData.Name == Player.Spellbook.GetSpell(SpellSlot.E).Name && spell.Target != null)
            {
                ETarget = spell.Target as Obj_AI_Base;
            }
        }

        public static bool IsPositionSafe(Obj_AI_Hero target, Spell spell)
        // use underTurret and .Extend for this please
        {
            var predPos = spell.GetPrediction(target).UnitPosition.To2D();
            var myPos = Player.Position.To2D();
            var newPos = (target.Position.To2D() - myPos);
            newPos.Normalize();

            var checkPos = predPos + newPos * (spell.Range - Vector2.Distance(predPos, myPos));
            Obj_Turret closestTower = null;

            foreach (var tower in ObjectManager.Get<Obj_Turret>()
                .Where(tower => tower.IsValid && !tower.IsDead && Math.Abs(tower.Health) > float.Epsilon)
                .Where(tower => Vector3.Distance(tower.Position, Player.Position) < 1450))
            {
                closestTower = tower;
            }

            if (closestTower == null)
                return true;

            if (Vector2.Distance(closestTower.Position.To2D(), checkPos) <= 910)
                return false;

            return true;
        }

        public static bool IsHePantheon(Obj_AI_Hero target)
        {
            /* Quinn's Spell E can do nothing when Pantheon's passive is active. */
            return target.Buffs.All(buff => buff.Name == "pantheonpassivebuff");
        }

        private static bool IsValorMode()
        {
            return Player.Spellbook.GetSpell(SpellSlot.R).Name == "QuinnRFinale";
        }

        public static void CalculateValorDamage()
        {
            //var multiplier = 1 + ((100 - target.HealthPercentage()) / 100);
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level > 0)
            {
                ValorMinDamage = 1 * 50 + 50;
                ValorMinDamage += Player.FlatPhysicalDamageMod * 0.5f;

                ValorMaxDamage = 1 * 100 + 100;
                ValorMaxDamage += Player.FlatPhysicalDamageMod * 1;
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (LaneClearActive)
            {
                var vMinions = MinionManager.GetMinions(Player.Position, Orbwalking.GetRealAutoAttackRange(null));
                foreach (var minions in
                    vMinions.Where(
                        minions => minions.HasBuff("QuinnW") && (minions.Health < (Player.GetAutoAttackDamage(minions) + PassiveMarkDamage(minions)) || minions.Health > (Player.GetAutoAttackDamage(minions) + PassiveMarkDamage(minions)) + (20 + Player.Level*2))))
                {
                    Orbwalker.ForceTarget(minions);
                }
            }

            if (!ComboActive)
            {
                Orbwalker.SetAttack(true);
                Orbwalker.SetMovement(true);
                return;
            }
            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));          
            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR");
            var useET = GetValue<bool>("UseET" + (ComboActive ? "C" : "H"));
            var manE = GetValue<KeyBind>("ManualE").Active;

            if (Player.IsMelee())
            {
                if (IsValorMode())
                {
                    if (useR)
                    {
                        var vTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                        if (vTarget != null)
                        {
                            CalculateValorDamage();
                            if (vTarget.Health >= ValorMinDamage && vTarget.Health <= ValorMaxDamage)
                                R.Cast();
                        }
                    }
                    if (Q.IsReady() && useQ)
                    {
                        var t = TargetSelector.GetTarget(275, TargetSelector.DamageType.Physical);
                        if (t != null && (AttackReadiness > 0.2 && AttackReadiness < 0.75))
                            Q.Cast();
                    }
                }
                var targetLoc = Game.CursorPos;
                YTarget = (TargetSelector.GetSelectedTarget() != null &&
                       TargetSelector.GetSelectedTarget().Distance(Game.CursorPos) <= GetValue<Slider>("Cirsize").Value)
                ? TargetSelector.GetSelectedTarget()
                : TargetSelector.GetTarget(
                    GetValue<Slider>("Cirsize").Value, TargetSelector.DamageType.Physical, true, null, Game.CursorPos);
                Orbwalker.ForceTarget(YTarget);
                Orbwalker.SetMovement(false);
                if (YTarget.IsValidTarget())
                {
                    Orbwalker.SetAttack(
                    Orbwalker.GetTarget().IsValidTarget() && Orbwalker.GetTarget().NetworkId == YTarget.NetworkId);
                    if (Orbwalking.CanMove(50))
                    {
                        if (!Orbwalker.InAutoAttackRange(YTarget))
                        {
                            var pLoc = Prediction.GetPrediction(YTarget, 0.1f, 175, 600).UnitPosition;
                            MoveTo(pLoc, Orbwalking.GetRealAutoAttackRange(YTarget) / 2);
                        }
                        else
                        {
                            MoveByTarget(YTarget);
                        }
                    }
                    if (E.IsReady() && (useE || manE) && Player.Distance(YTarget) > 350)
                    {
                        E.CastOnUnit(YTarget);
                    }
                }
                else
                {
                    MoveTo(targetLoc, 100);
                    Orbwalker.SetAttack(false);
                }
            }
            else
            {
                Orbwalker.SetAttack(true);
                Orbwalker.SetMovement(true);
                var targ =
                        HeroManager.Enemies.Where(enemy => enemy.IsValidTarget() && Orbwalking.InAutoAttackRange(enemy) && enemy.HasBuff("QuinnW")).OrderBy(PhysHealth)
                            .FirstOrDefault();
                Orbwalker.ForceTarget(targ);

                if (E.IsReady() && (useE || manE))
                {
                    var vTarget = Orbwalker.GetTarget() as Obj_AI_Hero ?? TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (vTarget != null && !IsHePantheon(vTarget) &&
                        (Player.Distance(vTarget) > Orbwalking.GetRealAutoAttackRange(vTarget) ||
                         (AttackReadiness > 0.2 && AttackReadiness < 0.75)))
                    {
                        if (!useET)
                            E.CastOnUnit(vTarget);
                        else if (!vTarget.UnderTurret())
                            E.CastOnUnit(vTarget);
                    }
                }               
                if (Q.IsReady() && useQ)
                {
                    var t =
                        HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && (GetValue<StringList>("Blind" + enemy.ChampionName).SelectedIndex == 1
                            || enemy.HealthPercentage() <= GetValue<Slider>("BlindD").Value || !BlindTargetNear(enemy))).OrderBy(PhysHealth)
                            .FirstOrDefault();
                    if (t != null &&
                        (Player.Distance(t) > Orbwalking.GetRealAutoAttackRange(t) ||
                         (AttackReadiness > 0.2 && AttackReadiness < 0.75)))
                    {
                        Q.Collision = false;
                        var chance = Q.GetPrediction(t).Hitchance;
                        if (chance >= HitChance.VeryHigh)
                        {
                            Q.Collision = true;
                            CastSpellAoEShot(Q, t, 100);
                        }
                    }
                }
                Drawing.DrawText(
        Drawing.WorldToScreen(Player.Position)[0] + 50,
        Drawing.WorldToScreen(Player.Position)[1] - 20, Color.Yellow, "lol");
            }
        }

        public bool BlindTargetNear(Obj_AI_Base target)
        {
            return HeroManager.Enemies.FirstOrDefault(enemy => enemy.NetworkId != target.NetworkId && enemy.Distance(target) < 600 && GetValue<StringList>("Blind" + enemy.ChampionName).SelectedIndex == 1) != null;
        }

        public double PassiveMarkDamage(Obj_AI_Base target)
        {
            return Player.CalcDamage(target, Damage.DamageType.Physical, 15 + (Player.Level * 10) + (Player.FlatPhysicalDamageMod * 0.5));
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("Target" + Id, "Target Circle").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("ManualE" + Id, "Manual E").SetValue(new KeyBind(32, KeyBindType.Press)));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseETC" + Id, "Do not Under Turret E").SetValue(true));
            config.AddItem(new MenuItem("UseR" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("BlindD" + Id, "Blind for Damage when Enemy is below").SetValue(new Slider(60)));
            foreach (var obj in HeroManager.Enemies)
            {
                config.AddItem(new MenuItem("Blind" + obj.ChampionName + Id, obj.ChampionName))
                    .SetValue(new StringList(new[] { "Dont Blind", "Blind"}, 1));
            }
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseETH" + Id, "Do not Under Turret E").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("Cirsize" + Id, "Cursor Circle Size").SetValue(new Slider(400, 300, 600)));
            config.AddItem(
                new MenuItem("Cursor" + Id, "Cursor Range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 255, 255))));
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
