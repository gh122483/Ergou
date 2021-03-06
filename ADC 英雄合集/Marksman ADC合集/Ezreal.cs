#region

using System;
using System.Configuration;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman
{
    internal class Ezreal : Champion
    {
        public Spell Q;
        public Spell R;
        public Spell W;

        public static Items.Item Dfg = new Items.Item(3128, 750);
        public Ezreal()
        {
            Utils.PrintMessage("Ezreal loaded.");

            Q = new Spell(SpellSlot.Q, 1200);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 950);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 2500);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

        }

        public override void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if ((ComboActive || HarassActive) && unit.IsMe && (target is Obj_AI_Hero))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                {
                    Q.Cast(target);
                }
                else if (W.IsReady() && useW)
                {
                    W.Cast(target);
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, W };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero t;

            if (Q.IsReady() &&  GetValue<KeyBind>("UseQTH").Active && ToggleActive)
            {
                if(ObjectManager.Player.HasBuff("Recall"))
                    return;
                t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                if (t != null)
                    Q.Cast(t);
            }

            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Orbwalking.CanMove(100))
                {
                    if (Dfg.IsReady())
                    {
                        t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
                        Dfg.Cast(t);
                    }

                    if (Q.IsReady() && useQ)
                    {
                        t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                        if (t != null)
                            Q.Cast(t);
                    }

                    if (W.IsReady() && useW)
                    {
                        t = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
                        if (t != null)
                            W.Cast(t);
                    }
                }
            }

            if (LaneClearActive)
            {
                bool useQ = GetValue<bool>("UseQL");

                if (Q.IsReady() && useQ)
                {
                    var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                    foreach (
                        Obj_AI_Base minions in
                            vMinions.Where(
                                minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q)))
                        Q.Cast(minions);
                }
            }


            if (!R.IsReady() || !GetValue<KeyBind>("CastR").Active) return;
            t = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            if (t != null)
                R.Cast(t);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "使用Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "使用W").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "使用Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "使用W").SetValue(true));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "使用Q (自动)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle)));
            return true;
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("CastR" + Id, "手动 R (2000 范围)").SetValue(new KeyBind("T".ToCharArray()[0],
                    KeyBindType.Press)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q 范围").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W 范围").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {

            return true;
        }
        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQL" + Id, "使用 Q").SetValue(true));
            return true;
        }

    }
}
