#region

using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

#endregion

namespace GodlikeSkillz
{
    internal class Thresh : Champion
    {
        public Spell E;
        public Spell Q;
        public static bool ECast;

        public Thresh()
        {
            Utils.PrintMessage("Thresh loaded");
            //Orbwalk = false;
            E = new Spell(SpellSlot.E, 500f);
            Q = new Spell(SpellSlot.Q, 500f);
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
        }

        public override void Drawing_OnDraw(EventArgs args)
        {           
            Spell[] spellList = { E };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
            var autoItem = GetValue<Circle>("DrawAuto");
            if (autoItem.Active)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Orbwalking.GetRealAutoAttackRange(null) + 65, autoItem.Color);
           
        }

        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs castedSpell)
        {
            if (!unit.IsMe)
                return;

            if (castedSpell.SData.Name == Player.Spellbook.GetSpell(SpellSlot.W).Name)
            {                
                var target = TargetSelector.GetTarget(300, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    ECast = true;
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
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (GetValue<KeyBind>("Flay").Active)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (E.IsReady())
                {
                    E.Cast(Prediction.GetPrediction(t, 0.25f).UnitPosition.Extend(Player.Position, 800f));
                }
                /*if (Q.IsReady())
                {
                    Q.Cast();
                }*/
            }

            var tiamatId = ItemData.Tiamat_Melee_Only.Id;
            var hydraId = ItemData.Ravenous_Hydra_Melee_Only.Id;
            var hasTiamat = Items.HasItem(tiamatId);
            var hasHydra = Items.HasItem(hydraId);
            var itemId = hasTiamat ? tiamatId : hydraId;
            var g = new Spell(GetItemSlot(itemId).SpellSlot);
            if (ECast)
            {
                if (hasHydra || hasTiamat)
                {
                    Items.UseItem(itemId);
                }
            }
            if (!g.IsReady())
            {
                ECast = false;
            }
        }

        private static InventorySlot GetItemSlot(int id)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId)id);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("Flay" + Id, "Flay Back").SetValue(new KeyBind(32, KeyBindType.Press)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawAuto" + Id, "Attack range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 255))));
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
