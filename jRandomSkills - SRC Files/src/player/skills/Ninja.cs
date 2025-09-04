using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Ninja : ISkill
    {
        private const Skills skillName = Skills.Ninja;
        private static float idlePercentInvisibility = Config.GetValue<float>(skillName, "idlePercentInvisibility");
        private static float duckPercentInvisibility = Config.GetValue<float>(skillName, "duckPercentInvisibility");
        private static float knifePercentInvisibility = Config.GetValue<float>(skillName, "knifePercentInvisibility");
        private static Dictionary<nint, float> invisibilityChanged = new Dictionary<nint, float>();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                    DisableSkill(player);
                invisibilityChanged.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid || player.PlayerPawn.Value == null) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    DisableSkill(player);

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventItemPickup>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                UpdateNinja(player);
                return HookResult.Continue;
            });

            Instance.RegisterListener<OnTick>(OnTick);
        }

        private static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    UpdateNinja(player);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SetPlayerVisibility(player, 0);
            SetWeaponVisibility(player, 0);
        }

        private static void UpdateNinja(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;
            var flags = (PlayerFlags)pawn.Flags;
            var buttons = player.Buttons;

            if (pawn.WeaponServices == null) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;

            if (activeWeapon == null) return;

            float percentInvisibility = 0;

            if (buttons.HasFlag(PlayerButtons.Duck))
                percentInvisibility += duckPercentInvisibility;
            if (activeWeapon.DesignerName == "weapon_knife")
                percentInvisibility += knifePercentInvisibility;
            if (!buttons.HasFlag(PlayerButtons.Moveleft) && !buttons.HasFlag(PlayerButtons.Moveright) && !buttons.HasFlag(PlayerButtons.Forward) && !buttons.HasFlag(PlayerButtons.Back) && flags.HasFlag(PlayerFlags.FL_ONGROUND))
                percentInvisibility += idlePercentInvisibility;

            if (invisibilityChanged.TryGetValue(player.Handle, out float oldInvisibility))
                if (percentInvisibility == oldInvisibility)
                    return;

            invisibilityChanged[player.Handle] = percentInvisibility;
            SetPlayerVisibility(player, percentInvisibility);
            SetWeaponVisibility(player, percentInvisibility);
        }

        private static void SetPlayerVisibility(CCSPlayerController player, float percentInvisibility)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null)
            {
                var color = Color.FromArgb(Math.Max(255 - (int)(255 * percentInvisibility), 0), 255, 255, 255);
                playerPawn.Render = color;
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            }
        }

        private static void SetWeaponVisibility(CCSPlayerController player, float percentInvisibility)
        {
            if (!Instance.IsPlayerValid(player)) return;
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null || !playerPawn.IsValid) return;

            var color = Color.FromArgb(Math.Max(255 - (int)(255 * percentInvisibility * 2), 0), 255, 255, 255);

            var weaponServices = playerPawn.WeaponServices;

            if (weaponServices == null) return;

            foreach (var weapon in weaponServices.MyWeapons)
            {
                if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                {
                    weapon.Value.Render = color;
                    Utilities.SetStateChanged(weapon.Value, "CBaseModelEntity", "m_clrRender");
                }
            }
        }

        public class SkillConfig : Config.DefaultSkillInfo
        {
            public float IdlePercentInvisibility { get; set; }
            public float DuckPercentInvisibility { get; set; }
            public float KnifePercentInvisibility { get; set; }
            public SkillConfig(Skills skill = skillName, bool active = true, string color = "#dedede", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float idlePercentInvisibility = .3f, float duckPercentInvisibility = .3f, float knifePercentInvisibility = .3f) : base(skill, active, color, onlyTeam, needsTeammates)
            {
                IdlePercentInvisibility = idlePercentInvisibility;
                DuckPercentInvisibility = duckPercentInvisibility;
                KnifePercentInvisibility = knifePercentInvisibility;
            }
        }
    }
}