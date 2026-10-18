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
    public class DashStreak
    {
        public Vector2 StartCenter;
        public Vector2 EndCenter;
        public Vector2 Direction;
        public float Distance;
        public float Rotation;
        public int TimeLeft;
        public int MaxTimeLeft;
    }

    public class ScreenCutLine
    {
        public Vector2 WorldOrigin;
        public float Angle;
        public int TimeLeft;
        public int MaxTimeLeft;
    }

    public class SlowMoGhost
    {
        public Vector2 WorldPosition;
        public Vector2 BasePlayerPosition;
        public Vector2 BaseScreenPosition;
        public List<DrawData> DrawDataList;
        public int TimeLeft;
        public int MaxTimeLeft;
        public int Direction;
    }

    public class DyingEnemyGlitch
    {
        public Microsoft.Xna.Framework.Graphics.Texture2D Texture;
        public Rectangle SourceRect;
        public Vector2 Origin;
        public Vector2 WorldPosition;
        public float Rotation;
        public float Scale;
        public Microsoft.Xna.Framework.Graphics.SpriteEffects Effects;
        public int TimeLeft;
        public int MaxTimeLeft;
    }

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
        public bool IsDragonDashing = false;
        public float SlowMoVisualIntensity = 0f;

        // Dictionaries for tracking slowed mob and projectile speeds
        public Dictionary<int, float> OriginalNPCSpeeds = new Dictionary<int, float>();
        public Dictionary<int, float> OriginalProjectileSpeeds = new Dictionary<int, float>();

        // Active ghost silhouettes along dash trajectory
        public List<GhostSilhouette> ActiveGhosts = new List<GhostSilhouette>();

        // Active continuous chromatic motion blur dash streaks
        public List<DashStreak> ActiveStreaks = new List<DashStreak>();

        // Active full-screen kill slash cut lines
        public List<ScreenCutLine> ActiveScreenCuts = new List<ScreenCutLine>();

        // Active slow-mo movement ghosts (5-10 short trailing ghosts when moving in slowmo)
        public List<SlowMoGhost> ActiveSlowMoGhosts = new List<SlowMoGhost>();
        private int slowMoGhostSpawnTimer = 0;

        // Active dying enemy glitch ghosts left at standing position on dash kill
        public List<DyingEnemyGlitch> ActiveDyingEnemyGlitches = new List<DyingEnemyGlitch>();

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
                // Exactly 5 frames lifetime (fades smoothly in frames 4, 3, 2, 1)
                int life = 5;

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

        public void SpawnDashStreak(Vector2 startCenter, Vector2 endCenter, Vector2 direction)
        {
            float dist = Vector2.Distance(startCenter, endCenter);
            float rot = direction.ToRotation();
            int life = 6; // Exactly 5-6 frames lifetime matching Katana Zero
            ActiveStreaks.Add(new DashStreak
            {
                StartCenter = startCenter,
                EndCenter = endCenter,
                Direction = direction,
                Distance = dist,
                Rotation = rot,
                TimeLeft = life,
                MaxTimeLeft = life
            });
        }

        public void SpawnScreenCut(Vector2 worldPos, float angle)
        {
            foreach (var existing in ActiveScreenCuts)
            {
                if (Vector2.DistanceSquared(existing.WorldOrigin, worldPos) < 400f && existing.TimeLeft > existing.MaxTimeLeft - 3)
                    return;
            }

            int life = 12;
            ActiveScreenCuts.Add(new ScreenCutLine
            {
                WorldOrigin = worldPos,
                Angle = angle,
                TimeLeft = life,
                MaxTimeLeft = life
            });
        }

        public void SpawnDyingEnemyGlitch(NPC npc)
        {
            var drawInfo = MyGlobalNPC.GetNPCDrawInfo(npc, Vector2.Zero);
            if (!drawInfo.HasValue)
                return;

            var (texture, _, origin, sourceRect, effects) = drawInfo.Value;
            int life = 26; // ~0.43s lingering white-purple glitch ghost while body falls
            ActiveDyingEnemyGlitches.Add(new DyingEnemyGlitch
            {
                Texture = texture,
                SourceRect = sourceRect,
                Origin = origin,
                WorldPosition = npc.Center,
                Rotation = npc.rotation,
                Scale = npc.scale,
                Effects = effects,
                TimeLeft = life,
                MaxTimeLeft = life
            });
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

            // 5-10 short ghosts trailing close behind Fifteen while moving in slow-mo
            if (IsSlowingTime && Player.velocity.LengthSquared() > 0.10f)
            {
                if (++slowMoGhostSpawnTimer >= 2)
                {
                    slowMoGhostSpawnTimer = 0;
                    if (CapturedPlayerDrawData != null && CapturedPlayerDrawData.Count > 0)
                    {
                        int ghostLife = 9; // Fades fast over 9 ticks (~0.15s)
                        ActiveSlowMoGhosts.Add(new SlowMoGhost
                        {
                            WorldPosition = Player.position,
                            BasePlayerPosition = CapturedPlayerPosition,
                            BaseScreenPosition = CapturedScreenPosition,
                            DrawDataList = new List<DrawData>(CapturedPlayerDrawData),
                            TimeLeft = ghostLife,
                            MaxTimeLeft = ghostLife,
                            Direction = Player.direction
                        });

                        while (ActiveSlowMoGhosts.Count > 9)
                        {
                            ActiveSlowMoGhosts.RemoveAt(0);
                        }
                    }
                }
            }
            else
            {
                slowMoGhostSpawnTimer = 0;
            }

            // Update active slow-mo trailing ghosts
            for (int i = ActiveSlowMoGhosts.Count - 1; i >= 0; i--)
            {
                ActiveSlowMoGhosts[i].TimeLeft--;
                if (ActiveSlowMoGhosts[i].TimeLeft <= 0)
                {
                    ActiveSlowMoGhosts.RemoveAt(i);
                }
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

            // Update active dash streaks
            for (int i = ActiveStreaks.Count - 1; i >= 0; i--)
            {
                ActiveStreaks[i].TimeLeft--;
                if (ActiveStreaks[i].TimeLeft <= 0)
                {
                    ActiveStreaks.RemoveAt(i);
                }
            }

            // Update active screen cut lines
            for (int i = ActiveScreenCuts.Count - 1; i >= 0; i--)
            {
                ActiveScreenCuts[i].TimeLeft--;
                if (ActiveScreenCuts[i].TimeLeft <= 0)
                {
                    ActiveScreenCuts.RemoveAt(i);
                }
            }

            // Update active dying enemy glitches
            for (int i = ActiveDyingEnemyGlitches.Count - 1; i >= 0; i--)
            {
                ActiveDyingEnemyGlitches[i].TimeLeft--;
                if (ActiveDyingEnemyGlitches[i].TimeLeft <= 0)
                {
                    ActiveDyingEnemyGlitches.RemoveAt(i);
                }
            }
        }

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            // Fifteen turns electric blue when in slow-mo
            if (SlowMoVisualIntensity > 0.01f)
            {
                float t = SlowMoVisualIntensity;
                r = MathHelper.Lerp(r, r * 0.20f, t);
                g = MathHelper.Lerp(g, g * 0.75f + 0.15f, t);
                b = MathHelper.Lerp(b, Math.Min(1f, b * 1.35f + 0.35f), t);
                fullBright = true;
            }
        }
    }
}
