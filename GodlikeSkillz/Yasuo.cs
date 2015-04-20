using System;
using System.Collections.Generic;
using System.Linq;
using GodlikeSkillz.Evade;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;

namespace GodlikeSkillz
{
    internal class Yasuo : Champion
    {
        public static List<Skillshot> DetectedSkillShots = new List<Skillshot>();
        public static List<Skillshot> EvadeDetectedSkillshots = new List<Skillshot>();
        public static List<MenuData> MenuWallsList = new List<MenuData>();

        public static Spell Q1;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static bool ECasting;
        private static bool ECast;
        private Obj_AI_Base ETar;
        public Obj_AI_Base YTarget;
        //private string textl;

        public Yasuo()
        {
            Utils.PrintMessage("Yasuo loaded");

            Q1 = new Spell(SpellSlot.Q, 475f);
            Q2 = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 400f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1200f);

            Q1.SetSkillshot(0.45f, 50f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.45f, 50f, 1200f, false, SkillshotType.SkillshotLine);

            UsesMana = false;
            new YasuoEvade();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
        }

        public struct MenuData
        {
            public string ChampionName;
            public string DisplayName;
            public bool IsWindwall;
            public string Slot;
            public string SpellDisplayName;
            public string SpellName;

            public void AddToMenu(Menu config, string id)
            {
                if (config.Item("autoww" + "." + ChampionName + "." + Slot + id) == null)
                {
                    config.AddItem(new MenuItem("autoww" + "." + ChampionName + "." +
                        Slot + id, SpellDisplayName).SetValue(true));
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if(Player.IsDead)
                return;

            Spell[] spellList = {Q1, W, E, R};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }
            if (ComboActive)
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

            if (spell.SData.Name == Player.Spellbook.GetSpell(SpellSlot.E).Name && spell.Target != null)
            {
                var ePos = GetDashingEnd((Obj_AI_Base)spell.Target).Extend(Player.Position.To2D(), -125f).To3D();
                ETar = (Obj_AI_Base)spell.Target;
                if (UnitNearE(ePos, ETar))
                {
                    ECast = true;
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            /*Drawing.DrawText(
                    Drawing.WorldToScreen(Player.Position)[0] + 50,
                    Drawing.WorldToScreen(Player.Position)[1] - 20, Color.Yellow, "" + (E.Instance.CooldownExpires - Game.Time));*/

            EvadeDetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());
            Evader();

            if (Player.IsDashing() && ECast)
            {
                ECasting = true;
                ECast = false;
            }

            if (GetValue<bool>("UseRC"))
            {
                AutoUlt();
            }

            var dashList =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(o => o.IsValidTarget(E.Range) && IsDashable(o) &&
                            (o.Type == GameObjectType.obj_AI_Hero || o.Type == GameObjectType.obj_AI_Minion)).ToList();

            if (GetValue<bool>("UseQC") && (GetValue<KeyBind>("UseQHT").Active || ComboActive))
            {
                AutoQ(
                    HasWhirlwind()
                        ? TargetSelector.GetTarget(950, TargetSelector.DamageType.Physical)
                        : TargetSelector.GetTarget(475, TargetSelector.DamageType.Physical));
            }

            var targetLoc = Game.CursorPos;

            if (LaneClearActive)
            {
                if (GetValue<StringList>("ELmode").SelectedIndex == 2)
                    DashToLoc(targetLoc, dashList, true);
                    LaneClear();
            }

            if (ComboActive)
            {
                if (ECasting && ETar != null && ETar.IsValidTarget() && Q1.IsReady() && !HasWhirlwind())
                {
                    Q1.Cast(ETar);
                }
                YTarget = (TargetSelector.GetSelectedTarget() != null &&
                           TargetSelector.GetSelectedTarget().Distance(Game.CursorPos) <=
                           GetValue<Slider>("Cirsize").Value)
                    ? TargetSelector.GetSelectedTarget()
                    : TargetSelector.GetTarget(
                        GetValue<Slider>("Cirsize").Value, TargetSelector.DamageType.Physical, true, null,
                        Game.CursorPos);
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
                            DashToLoc(YTarget.Position, dashList, false);
                            var pLoc = Prediction.GetPrediction(YTarget, 0.1f, 175, 600).UnitPosition;
                            MoveTo(pLoc, Orbwalking.GetRealAutoAttackRange(YTarget) / 2);
                        }
                        else
                        {
                            MoveByTarget(YTarget);
                        }
                    }
                }
                else
                {
                    MoveTo(targetLoc, 100);
                    DashToLoc(targetLoc, dashList, false);
                    Orbwalker.SetAttack(false);
                }
            }
            else
            {
                Orbwalker.SetAttack(true);
                Orbwalker.SetMovement(true);
            }

            if (!Player.IsDashing() && ECasting && E.Instance.CooldownExpires - Game.Time > 0)
            {
                ECasting = false;
                ETar = null;
            }
        }

        public void LaneClear()
        {
            if (GetValue<bool>("UseEQL"))
            {
                if (ECasting && ETar != null && ETar.IsValidTarget() && Q1.IsReady())
                {
                    Q1.Cast(ETar);
                }
            }

            var vMinions = MinionManager.GetMinions(Player.Position, 950);

            if (GetValue<StringList>("ELmode").SelectedIndex == 1 && E.IsReady())
            {
                var min =
                    vMinions.Where(
                        minion =>
                            minion.IsValidTarget(E.Range) && IsDashable(minion) && !GetDashingEnd(minion).To3D().UnderTurret(true) &&
                            minion.Health < GetSweepingBladeDamage(minion)).OrderBy(tar => tar.Health).FirstOrDefault();
                if (min != null)
                {
                    if (E.CastOnUnit(min))
                        vMinions.Remove(min);
                }
            }

            if (Q1.IsReady() && GetValue<bool>("UseQL") && !ETar.IsValidTarget() && !Player.IsDashing() && !ECast && !ECasting)
            {
                foreach (var minions in vMinions.Where(minions => minions.IsValidTarget(HasWhirlwind() ? Q2.Range : Q1.Range) && minions.Health < Player.GetSpellDamage(minions, SpellSlot.Q) && (AttackNow || !Orbwalker.InAutoAttackRange(minions))).OrderBy(tar => tar.Health)) {
                    if (ETar != null &&  ETar.ToString() == minions.ToString())
                    {
                        if (ETar.IsValidTarget() && GetSweepingBladeDamage(minions) < minions.Health)
                        {
                            if (!HasWhirlwind())
                            {
                                Q1.Cast(minions);
                            }
                            else
                            {
                                Q2.Cast(minions, false, true);
                            }
                        }
                    }
                    else
                    {
                        if (!HasWhirlwind())
                            Q1.Cast(minions);
                        else
                            Q2.Cast(minions, false, true);
                    }
                }
            }
        }

        public void AutoUlt()
        {
            if (R.IsReady())
            {
                var rMode = GetValue<StringList>("rmode").SelectedIndex;
                var rMPercent = GetValue<Slider>("renemieshealthper").Value;
                var rSPercent = GetValue<Slider>("renemyhealthper").Value;
                var rMin = GetValue<Slider>("rmin").Value;

                var targets =
                    ObjectManager.Get<Obj_AI_Hero>().Where(t => t.IsValidTarget() && IsKnockedup(t)).ToList();
                if (targets.Count() == 1 && (rMode == 1 || rMode == 2))
                {
                    var totalPercent = targets[0].Health / targets[0].MaxHealth * 100;
                    if (totalPercent <= rSPercent)
                    {
                        if (KnockupTimeLeft(targets[0]) <= 0.5 && AlliesNearTarget(targets[0], 600))
                        {
                            R.Cast();
                        }
                        else
                        {
                            R.Cast();
                        }
                    }

                }
                else if (targets.Count() >= rMin && (rMode == 0 || rMode == 2))
                {
                    var totalPercent = targets.Sum(t => t.Health / t.MaxHealth * 100) / targets.Count();
                    if (totalPercent <= rMPercent)
                    {
                        var lowestAirtime = targets.OrderBy(t => Game.Time - KnockupTimeLeft(t)).FirstOrDefault();
                        if (lowestAirtime != null && KnockupTimeLeft(lowestAirtime) <= 0.5 && AlliesNearTarget(lowestAirtime, 600))
                        {
                            R.Cast();
                        }
                        else
                        {
                            R.Cast();
                        }
                    }
                }
            }
        }

        public bool AlliesNearTarget(Obj_AI_Base target, float range)
        {
            return HeroManager.Allies.Where(tar => tar.Distance(target) < range).Any(tar => tar != null);
        }

        public static float GetNewQSpeed()
        {
            const float ds = 0.5f;
            var a = 1 / ds * Player.AttackSpeedMod;
            return 1 / a;
        }

        public void AutoQ(Obj_AI_Base targ)
        {
            if (targ != null && targ.IsValidTarget())
            {
                if (Q1.IsReady() && !ECasting && !Player.IsDashing())
                {
                    if (!HasWhirlwind() && Player.Distance(targ) < 450 && AttackNow)
                    {
                        Q1.Cast(targ);
                    }
                    else if (HasWhirlwind() && Player.Distance(targ) < 950 && ComboActive)
                    {
                        Q2.Cast(targ, false, true);
                    }
                }
            }
        }

        public void DashToLoc(Vector3 targetLoc, List<Obj_AI_Base> list , bool turrets)
        {
            if(!GetValue<bool>("UseEC"))
                return;
            Obj_AI_Base[] eMinion = { null };
            foreach (var o in from o in list where Player.Distance(o) < 475 && Player.ServerPosition.Distance(o.ServerPosition) > 45
                              let ePos = GetDashingEnd(o).To3D()
                              where targetLoc.Distance(ePos) < (Math.Abs(Player.Distance(targetLoc) - (Player.MoveSpeed*0.4))) && (!turrets || !ePos.UnderTurret(true))
                                      where eMinion[0] == null || targetLoc.Distance(ePos) < targetLoc.Distance(GetDashingEnd(eMinion[0]).To3D()) select o) 
                eMinion[0] = o;
            
            if (eMinion[0] != null)
            {
                E.Cast(eMinion[0]);
            }
        }

        public bool UnitNearE(Vector3 loc, Obj_AI_Base tar)
        {
            var unit =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(o => o.Distance(loc) < 350 && o.IsValidTarget()
                        && (o.Type == GameObjectType.obj_AI_Hero || (o.Type == GameObjectType.obj_AI_Minion && (GetValue<bool>("UseQCH") || LaneClearActive))) && 
                        ((tar.ToString() == o.ToString() && tar.Health > GetSweepingBladeDamage(tar)) || tar.ToString() != o.ToString()));
            return unit != null;
        }

        public static bool HasWhirlwind()
        {
            return Player.HasBuff("YasuoQ3W");
        }

        public static bool IsDashable(Obj_AI_Base target)
        {
            return Player.Distance(target.Position) < E.Range && !target.HasBuff("YasuoDashWrapper");
        }

        public static bool IsKnockedup(Obj_AI_Base @base, bool selfKnockup = false)
        {
            return selfKnockup
                ? @base.HasBuff("yasuoq3mis")
                : @base.HasBuffOfType(BuffType.Knockup) || @base.HasBuffOfType(BuffType.Knockback);
        }

        public static float KnockupTimeLeft(Obj_AI_Base @base)
        {
            var buff = @base.Buffs.Find(b => b.Type.Equals(BuffType.Knockup) || b.Type.Equals(BuffType.Knockback));
            return (buff != null) ? buff.EndTime - Game.Time : -1f;
        }

        public static double GetSweepingBladeDamage(Obj_AI_Base target)
        {
            var stacksPassive = Player.Buffs.Find(b => b.DisplayName.Equals("YasuoDashScalar"));
            var stacks = 1 + 0.25 * ((stacksPassive != null) ? stacksPassive.Count : 0);
            var damage = ((50 + 20 * E.Level) * stacks) + (Player.FlatMagicDamageMod * 0.6);
            return Player.CalcDamage(target, Damage.DamageType.Magical, damage);
        }

        public static Vector2 GetDashingEnd(Obj_AI_Base target)
        {
            if (!target.IsValidTarget())
            {
                return Vector2.Zero;
            }

            var baseX = Player.Position.X;
            var baseY = Player.Position.Y;
            var targetX = target.Position.X;
            var targetY = target.Position.Y;

            var vector = new Vector2(targetX - baseX, targetY - baseY);
            var sqrt = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);

            var x = (float)(baseX + (E.Range * (vector.X / sqrt)));
            var y = (float)(baseY + (E.Range * (vector.Y / sqrt)));

            return new Vector2(x, y);
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Base;
            if (!unit.IsMe)
                return;

            if (t == null || !t.IsValidTarget())
                return;

            if (LaneClearActive && GetValue<bool>("UseQL"))
            {
                if ((t.Type == GameObjectType.obj_AI_Minion || t.Type == GameObjectType.obj_AI_Hero) &&
                    PhysHealth(t) >
                    (Player.TotalAttackDamage * (Player.Crit > .7
                        ? (Items.HasItem(ItemData.Infinity_Edge.Id) ? 2.25 : 1.8)
                        : 1)))
                {
                    if (Q1.IsReady() && !HasWhirlwind())
                        Q1.Cast(t);
                }
            }
        }

        private void Evader()
        {
            foreach (var skillshot in EvadeDetectedSkillshots)
            {
                if (W.IsReady())
                {
                    if (GetValue<bool>("autoww"))
                    {
                        Windwall(skillshot);
                    }
                }
            }
        }

        private void Windwall(Skillshot skillshot)
        {
            if (!GetValue<bool>("autoww"))
            {
                return;
            }

            if (W.IsReady() && skillshot.SpellData.Type != SkillShotType.SkillshotCircle ||
                skillshot.SpellData.Type != SkillShotType.SkillshotRing)
            {
                var isAboutToHitRange = GetValue<Slider>("wwdelay").Value;

                if (GetValue<bool>("autoww" + "." + skillshot.SpellData.ChampionName + "." + skillshot.SpellData.Slot))
                {
                    
                    if (!skillshot.IsAboutToHit(isAboutToHitRange, Player))
                    {
                        return;
                    }
                    var cast = Player.ServerPosition +
                               Vector3.Normalize(skillshot.MissilePosition.To3D() - Player.ServerPosition) *
                               10;
                    W.Cast(skillshot.MissilePosition.To3D());
                }

            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("Target" + Id, "Target Circle").SetValue(new Circle(true, Color.White)));
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseQCH" + Id, "Charge Q while Dashing").SetValue(true));
            config.AddItem(
                new MenuItem("UseQHT" + Id, "Auto Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle)));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("rmode" + Id, "Last Breath (R) Mode")
                .SetValue(new StringList(new[] { "Multi-target", "Single-target", "Both" }, 2)));
            config.AddItem(new MenuItem("renemieshealthper" + Id, "[Last Breath] Min. Multi Health %").SetValue(new Slider(40)));
            config.AddItem(new MenuItem("renemyhealthper" + Id, "[Last Breath] Min. Single Health %").SetValue(new Slider(40)));
            config.AddItem(new MenuItem("rmin" + Id, "[Last Breath] Min. Multi-Enemy to Use R").SetValue(new Slider(3, 1, 5)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
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
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true,
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

            config.AddItem(new MenuItem("autoww" + Id, "Use Auto Windwall")).SetValue(true);
            config.AddItem(new MenuItem("wwdelay" + Id, "Windwall Delay")).SetValue(new Slider(500, 150, 2000));

            var enemies = ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsEnemy);

            foreach (
                var spell in
                    enemies.SelectMany(e1 => SpellDatabase.Spells.Where(s => s.ChampionName == e1.BaseSkinName)))
            {
                // => Windwall
                if (spell.CollisionObjects.Any(e2 => e2 == CollisionObjectTypes.YasuoWall))
                {
                    var spellActualName = spell.ChampionName;
                    var slot = "?";
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            spellActualName += " Q";
                            slot = "Q";
                            break;
                        case SpellSlot.W:
                            spellActualName += " W";
                            slot = "W";
                            break;
                        case SpellSlot.E:
                            spellActualName += " E";
                            slot = "E";
                            break;
                        case SpellSlot.R:
                            spellActualName += " R";
                            slot = "R";
                            break;
                    }
                    var theSpell = new MenuData
                    {
                        ChampionName = spell.ChampionName,
                        SpellName = spell.SpellName,
                        SpellDisplayName = spellActualName,
                        DisplayName = spellActualName,
                        Slot = slot,
                        IsWindwall = true
                    };
                    theSpell.AddToMenu(config, Id);
                    MenuWallsList.Add(theSpell);
                }
            }
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {

            return true;
        }
        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQL" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEQL" + Id, "Use EQ").SetValue(true));
            config.AddItem(new MenuItem("ELmode" + Id, "Dash Mode")
                .SetValue(new StringList(new[] { "Off", "Last-Hit Only", "Towards Mouse" }, 2)));            
            return true;
        }

    }

    public class YasuoEvade
    {
        public YasuoEvade()
        {
            SkillshotDetector.OnDetectSkillshot += Evade_OnDetectSkillshot;
            SkillshotDetector.OnDeleteMissile += Evade_OnDeleteMissile;
        }

        private static void Evade_OnDetectSkillshot(Skillshot skillshot)
        {
            //Check if the skillshot is already added.
            var alreadyAdded = false;

            foreach (var item in Yasuo.EvadeDetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                }
            }

            //Check if the skillshot is from an ally.
            if (skillshot.Unit.Team == ObjectManager.Player.Team)
            {
                return;
            }

            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(ObjectManager.Player.ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }

            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded)
            {
                //Multiple skillshots like twisted fate Q.
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        const int angle = 60;
                        const int fraction = -angle / 2;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                fraction * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float)Math.PI / 180);

                        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            var v = minion.ServerPosition.To2D() - skillshot.Unit.ServerPosition.To2D();
                            if (minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Unit) < 800 &&
                                (minion.Team != ObjectManager.Player.Team))
                            {
                                var start = minion.ServerPosition.To2D();
                                var end = skillshot.Unit.ServerPosition.To2D()
                                    .Extend(
                                        minion.ServerPosition.To2D(),
                                        skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000);

                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Unit);
                                Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;


                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");

                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        Yasuo.EvadeDetectedSkillshots.Add(bounce1);
                        Yasuo.EvadeDetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }

                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var endCSpellData = SpellDatabase.GetByName("OriannaQend");

                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, endCSpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                }


                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }

                Yasuo.EvadeDetectedSkillshots.Add(skillshot);
            }
        }

        private static void Evade_OnDeleteMissile(Skillshot skillshot, Obj_SpellMissile missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();
                if (Yasuo.EvadeDetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (var i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Environment.TickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i * direction * spellData.Range, skillshot.Unit);
                        Yasuo.EvadeDetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }
    }
}
