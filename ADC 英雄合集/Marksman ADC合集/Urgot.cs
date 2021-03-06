#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace Marksman
{
    internal class Urgot : Champion
    {
        public static Spell Q, QEx, W, E, R;

        public Urgot()
        {
            Utils.PrintMessage("Urgot loaded.");

            Q = new Spell(SpellSlot.Q, 1000);
            QEx = new Spell(SpellSlot.Q, 1600) { MinHitChance = HitChance.Collision };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 900);
            R = new Spell(SpellSlot.R, 700);

            Q.SetSkillshot(0.10f, 100f, 1600f, true, SkillshotType.SkillshotLine);
            QEx.SetSkillshot(0.10f, 60f, 1600f, false, SkillshotType.SkillshotLine);

            E.SetSkillshot(0.283f, 0f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetTargetted(1f, 100f);
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.CastOnUnit(gapcloser.Sender);
        }

        public static bool UnderAllyTurret(Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where<Obj_AI_Turret>((turret) =>
            {
                if (turret == null || !turret.IsValid || turret.Health <= 0f)
                {
                    return false;
                }
                if (!turret.IsEnemy)
                {
                    return true;
                }
                return false;
            })
                .Any<Obj_AI_Turret>(
                    (turret) =>
                        SharpDX.Vector2.Distance(unit.Position.To2D(), turret.Position.To2D()) < 900f && turret.IsAlly);
        }

        public static bool TeleportTurret(Obj_AI_Hero vTarget)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Any(player => !player.IsDead && player.IsMe && UnderAllyTurret(ObjectManager.Player));
        }

        public static int UnderTurretEnemyMinion
        {
            get
            {
                return ObjectManager.Get<Obj_AI_Minion>().Count(xMinion => xMinion.IsEnemy && UnderAllyTurret(xMinion));
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, QEx, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var drawQEx = GetValue<Circle>("DrawQEx");
            if (drawQEx.Active)
            {
                foreach (
                    var enemy in
                        from enemy in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(
                                    enemy =>
                                        enemy.IsEnemy && ObjectManager.Player.Distance(enemy) <= QEx.Range &&
                                        enemy.HasBuff("urgotcorrosivedebuff", true))
                        select enemy)
                {
                    Utility.DrawCircle(enemy.Position, 75f, drawQEx.Color);
                }
            }
        }

        private static void UseSpells(bool useQ, bool useW, bool useE)
        {
            Obj_AI_Hero t;

            if (Q.IsReady() && useQ)
            {
                t = SimpleTs.GetTarget(QEx.Range, SimpleTs.DamageType.Physical);
                if (t != null && t.HasBuff("urgotcorrosivedebuff", true))
                {
                    W.Cast();
                    QEx.Cast(t);
                }
                else
                {
                    t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                    if (t != null)
                    {
                        if (Q.GetPrediction(t).Hitchance >= HitChance.High)
                            W.Cast();
                        Q.Cast(t);
                    }
                }
            }

            if (W.IsReady() && useW)
            {
                t = SimpleTs.GetTarget(ObjectManager.Player.AttackRange - 30, SimpleTs.DamageType.Physical);
                if (t != null)
                    W.Cast();
            }

            if (E.IsReady() && useE)
            {
                t = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
                if (t != null)
                    E.Cast(t);
            }
        }

        private static void UltUnderTurret()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            Drawing.DrawText(Drawing.Width * 0.41f, Drawing.Height * 0.80f, Color.GreenYellow,
                "Teleport enemy to under ally turret active!");

            if (R.IsReady() && Program.CClass.GetValue<bool>("UseRC"))
            {
                var t = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                if (t != null && UnderAllyTurret(ObjectManager.Player) && !UnderAllyTurret(t) &&
                    ObjectManager.Player.Distance(t) > 200)
                {
                    R.CastOnUnit(t);
                }
            }

            UseSpells(Program.CClass.GetValue<bool>("UseQC"), Program.CClass.GetValue<bool>("UseWC"),
                Program.CClass.GetValue<bool>("UseEC"));
        }
        private static void UltInMyTeam()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            Drawing.DrawText(Drawing.Width * 0.42f, Drawing.Height * 0.80f, Color.GreenYellow,
            "Teleport enemy to my team active!");

            var t = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            if (R.IsReady() && t != null)
            {
                IEnumerable<Obj_AI_Hero> Ally =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            ally =>
                                ally.IsAlly && !ally.IsDead && ObjectManager.Player.Distance(ally) <= R.Range &&
                                t.Distance(ally) > t.Distance(ObjectManager.Player));

                if (Ally.Count() >= Program.CClass.GetValue<Slider>("UltOp2Count").Value)
                    R.CastOnUnit(t);

            }

            UseSpells(Program.CClass.GetValue<bool>("UseQC"), Program.CClass.GetValue<bool>("UseWC"),
                Program.CClass.GetValue<bool>("UseEC"));
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            R.Range = 150 * R.Level + 400;

            if (GetValue<KeyBind>("UltOp1").Active)
            {
                UltUnderTurret();
            }

            if (GetValue<KeyBind>("UltOp2").Active)
            {
                UltInMyTeam();
            }

            if ((ComboActive && !HarassActive) || Orbwalking.CanMove(100))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseWC");
                var useE = GetValue<bool>("UseEC");

                UseSpells(useQ, useW, useE);
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

        }

        public override void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if ((!ComboActive && !HarassActive) || unit.IsMe || (!(target is Obj_AI_Hero))) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            if (useQ)
                Q.Cast(target, false, true);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "使用 Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "使用 W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "使用 E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "使用 R").SetValue(true));


            config.AddSubMenu(new Menu("大招设置 1", "UltOpt1"));

            config.SubMenu("UltOpt1")
                .AddItem(
                    new MenuItem("UltOp1" + Id, "R到友方炮塔").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));

            config.AddSubMenu(new Menu("大招设置 2", "UltOpt2"));
            config.SubMenu("UltOpt2")
                .AddItem(
                    new MenuItem("UltOp2" + Id, "R到友方阵容").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Press)));
            config.SubMenu("UltOpt2")
                .AddItem(new MenuItem("UltOp2Count" + Id, "使用R|身边最少队友").SetValue(new Slider(1, 1, 5)));


            config.AddSubMenu(new Menu("不使用 R", "DontUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                config.SubMenu("DontUlt")
                    .AddItem(
                        new MenuItem(string.Format("DontUlt{0}", enemy.BaseSkinName), enemy.BaseSkinName).SetValue(false));
            }

            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "U使用 Q").SetValue(true));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "使用 Q (自动)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle)));

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q 范围").SetValue(new Circle(true, Color.LightGray)));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E 范围").SetValue(new Circle(false, Color.LightGray)));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R 范围").SetValue(new Circle(false, Color.LightGray)));
            config.AddItem(
                new MenuItem("DrawQEx" + Id, "腐蚀电荷（E)").SetValue(new Circle(true, Color.LightGray)));

            return true;
        }

        public override bool MiscMenu(Menu config)
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
