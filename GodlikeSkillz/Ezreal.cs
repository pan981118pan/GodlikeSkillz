#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;
#endregion

namespace GodlikeSkillz
{
    internal class Ezreal : Champion
    {
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Spell R;
        public static Font VText;

        public Ezreal()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 2500f);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            VText = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Courier new",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });

            Utils.PrintMessage("Ezreal loaded.");
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {

            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, W };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (GetValue<bool>("DrawHarassToggleStatus"))
            {
                DrawHarassToggleStatus();
            }

            if (GetValue<bool>("ShowKillableStatus"))
            {
                ShowKillableStatus();
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (Q.IsReady() && GetValue<bool>("UseQM"))
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(Q.Range) &&
                                    GetQDamage(hero) > hero.Health))
                {
                    if (Config.Item("MURAMANA").GetValue<bool>() &&
                        (Items.HasItem(ItemData.Muramana.Id) || Items.HasItem(ItemData.Muramana2.Id)))
                    {
                        if (!Player.HasBuff("Muramana", true))
                        {
                            Items.UseItem(ItemData.Muramana.Id);
                            Items.UseItem(ItemData.Muramana2.Id);
                        }
                    }
                    Q.CastIfHitchanceEquals(hero, HitChance.VeryHigh);                
                }
            }

           
            if (LaneClearActive)
            {
                var useQ = GetValue<bool>("UseQL");

                if (Q.IsReady() && useQ)
                {
                    var vMinions = MinionManager.GetMinions(Player.Position, Q.Range);
                    foreach (var minions in
                        vMinions.Where(
                            minions =>
                                minions.Health < GetQDamage(minions) &&
                                (Orbwalker.GetTarget() == null || minions.ToString() != Orbwalker.GetTarget().ToString()))
                        )
                    {
                        Q.Cast(minions);
                    }
                }
            }

            var harassQ = GetValue<KeyBind>("UseQHT").Active;
            if (ComboActive || HarassActive || (harassQ && !Player.HasBuff("Recall")))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                {
                    var t = (Obj_AI_Hero)Orbwalker.GetTarget() ??
                        TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                    if (t != null && (Player.Distance(t) > Orbwalking.GetRealAutoAttackRange(t) || (AttackReadiness > 0.2 && AttackReadiness < 0.75)))
                    {                        
                        CastQ(t, GetValue<Slider>("QHit").Value);
                    }
                }               
                else if (W.IsReady() && useW && Player.ManaPercentage() >= GetValue<Slider>("WlimM").Value)
                {
                    var t = (Obj_AI_Hero)Orbwalker.GetTarget() ??
                        TargetSelector.GetTarget(700, TargetSelector.DamageType.Magical);
                    if (t != null && (Player.Distance(t) > Orbwalking.GetRealAutoAttackRange(t) || (AttackReadiness > 0.2 && AttackReadiness < 0.75)))
                    {
                        W.Cast(t);
                    }
                }
            }

            if (WhichTear() != 0)
            {
                var g = new Spell(GetItemSlot(WhichTear()).SpellSlot);
                if (Q.IsReady() && GetValue<KeyBind>("StackT").Active &&
                    Math.Max(g.Instance.CooldownExpires - Game.Time, 0) <= 0)
                {
                    if (Player.HasBuff("Recall"))
                        return;
                    Q.Cast(Game.CursorPos);
                }
            }

        }

        public static void CastSpellQShot(Obj_AI_Base target, int spellAoE)
        {
            var po = Q.GetPrediction(target);
            if (po.CollisionObjects.Count > 0)
            {
                var firstCol = po.CollisionObjects.OrderBy(unit => unit.Distance(Player.ServerPosition)).First();
                if (firstCol.IsValidTarget() && (/*firstCol.Distance(target.ServerPosition) < spellAoE ||*/ firstCol.Distance(po.UnitPosition) < spellAoE))
                {
                    if(firstCol.Type == GameObjectType.obj_AI_Hero)
                        if (Program.Config.Item("MURAMANA").GetValue<bool>() && (Items.HasItem(ItemData.Muramana.Id) || Items.HasItem(ItemData.Muramana2.Id)))
                        {
                            if (!Player.HasBuff("Muramana", true))
                            {
                                Items.UseItem(ItemData.Muramana.Id);
                                Items.UseItem(ItemData.Muramana2.Id);
                            }
                        }
                    Q.Cast(po.CastPosition);
                }
            }
            else
            {
                if (Program.Config.Item("MURAMANA").GetValue<bool>() && (Items.HasItem(ItemData.Muramana.Id) || Items.HasItem(ItemData.Muramana2.Id)))
                {
                    if (!Player.HasBuff("Muramana", true))
                    {
                        Items.UseItem(ItemData.Muramana.Id);
                        Items.UseItem(ItemData.Muramana2.Id);
                    }
                }
                Q.Cast(po.CastPosition);
            }
        }

        private static void QSpell(Obj_AI_Base target)
        {
            if (Items.HasItem(ItemData.Iceborn_Gauntlet.Id))
                CastSpellQShot(target, 200);
            else
            {
                if (Program.Config.Item("MURAMANA").GetValue<bool>() && (Items.HasItem(ItemData.Muramana.Id) || Items.HasItem(ItemData.Muramana2.Id)))
                {
                    if (!Player.HasBuff("Muramana", true))
                    {
                        Items.UseItem(ItemData.Muramana.Id);
                        Items.UseItem(ItemData.Muramana2.Id);
                    }
                }
                Q.Cast(target);
            }
        }

        private static void CastQ(Obj_AI_Base target, int hitChanceNum)
        {
            switch (hitChanceNum)
            {
                case 0:
                    QSpell(target);
                    break;
                case 1:
                {
                    Q.Collision = false;
                    var chance = Q.GetPrediction(target).Hitchance;
                    if (chance >= HitChance.VeryHigh)
                    {
                        Q.Collision = true;
                        QSpell(target);
                    }              
                }
                    break;
                case 2:
                {
                    if (target.Path.Count() >= 2)
                    {
                        return;
                    }
                    Q.Collision = false;
                    var chance = Q.GetPrediction(target).Hitchance;
                    if (chance >= HitChance.VeryHigh)
                    {
                        Q.Collision = true;
                        QSpell(target);
                    }
                }
                    break;
            }
        }

        private static InventorySlot GetItemSlot(int id)
        {
            return Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId) id);
        }

        private static int WhichTear()
        {
            if (Items.HasItem(ItemData.Tear_of_the_Goddess.Id))
                return ItemData.Tear_of_the_Goddess.Id;
            if (Items.HasItem(ItemData.Tear_of_the_Goddess_Crystal_Scar.Id))
                return ItemData.Tear_of_the_Goddess_Crystal_Scar.Id;
            if (Items.HasItem(ItemData.Manamune.Id))
                return ItemData.Manamune.Id;
            if (Items.HasItem(ItemData.Manamune_Crystal_Scar.Id))
                return ItemData.Manamune_Crystal_Scar.Id;
            return 0;
        }

        private static float GetQDamage(Obj_AI_Base t)
        {
            var qDamage = 0f;
            var iDamage = 0f;
            var mDamage = 0f;
            var damageType = Damage.DamageType.Physical;

            if (Q.IsReady())
                qDamage += (float)Player.GetSpellDamage(t, SpellSlot.Q);

            if (Items.HasItem(ItemData.Lich_Bane.Id))
            {
                iDamage += Player.BaseAbilityDamage;
                damageType = Damage.DamageType.Magical;
            }
            else if (Items.HasItem(ItemData.Trinity_Force.Id))
            {
                iDamage += (Player.BaseAttackDamage * 2f);
            }
            else if (Items.HasItem(ItemData.Iceborn_Gauntlet.Id))
            {
                iDamage += (Player.BaseAttackDamage * 1.25f);
            }
            else if (Items.HasItem(ItemData.Sheen.Id))
            {
                iDamage += Player.BaseAttackDamage;
            }

            if (t.Type == GameObjectType.obj_AI_Hero && 
                (Items.HasItem(ItemData.Muramana.Id) || Items.HasItem(ItemData.Muramana2.Id)))
            {
                mDamage = Player.Mana * 0.06f;
            }

            return qDamage + (float)Player.CalcDamage(t, damageType, iDamage) + (float)Player.CalcDamage(t, Damage.DamageType.Physical, mDamage);
        }

        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (Q.IsReady())
                fComboDamage += (float)Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += (float)Player.GetSpellDamage(t, SpellSlot.W);

            if (E.IsReady())
                fComboDamage += (float)Player.GetSpellDamage(t, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += (float)Player.GetSpellDamage(t, SpellSlot.R);

            if (Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) ==
                SpellState.Ready && Player.Distance(t) < 550)
                fComboDamage += (float)Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3144) && Player.Distance(t) < 550)
                fComboDamage += (float)Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(3153) && Player.Distance(t) < 550)
                fComboDamage += (float)Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            if (Items.CanUseItem(3128) && Player.Distance(t) < 550)
                fComboDamage += (float)Player.GetItemDamage(t, Damage.DamageItems.Dfg);

            return fComboDamage;
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "W").SetValue(true));
            config.AddItem(new MenuItem("QHit" + Id, "Q Hitchance").SetValue(new Slider(2, 0, 2)));
            config.AddItem(new MenuItem("WlimM" + Id, "W Mana Limiter").SetValue(new Slider(25, 0, 50)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Q").SetValue(true));
            config.AddItem(
                new MenuItem("UseQHT" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle)));
            config.AddItem(new MenuItem("UseWH" + Id, "W").SetValue(true));
            config.AddItem(new MenuItem("DrawHarassToggleStatus" + Id, "Draw Toggle Status").SetValue(true));
            return true;
        }

        private void DrawHarassToggleStatus()
        {
            var xHarassStatus = "";
            if (GetValue<KeyBind>("UseQHT").Active)
                xHarassStatus = "Q";

            Utils.DrawText(
                VText, xHarassStatus, (int)Player.HPBarPosition.X + 145,
                (int)Player.HPBarPosition.Y + 5, SharpDX.Color.White);
        }

        private static void ShowKillableStatus()
        {
            var t = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget(2000) && t.Health < GetComboDamage(t))
            {
                const string xComboText = ">> Kill <<";
                Utils.DrawText(
                    VText, xComboText, (int)t.HPBarPosition.X + 145, (int)t.HPBarPosition.Y + 5, SharpDX.Color.White);
            }
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(
                    new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(
                    new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(new MenuItem("ShowKillableStatus" + Id, "Show Killable Status").SetValue(true));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo" + Id, "Damage After Combo").SetValue(true);

            Config.AddItem(dmgAfterComboItem);
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQM" + Id, "Use Q To Killsteal").SetValue(true));
            config.AddItem(
                new MenuItem("StackT" + Id, "Stack Tear").SetValue(
                    new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQL" + Id, "Use Q").SetValue(true));
            return true;
        }

    }
}
