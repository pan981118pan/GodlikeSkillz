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
    internal class Lucian : Champion
    {
        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell R;

        private bool _pCast;
        private bool ls;

        public Lucian()
        {
            Utils.PrintMessage("Lucian loaded.");

            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000);
            R = new Spell(SpellSlot.R, 1400);

            Q.SetSkillshot(0.25f, 65f, 1100f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            //Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        /*public void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.R)
            {
                //args.Process = false;
                if (GetValue<bool>("GhostR"))
                {
                    Items.UseItem(3142);
                }
                if (GetValue<bool>("WR"))
                {
                    W.Cast(Game.CursorPos);
                }
                R.Cast(Game.CursorPos);
            }
        }*/

        public static Obj_AI_Base QMinion
        {
            get
            {
                var vTarget = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
                var vMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.None);

                return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(Q.Range))
                    let endPoint =
                        vMinion.ServerPosition.To2D()
                            .Extend(ObjectManager.Player.ServerPosition.To2D(), -Q2.Range)
                            .To3D()
                    where
                        vMinion.Distance(vTarget) <= vTarget.Distance(ObjectManager.Player) &&
                        Intersection(
                            ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(),
                            Prediction.GetPrediction(vTarget, 0.25f).UnitPosition.To2D(),
                            vTarget.BoundingRadius + Q.Width / 4)
                    /*vMinion.Distance(vTarget) <= vTarget.Distance(ObjectManager.Player) &&
                            Intersection(ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(),
                                vTarget.ServerPosition.To2D(), vTarget.BoundingRadius + Q.Width / 4)*/
                    select vMinion).FirstOrDefault();
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, Q2, W };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (!menuItem.Active || spell.Level < 0)
                {
                    return;
                }

                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);

            var m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            var constant = (m * p1.X) - p1.Y;
            var b = -(2f * ((m * constant) + p3.X + (m * p3.Y)));
            var a = (1 + (m * m));
            var c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * constant * p3.Y) + (constant * constant));
            var d = ((b * b) - (4f * a * c));

            return d > 0;
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

            if (spell.SData.Name == ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Name)
            {
                _pCast = true;
                ls = true;
            }
            if (spell.SData.Name == ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name)
            {
                _pCast = true;
                ls = true;
            }
            if (spell.SData.Name == ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Name)
            {
                _pCast = true;
                ls = true;
            }
            if (spell.SData.Name == "LucianR")
            {
                _pCast = true;
                ls = true;
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {

            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            if (_pCast && ObjectManager.Player.HasBuff("lucianpassivebuff", true))
            {
                _pCast = false;
                ls = true;
            }
            if (ls && !_pCast && !ObjectManager.Player.HasBuff("lucianpassivebuff", true))
            {
                ls = false;
            }

            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active && ToggleActive)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                {
                    return;
                }

                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                {
                    Q.CastOnUnit(t);
                }
            }

            if (Q.IsReady() && GetValue<KeyBind>("UseQExtendedTH").Active && ToggleActive)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                {
                    return;
                }

                var t = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && QMinion.IsValidTarget())
                {
                    if (ObjectManager.Player.Distance(t) > Q.Range)
                    {
                        Q.CastOnUnit(QMinion);
                    }
                }
            }

            if ((!ComboActive && !HarassActive))
            {
                return;
            }

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var targ = Orbwalker.GetTarget() as Obj_AI_Base ??
                       TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (targ.IsValidTarget() && AttackReadiness > 0.2 && AttackReadiness < 0.8 && !ls)
            {
                if (W.IsReady() && useW && (Player.AttackSpeedMod > 1.5 || !Q.IsReady() || GetValue<bool>("WFirst")))
                {
                    W.Cast(targ);
                }
                else if (Q.IsReady() && useQ)
                {
                    Q.CastOnUnit(targ);
                }
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseQExtendedC" + Id, "Use Extended Q").SetValue(true));
            config.AddItem(new MenuItem("Cx", ""));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("WFirst" + Id, "Use W First in Combo").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(
                    new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            config.AddItem(new MenuItem("Cx", ""));
            config.AddItem(new MenuItem("UseQExtendedH" + Id, "Use Extended Q").SetValue(true));
            config.AddItem(
                new MenuItem("UseQExtendedTH" + Id, "Use Ext. Q (Toggle)").SetValue(
                    new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("GhostR" + Id, "Use Ghostblade with Ult").SetValue(true));
            config.AddItem(new MenuItem("WR" + Id, "Use W with Ult").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.Gray)));
            config.AddItem(new MenuItem("DrawQ2" + Id, "Ext. Q range").SetValue(new Circle(true, Color.Gray)));
            config.AddItem(new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.Gray)));
            config.AddItem(new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.Gray)));

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