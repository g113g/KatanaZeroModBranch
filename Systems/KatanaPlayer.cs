using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using KatanaZERO.Items.FifteensBlade;

namespace KatanaZERO.Systems
{
    public class KatanaPlayer : ModPlayer
    {
        // Standalone Chronos Battery pool (11 segments, 10 units each = 110 max)
        public const float MaxBattery = 110f;
        public const float DashBatteryCost = 0f; // Dash does NOT cost battery in Katana Zero
        public const float SlowMoDrainPerTick = 0.26f; // ~7.05 seconds of continuous slow-mo
        public const float BatteryRechargePerTick = 0.60f; // ~3 seconds to recharge full from empty

        public const int MaxDashCooldown = 180; // 3 seconds at 60fps
        public int DragonDashCooldown = 0;

        public float Battery = MaxBattery;
        public int RechargeCooldown = 0;

        public bool IsHoldingFifteen = false;
        public bool IsHoldingAnyKatana = false;
        public bool IsSlowingTime = false;
        public float SlowMoVisualIntensity = 0f;

        // Dictionaries for tracking slowed mob and projectile speeds
        public Dictionary<int, float> OriginalNPCSpeeds = new Dictionary<int, float>();
        public Dictionary<int, float> OriginalProjectileSpeeds = new Dictionary<int, float>();

        // Active ghost silhouettes along dash trajectory
        public List<GhostSilhouette> ActiveGhosts = new List<GhostSilhouette>();

        // Cached snapshot of player DrawData
        public List<DrawData> CapturedPlayerDrawData = new List<DrawData>();
        public Vector2 CapturedPlayerPosition = Vector2.Zero;
        public Vector2 CapturedScreenPosition = Vector2.Zero;
        public int CapturedPlayerDirection = 1;
        public int LastGhostDrawDataCount = 0;

        public static float LocalSlowMoIntensity => Main.LocalPlayer?.GetModPlayer<KatanaPlayer>()?.SlowMoVisualIntensity ?? 0f;
        public static bool LocalIsSlowingTime => Main.LocalPlayer?.GetModPlayer<KatanaPlayer>()?.IsSlowingTime ?? false;

        public override void ResetEffects()
        {
            IsHoldingFifteen = false;
            IsHoldingAnyKatana = false;
        }

        public bool CanUseSlowMo() => Battery > 2f;
        public bool CanDash() => DragonDashCooldown <= 0;

        public void ConsumeDashBattery()
        {
            // Dash does not consume battery in Katana Zero
        }

        public void StartSlowMo()
        {
            if (!IsSlowingTime && CanUseSlowMo())
            {
                IsSlowingTime = true;
                if (Main.myPlayer == Player.whoAmI)
                {
                    SoundEngine.PlaySound(FifteensBlade.SlowEngage);
                }
            }
        }

        public void EndSlowMo()
        {
            if (IsSlowingTime)
            {
                IsSlowingTime = false;
                RevertNormalSpeed();
                if (Main.myPlayer == Player.whoAmI)
                {
                    SoundEngine.PlaySound(FifteensBlade.SlowDisengage);
                }
            }
        }

        public void CapturePlayerDrawData(List<DrawData> drawDataCache, Vector2 playerPos, Vector2 screenPos)
        {
            if (drawDataCache == null || drawDataCache.Count == 0)
                return;

            CapturedPlayerDrawData = new List<DrawData>(drawDataCache);
            CapturedPlayerPosition = playerPos;
            CapturedScreenPosition = screenPos;
            CapturedPlayerDirection = Player.direction;
        }

        public void SpawnGhostTrails(List<Vector2> positions, Vector2 dashDirection)
        {
            if (positions == null || positions.Count == 0)
                return;

            int dashDir = dashDirection.X != 0f ? (dashDirection.X > 0 ? 1 : -1) : Player.direction;
            if (CapturedPlayerDrawData == null || CapturedPlayerDrawData.Count == 0 || CapturedPlayerDirection != dashDir)
            {
                try
                {
                    Player.direction = dashDir;
                    PlayerDrawSet pds = default;
                    pds.BoringSetup(Player, new List<DrawData>(), new List<int>(), new List<int>(), Player.position, 0f, 0f, Vector2.Zero);
                    PlayerLoader.ModifyDrawInfo(ref pds);
                    PlayerDrawLayer[] drawLayers = PlayerDrawLayerLoader.GetDrawLayers(pds);
                    foreach (PlayerDrawLayer playerDrawLayer in drawLayers)
                    {
                        if (playerDrawLayer is not GhostTrailLayer && playerDrawLayer is not GhostCaptureLayer)
                        {
                            playerDrawLayer.DrawWithTransformationAndChildren(ref pds);
                        }
                    }
                    CapturePlayerDrawData(pds.DrawDataCache, Player.position, Main.screenPosition);
                }
                catch
                {
                }
            }

            if (CapturedPlayerDrawData == null || CapturedPlayerDrawData.Count == 0)
                return;

            int count = positions.Count;
            for (int i = 0; i < count; i++)
            {
                // Stagger duration so closer afterimages dissolve smoothly
                int life = 18 + (int)((float)i / count * 8);

                ActiveGhosts.Add(new GhostSilhouette
                {
                    WorldPosition = positions[i],
                    BasePlayerPosition = CapturedPlayerPosition,
                    BaseScreenPosition = CapturedScreenPosition,
                    DrawDataList = new List<DrawData>(CapturedPlayerDrawData),
                    TimeLeft = life,
                    MaxTimeLeft = life,
                    GlitchOffset = 3f,
                    DashDirection = dashDirection
                });
            }
        }

        public void SlowDownWorld()
        {
            const float slowdownFactor = 0.25f;
            const float maxDistance = 2000f;

            foreach (NPC mob in Main.npc)
            {
                if (mob.active && !mob.friendly && !mob.townNPC && !mob.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[mob.type] && (mob.damage > 0 || mob.lifeMax > 5) && mob.Distance(Player.Center) < maxDistance)
                {
                    if (!OriginalNPCSpeeds.ContainsKey(mob.whoAmI))
                    {
                        OriginalNPCSpeeds[mob.whoAmI] = Math.Max(0.5f, mob.velocity.Length());
                    }
                    float originalSpeed = OriginalNPCSpeeds[mob.whoAmI];
                    float desiredSpeed = originalSpeed * slowdownFactor;

                    if (mob.velocity.Length() > desiredSpeed)
                    {
                        Vector2 currentDirection = mob.velocity.SafeNormalize(Vector2.Zero);
                        Vector2 newVelocity = currentDirection * desiredSpeed;
                        mob.velocity = newVelocity;

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            SendNPCVelocityChange(mob.whoAmI, newVelocity);
                        }
                    }
                }
                else
                {
                    if (OriginalNPCSpeeds.TryGetValue(mob.whoAmI, out float origSpeed))
                    {
                        if (mob.active)
                        {
                            mob.velocity = mob.velocity.SafeNormalize(Vector2.Zero) * origSpeed;
                        }
                        OriginalNPCSpeeds.Remove(mob.whoAmI);
                    }
                }
            }

            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && (proj.hostile || proj.trap) && proj.Distance(Player.Center) < maxDistance)
                {
                    if (!OriginalProjectileSpeeds.ContainsKey(proj.whoAmI))
                    {
                        OriginalProjectileSpeeds[proj.whoAmI] = Math.Max(0.5f, proj.velocity.Length());
                    }
                    float originalSpeed = OriginalProjectileSpeeds[proj.whoAmI];
                    float desiredSpeed = originalSpeed * slowdownFactor;

                    if (proj.velocity.Length() > desiredSpeed)
                    {
                        Vector2 currentDirection = proj.velocity.SafeNormalize(Vector2.Zero);
                        Vector2 newVelocity = currentDirection * desiredSpeed;
                        proj.velocity = newVelocity;

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            SendProjectileVelocityChange(proj.whoAmI, newVelocity);
                        }
                    }
                }
                else
                {
                    if (OriginalProjectileSpeeds.TryGetValue(proj.whoAmI, out float origSpeed))
                    {
                        if (proj.active)
                        {
                            proj.velocity = proj.velocity.SafeNormalize(Vector2.Zero) * origSpeed;
                        }
                        OriginalProjectileSpeeds.Remove(proj.whoAmI);
                    }
                }
            }
        }

        public void RevertNormalSpeed()
        {
            foreach (var kvp in OriginalNPCSpeeds)
            {
                int id = kvp.Key;
                if (id >= 0 && id < Main.maxNPCs)
                {
                    NPC mob = Main.npc[id];
                    if (mob.active)
                    {
                        float originalSpeed = kvp.Value;
                        Vector2 currentDirection = mob.velocity.SafeNormalize(Vector2.Zero);
                        Vector2 newVelocity = currentDirection * originalSpeed;
                        mob.velocity = newVelocity;

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            SendNPCVelocityChange(mob.whoAmI, newVelocity);
                        }
                    }
                }
            }
            OriginalNPCSpeeds.Clear();

            foreach (var kvp in OriginalProjectileSpeeds)
            {
                int id = kvp.Key;
                if (id >= 0 && id < Main.maxProjectiles)
                {
                    Projectile proj = Main.projectile[id];
                    if (proj.active)
                    {
                        float originalSpeed = kvp.Value;
                        Vector2 currentDirection = proj.velocity.SafeNormalize(Vector2.Zero);
                        Vector2 newVelocity = currentDirection * originalSpeed;
                        proj.velocity = newVelocity;

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            SendProjectileVelocityChange(proj.whoAmI, newVelocity);
                        }
                    }
                }
            }
            OriginalProjectileSpeeds.Clear();
        }

        private void SendNPCVelocityChange(int npcID, Vector2 newVelocity)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)1);
            packet.Write(npcID);
            packet.Write(newVelocity.X);
            packet.Write(newVelocity.Y);
            packet.Send();
        }

        private void SendProjectileVelocityChange(int projID, Vector2 newVelocity)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)2);
            packet.Write(projID);
            packet.Write(newVelocity.X);
            packet.Write(newVelocity.Y);
            packet.Send();
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            EndSlowMo();
        }

        public override void PreUpdateMovement()
        {
            // Low-gravity sluggish inertial drift during Chronos time-slow
            if (IsSlowingTime)
            {
                // Slow down ground movement to ~50%
                Player.maxRunSpeed *= 0.5f;
                Player.accRunSpeed *= 0.5f;

                // Low-gravity floatiness in air (~50% speed with momentum, noticeably faster than 75%-slowed enemies)
                Player.gravity *= 0.15f;
                Player.maxFallSpeed *= 0.35f;
                Player.fallStart = (int)(Player.position.Y / 16f); // Prevent fall damage accumulation

                // Smooth inertial drift while floating in air
                if (Player.velocity.Y > 1.5f)
                {
                    Player.velocity.Y = MathHelper.Lerp(Player.velocity.Y, 1.2f, 0.08f);
                }
                else if (Player.velocity.Y < -3f)
                {
                    Player.velocity.Y = MathHelper.Lerp(Player.velocity.Y, -2.5f, 0.10f);
                }
                if (Math.Abs(Player.velocity.X) > 0.05f)
                {
                    Player.velocity.X = MathHelper.Lerp(Player.velocity.X, 0f, 0.02f);
                }
            }
        }

        public override void PostUpdate()
        {
            // If player switched away from weapon, or died, or battery is 0, exit slowmo safely
            if (IsSlowingTime && (!IsHoldingFifteen || Player.dead || Battery <= 0f))
            {
                EndSlowMo();
            }

            // Smooth visual intensity transition (0.0 <-> 1.0)
            float targetIntensity = IsSlowingTime ? 1f : 0f;
            SlowMoVisualIntensity = MathHelper.Lerp(SlowMoVisualIntensity, targetIntensity, 0.20f);
            if (Math.Abs(SlowMoVisualIntensity - targetIntensity) < 0.01f)
            {
                SlowMoVisualIntensity = targetIntensity;
            }

            // Battery drain and auto-recharge logic
            if (IsSlowingTime)
            {
                Battery = Math.Max(0f, Battery - SlowMoDrainPerTick);
                RechargeCooldown = 25;

                if (Battery <= 0f)
                {
                    EndSlowMo();
                }
            }
            else
            {
                if (RechargeCooldown > 0)
                {
                    RechargeCooldown--;
                }
                else if (Battery < MaxBattery)
                {
                    Battery = Math.Min(MaxBattery, Battery + BatteryRechargePerTick);
                }
            }

            if (DragonDashCooldown > 0)
            {
                DragonDashCooldown--;
            }

            // Update ghost silhouettes along dash path
            for (int i = ActiveGhosts.Count - 1; i >= 0; i--)
            {
                ActiveGhosts[i].TimeLeft--;
                if (ActiveGhosts[i].TimeLeft <= 0)
                {
                    ActiveGhosts.RemoveAt(i);
                }
            }
        }
    }
}
