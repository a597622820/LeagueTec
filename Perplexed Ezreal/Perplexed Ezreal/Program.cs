﻿using System;
using System.Drawing;
using Aimtec;
using Aimtec.SDK.Menu.Components;
using Aimtec.SDK.Events;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.TargetSelector;
using Aimtec.SDK.Extensions;
using Aimtec.SDK.Damage;
using Aimtec.SDK.Prediction.Skillshots;
using System.Linq;
using Aimtec.SDK.Util.Cache;

namespace Perplexed_Ezreal
{
    class Program
    {
        public static Obj_AI_Hero Player;
        static void Main(string[] args)
        {
            GameEvents.GameStart += GameEvents_GameStart;
        }

        private static void GameEvents_GameStart()
        {
            Player = ObjectManager.GetLocalPlayer();
            if (Player.ChampionName != "Ezreal")
                return;

            MenuManager.Initialize();
            SpellManager.Initialize();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.Implementation.OnNonKillableMinion += Implementation_OnNonKillableMinion;
        }

        private static void Game_OnUpdate()
        {
            if (MenuManager.Ultimate["ultSemi"].As<MenuKeyBind>().Enabled)
                SemiCastUlt();
            if (Orbwalker.Implementation.IsWindingUp)
                return;
            AutoHarass();
            switch (Orbwalker.Implementation.Mode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case OrbwalkingMode.Laneclear:
                case OrbwalkingMode.Lasthit:
                    LastHitting();
                    break;
            }
        }

        private static void Combo()
        {
            if (MenuManager.Combo["comboR"].As<MenuBool>().Enabled && SpellManager.R.Ready)
            {

                var target = TargetSelector.GetOrderedTargets(MenuManager.Ultimate["ultMinRange"].As<MenuSlider>().Value).FirstOrDefault(x => Player.GetSpellDamage(x, SpellSlot.R) >= x.Health);
                if (target.IsValidTarget())
                {
                    var pred = SpellManager.R.GetPrediction(target);
                    var collisions = pred.CollisionObjects.Count > 7 ? 7 : pred.CollisionObjects.Count; //Can't have the damage reduced by more than 70%.
                    var ultDamage = Player.GetSpellDamage(target, SpellSlot.R);
                    if (collisions > 0)
                    {
                        float multiplier = collisions / 10;
                        ultDamage = ultDamage - (ultDamage * multiplier);
                    }
                    if (ultDamage >= target.Health)
                        SpellManager.R.Cast(target);
                }

            }
            if (MenuManager.Combo["comboQ"].As<MenuBool>().Enabled && SpellManager.Q.Ready)
            {
                var target = TargetSelector.GetTarget(SpellManager.Q.Range);
                var pred = SpellManager.Q.GetPrediction(target);
                if (target.IsValidTarget() && pred.HitChance >= HitChance.High)
                    SpellManager.Q.Cast(target);
            }
            if (MenuManager.Combo["comboW"].As<MenuBool>().Enabled && SpellManager.W.Ready)
            {
                var target = TargetSelector.GetTarget(SpellManager.W.Range);
                var pred = SpellManager.W.GetPrediction(target);
                if (target.IsValidTarget() && pred.HitChance >= HitChance.High)
                    SpellManager.W.Cast(target);
            }
        }

        private static void Harass()
        {
            var minManaPct = MenuManager.Harass["harassManaPct"].As<MenuSlider>().Value;
            if (MenuManager.Harass["harassQ"].As<MenuBool>().Enabled && SpellManager.Q.Ready && Player.ManaPercent() >= minManaPct)
            {
                var target = TargetSelector.GetTarget(SpellManager.Q.Range);
                var pred = SpellManager.Q.GetPrediction(target);
                if (target.IsValidTarget() && pred.HitChance >= HitChance.High)
                    SpellManager.Q.Cast(target);
            }
            if (MenuManager.Harass["harassW"].As<MenuBool>().Enabled && SpellManager.W.Ready && Player.ManaPercent() >= minManaPct)
            {
                var target = TargetSelector.GetTarget(SpellManager.W.Range);
                var pred = SpellManager.W.GetPrediction(target);
                if (target.IsValidTarget() && pred.HitChance >= HitChance.High)
                    SpellManager.W.Cast(target);
            }
        }
        private static void AutoHarass()
        {
            if (Player.IsRecalling())
                return;
            var minManaPct = MenuManager.AutoHarass["autoHarassManaPct"].As<MenuSlider>().Value;
            if (MenuManager.AutoHarass["autoHarassQ"].As<MenuBool>().Enabled && SpellManager.Q.Ready && Player.ManaPercent() >= minManaPct)
            {
                var target = TargetSelector.GetTarget(SpellManager.Q.Range);
                var pred = SpellManager.Q.GetPrediction(target);
                if (target.IsValidTarget() && !Player.IsUnderEnemyTurret() && MenuManager.AutoHarassWhitelist[target.ChampionName].As<MenuBool>().Enabled && pred.HitChance >= HitChance.High)
                    SpellManager.Q.Cast(target);
            }
            if (MenuManager.AutoHarass["autoHarassW"].As<MenuBool>().Enabled && SpellManager.W.Ready && Player.ManaPercent() >= minManaPct)
            {
                var target = TargetSelector.GetTarget(SpellManager.W.Range);
                var pred = SpellManager.W.GetPrediction(target);
                if (target.IsValidTarget() && !Player.IsUnderEnemyTurret() && MenuManager.AutoHarassWhitelist[target.ChampionName].As<MenuBool>().Enabled && pred.HitChance >= HitChance.High)
                    SpellManager.W.Cast(target);
            }
        }

        private static void LastHitting()
        {
            var minManaPct = MenuManager.LastHitting["lastHitManaPct"].As<MenuSlider>().Value;
            if (MenuManager.LastHitting_Q["lastHittingQ"].As<MenuBool>().Enabled && SpellManager.Q.Ready && Player.ManaPercent() >= minManaPct)
            {
                var onlyOutOfRange = MenuManager.LastHitting_Q["lastHittingQOutOfRange"].As<MenuBool>().Enabled;
                var minions = GameObjects.EnemyMinions.Where(x => x.IsInRange(SpellManager.Q.Range) && x.UnitSkinName.Contains("Minion") && Player.GetSpellDamage(x, SpellSlot.Q) >= x.Health && x.IsValidTarget()).OrderBy(x => x.Health);
                var target = onlyOutOfRange ? minions.FirstOrDefault(x => !x.IsInRange(Player.AttackRange)) : minions.FirstOrDefault();
                if (target.IsValidTarget())
                    SpellManager.Q.Cast(target);
            }
        }

        private static void SemiCastUlt()
        {
            if (SpellManager.R.Ready)
            {
                var target = TargetSelector.GetTarget(MenuManager.Ultimate["ultMinRange"].As<MenuSlider>().Value);
                if (target.IsValidTarget())
                    SpellManager.R.Cast(target);
            }
        }

        private static void Implementation_OnNonKillableMinion(object sender, NonKillableMinionEventArgs e)
        {
            var minion = e.Target as Obj_AI_Minion;
            if (minion == null)
                return;
            switch (Orbwalker.Implementation.Mode)
            {
                case OrbwalkingMode.Lasthit:
                case OrbwalkingMode.Laneclear:
                    var minManaPct = MenuManager.LastHitting["lastHitManaPct"].As<MenuSlider>().Value;
                    if (MenuManager.LastHitting_Q["lastHittingQWillDie"].As<MenuBool>().Enabled && SpellManager.Q.Ready && Player.GetSpellDamage(minion, SpellSlot.Q) >= minion.Health && Player.ManaPercent() >= minManaPct)
                        SpellManager.Q.Cast(minion);
                    break;
            }
        }

        private static void Render_OnPresent()
        {
            if (MenuManager.Drawing["drawQ"].As<MenuBool>().Enabled)
                Render.Circle(Player.Position, SpellManager.Q.Range, 30, Color.White);
            if (MenuManager.Drawing["drawW"].As<MenuBool>().Enabled)
                Render.Circle(Player.Position, SpellManager.W.Range, 30, Color.White);
            if (MenuManager.Drawing["drawR"].As<MenuBool>().Enabled)
            {
                var ultRange = MenuManager.Ultimate["ultMinRange"].As<MenuSlider>().Value;
                Render.Circle(Player.Position, ultRange, 30, Color.Red);
            }
        }


    }
}
