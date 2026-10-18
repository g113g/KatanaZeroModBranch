using KatanaZERO.Items.FifteensBlade;
using KatanaZERO.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using static Terraria.ModLoader.ModLoader;

namespace KatanaZERO
{
    public class KatanaZERO : Mod
    {

        // Global variable
        public static bool enableLunge = true; // Default to true
        public static bool enableTimeShift = true;
        public static bool enableVectorKnockback = false;
        public static bool toggleDrop = true;
        public static bool progressionDamage = false;
        public static bool enableKatanaZeroFont = true;

        public static string zeroKatanaSound;
        public static string prismSwordSound;
        public static string masterSwordSound;
        public static string savantKnifeSound;
        public static string claymoreSound;
        public static string phoenixEdgeSound;
        public static string fifteensKatanaSound;
        public static string dashTrailStyle;

        public override void Load()
        {
            SyncConfig(GetInstance<Settings>());
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte packetType = reader.ReadByte();
            switch (packetType)
            {
                case 1: // NPC velocity change
                    int npcID = reader.ReadInt32();
                    Vector2 npcVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    if (npcID >= 0 && npcID < Main.maxNPCs && Main.npc[npcID].active)
                    {
                        Main.npc[npcID].velocity = npcVelocity;
                    }
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)1);
                        packet.Write(npcID);
                        packet.Write(npcVelocity.X);
                        packet.Write(npcVelocity.Y);
                        packet.Send(-1, whoAmI);
                    }
                    break;
                case 2: // Projectile velocity change
                    int projID = reader.ReadInt32();
                    Vector2 projVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    if (projID >= 0 && projID < Main.maxProjectiles && Main.projectile[projID].active)
                    {
                        Main.projectile[projID].velocity = projVelocity;
                    }
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)2);
                        packet.Write(projID);
                        packet.Write(projVelocity.X);
                        packet.Write(projVelocity.Y);
                        packet.Send(-1, whoAmI);
                    }
                    break;
            }
        }

        public static void SyncConfig(Settings config)
        {
            enableLunge = config.Lunge;
            enableTimeShift = config.TimeShift;
            enableVectorKnockback = config.VectorKnockback;
            toggleDrop = config.ToggleDrop;
            progressionDamage = config.ProgressionDamage;

            zeroKatanaSound = config.ZeroKatanaSound;
            prismSwordSound = config.PrismSwordSound;
            masterSwordSound = config.MasterSwordSound;
            savantKnifeSound = config.SavantKnifeSound;
            claymoreSound = config.ClaymoreSound;
            phoenixEdgeSound = config.PhoenixEdgeSound;
            fifteensKatanaSound = config.FifteensBladeSound;
            dashTrailStyle = config.DashTrailStyle;
            enableKatanaZeroFont = config.CustomFont;
        }

        public static int GetRootEntityId(NPC npc)
        {
            if (npc == null || !npc.active)
                return -1;

            // 1. Explicit vanilla/modded realLife link (The Destroyer, Wyverns, Calamity worms, etc.)
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].active)
            {
                return npc.realLife;
            }

            // 2. Worm AI (AI Style 6) or Eater of Worlds segments: follow ai[0] link to find head
            if (npc.aiStyle == NPCAIStyleID.Worm || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
            {
                int current = npc.whoAmI;
                int safety = 0;
                while (safety++ < 100)
                {
                    NPC currNpc = Main.npc[current];
                    int parent = (int)currNpc.ai[0];
                    if (parent >= 0 && parent < Main.maxNPCs && parent != current && Main.npc[parent].active)
                    {
                        if (Main.npc[parent].aiStyle == NPCAIStyleID.Worm 
                            || Main.npc[parent].type == NPCID.EaterofWorldsHead 
                            || Main.npc[parent].type == NPCID.EaterofWorldsBody 
                            || Main.npc[parent].type == npc.type)
                        {
                            current = parent;
                            continue;
                        }
                    }
                    break;
                }
                return current;
            }

            // 3. Known multipart vanilla bosses:
            // Golem: Head (246), Fists (247, 248) -> Body (245)
            if (npc.type == NPCID.GolemHead || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.Golem)
                        return i;
                }
            }

            // Moon Lord: Hands (397), Head (396), Free Eyes (400) -> Core (398)
            if (npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordFreeEye)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.MoonLordCore)
                        return i;
                }
            }

            // Skeletron & Skeletron Prime: hands/arms link to head via ai[1]
            if (npc.type == NPCID.SkeletronHand || npc.type == NPCID.PrimeCannon || npc.type == NPCID.PrimeSaw || npc.type == NPCID.PrimeVice || npc.type == NPCID.PrimeLaser)
            {
                int head = (int)npc.ai[1];
                if (head >= 0 && head < Main.maxNPCs && Main.npc[head].active)
                    return head;
            }

            // Brain of Cthulhu Creepers link via ai[0]
            if (npc.type == NPCID.Creeper)
            {
                int brain = (int)npc.ai[0];
                if (brain >= 0 && brain < Main.maxNPCs && Main.npc[brain].active)
                    return brain;
            }

            // Wall of Flesh Eyes & Hungries
            if (npc.type == NPCID.WallofFleshEye || npc.type == NPCID.TheHungry || npc.type == NPCID.TheHungryII)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.WallofFlesh)
                        return i;
                }
            }

            return npc.whoAmI;
        }
    }

    public class MyGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {

            if (!npc.friendly && KatanaZERO.toggleDrop)
            {
                npcLoot.Add(ItemDropRule.Common(ItemType<FifteensBlade>(), 40000, 1, 1));
            }
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.Common(ItemType<FifteensBlade>(), 9, 1, 1));
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (KatanaZERO.enableTimeShift && KatanaPlayer.LocalIsSlowingTime)
            {
                if (npc.active && !npc.friendly && !npc.townNPC && !npc.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[npc.type] && (npc.damage > 0 || npc.lifeMax > 5))
                {
                    // Throttle hostile enemy updates to 1 out of every 4 ticks (75% time dilation).
                    // On the 3 skipped ticks, suppress AI so attack timers, Retinazer lasers, and charge accelerations are slowed down!
                    if (Main.GameUpdateCount % 4 != 0)
                    {
                        npc.velocity *= 0.25f;
                        return false;
                    }
                }
            }
            return base.PreAI(npc);
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity > 0.01f && npc.active && !npc.friendly && !npc.townNPC && !npc.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[npc.type] && (npc.damage > 0 || npc.lifeMax > 5))
            {
                // Dynamic Katana Zero Chronos enemy palette
                // Hostile enemies are perceived with fullbright illumination, completely immune to pitch-black nighttime/cave darkness.
                Player player = Main.LocalPlayer;
                float dist = Vector2.Distance(npc.Center, player.Center);
                bool inDashRange = dist <= 464f;

                // Calculate aim proximity if player is aiming
                float aimProximity = 0f;
                Vector2 toMouse = Main.MouseWorld - player.Center;
                float mouseDist = toMouse.Length();
                if (mouseDist > 1f)
                {
                    Vector2 aimDir = toMouse / mouseDist;
                    Vector2 segStart = player.Center;
                    Vector2 segEnd = player.Center + aimDir * Math.Min(464f, Math.Max(50f, mouseDist));
                    Vector2 seg = segEnd - segStart;
                    float segLenSq = seg.LengthSquared();
                    float t = segLenSq > 0f ? Math.Clamp(Vector2.Dot(npc.Center - segStart, seg) / segLenSq, 0f, 1f) : 0f;
                    Vector2 closestPoint = segStart + seg * t;
                    float distToAim = Vector2.Distance(npc.Center, closestPoint);
                    aimProximity = Math.Clamp(1f - distToAim / 100f, 0f, 1f);
                }

                Color targetBody;
                if (inDashRange)
                {
                    // Alerted / targeted state (matching Katana Zero combat state):
                    // Bright vibrant electric cyan/teal with luminous cyan-white highlights when aimed at!
                    Color baseCyan = new Color(175, 245, 255);
                    Color highlightCyan = new Color(255, 255, 255);
                    targetBody = Color.Lerp(baseCyan, highlightCyan, aimProximity * 0.75f);
                }
                else
                {
                    // Distant / unalerted state (matching Katana Zero patrol state):
                    // Stylized vibrant Katana Zero violet/indigo
                    targetBody = new Color(215, 185, 255);
                }

                byte alpha = (byte)Math.Clamp(255 - npc.alpha, 0, 255);
                targetBody.A = alpha;

                // Full replacement at peak slowmo intensity so enemies are vividly lit at night and day alike
                drawColor = Color.Lerp(drawColor, targetBody, intensity * 1.0f);
            }
        }

        public static (Texture2D texture, Vector2 drawPos, Vector2 origin, Rectangle sourceRect, SpriteEffects effects)? GetNPCDrawInfo(NPC npc, Vector2 screenPos)
        {
            // Moon Lord's background composite parts (Head, Hand, Core) are positioned and drawn by vanilla's screen-space system.
            // Skipping manual outline pass on Moon Lord prevents duplicate floating parts while preserving the full Katana Zero stylized indigo body via DrawEffects.
            if (npc.type == NPCID.MoonLordCore || npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand)
            {
                return null;
            }

            Texture2D texture;
            if (npc.type == NPCID.Golem)
            {
                texture = Terraria.GameContent.TextureAssets.Golem[3].Value;
            }
            else if (npc.type == NPCID.GolemHead)
            {
                texture = Terraria.GameContent.TextureAssets.Golem[0].Value;
            }
            else if (npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight)
            {
                texture = Terraria.GameContent.TextureAssets.Golem[1].Value;
            }
            else
            {
                Main.instance.LoadNPC(npc.type);
                if (!Terraria.GameContent.TextureAssets.Npc[npc.type].IsLoaded)
                    return null;
                texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
            }

            if (texture == null)
                return null;

            if (npc.type == NPCID.Deerclops)
            {
                int frameX = npc.frame.Y / 5;
                int frameY = npc.frame.Y % 5;
                Rectangle sourceRect = Terraria.Utils.Frame(texture, 5, 5, frameX, frameY, 2, 2);
                Vector2 origin = sourceRect.Size() * new Vector2(0.5f, 1f) - new Vector2(0f, 4f);
                Vector2 drawPos = npc.Bottom - screenPos;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                return (texture, drawPos, origin, sourceRect, effects);
            }
            else if (npc.type == NPCID.QueenSlimeBoss)
            {
                int frameCount = Math.Max(1, Main.npcFrameCount[npc.type]);
                int frameHeight = Math.Max(1, npc.frame.Height);
                int frameIndex = npc.frame.Y / frameHeight;
                int frameX = frameIndex / frameCount;
                int frameY = frameIndex % frameCount;
                if (frameX < 0 || frameX > 1) frameX = 0;
                Rectangle sourceRect = Terraria.Utils.Frame(texture, 2, frameCount, frameX, frameY, 0, 0);
                sourceRect.Inflate(0, -2);
                Vector2 origin = sourceRect.Size() * new Vector2(0.5f, 1f);
                Vector2 drawPos = npc.Bottom - screenPos + new Vector2(0f, 2f);
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                return (texture, drawPos, origin, sourceRect, effects);
            }
            else
            {
                Rectangle sourceRect = npc.frame;
                if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
                {
                    int fc = Math.Max(1, Main.npcFrameCount[npc.type]);
                    sourceRect = new Rectangle(0, 0, texture.Width, texture.Height / fc);
                }

                Vector2 halfSize = sourceRect.Size() / 2f;
                Vector2 drawPos = new Vector2(
                    npc.Center.X - screenPos.X,
                    npc.Bottom.Y - screenPos.Y - (sourceRect.Height * npc.scale / 2f) + npc.gfxOffY
                );

                Vector2 origin = halfSize;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                return (texture, drawPos, origin, sourceRect, effects);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity > 0.01f && npc.active && !npc.friendly && !npc.townNPC && !npc.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[npc.type] && (npc.damage > 0 || npc.lifeMax > 5))
            {
                var drawInfo = GetNPCDrawInfo(npc, screenPos);
                if (drawInfo.HasValue)
                {
                    var (texture, drawPos, origin, sourceRect, effects) = drawInfo.Value;

                    float alphaFactor = Math.Max(0f, (255f - npc.alpha) / 255f);
                    float strokeSize = Math.Clamp(Math.Max(npc.width, npc.height) / 20f, 3.5f, 9.5f);

                    Player player = Main.LocalPlayer;
                    float dist = Vector2.Distance(npc.Center, player.Center);
                    bool inDashRange = dist <= 464f;

                    // Dynamic Katana Zero outline: Neon Electric Cyan when alerted/in-range, Neon Violet when distant
                    Color outlineColor = inDashRange
                        ? new Color(0, 245, 255, 245) * (intensity * alphaFactor)
                        : new Color(140, 60, 255, 235) * (intensity * alphaFactor);

                    int sampleCount = 16;
                    for (int s = 0; s < sampleCount; s++)
                    {
                        float angle = MathHelper.TwoPi / sampleCount * s;
                        Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * strokeSize;
                        spriteBatch.Draw(
                            texture,
                            drawPos + offset,
                            sourceRect,
                            outlineColor,
                            npc.rotation,
                            origin,
                            npc.scale,
                            effects,
                            0f
                        );
                    }
                }
            }
            return true; // Allow Terraria to draw the stylized fullbright body on top of the outline
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity > 0.01f && npc.active && !npc.friendly && !npc.townNPC && !npc.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[npc.type] && (npc.damage > 0 || npc.lifeMax > 5))
            {
                var drawInfo = GetNPCDrawInfo(npc, screenPos);
                if (drawInfo.HasValue)
                {
                    var (texture, drawPos, origin, sourceRect, effects) = drawInfo.Value;
                    float alphaFactor = Math.Max(0f, (255f - npc.alpha) / 255f);

                    Player player = Main.LocalPlayer;
                    float dist = Vector2.Distance(npc.Center, player.Center);
                    bool inDashRange = dist <= 464f;

                    // 1. High-intensity additive neon wash
                    Color bodyGlow = inDashRange
                        ? new Color(0, 230, 255, 0) * (intensity * alphaFactor * 0.95f)
                        : new Color(180, 100, 255, 0) * (intensity * alphaFactor * 0.90f);

                    spriteBatch.Draw(
                        texture,
                        drawPos,
                        sourceRect,
                        bodyGlow,
                        npc.rotation,
                        origin,
                        npc.scale,
                        effects,
                        0f
                    );

                    // 2. Pure additive white sheen (lifts all pitch-black pixels into luminous brightness)
                    Color whiteSheen = new Color(255, 255, 255, 0) * (intensity * alphaFactor * 0.40f);
                    spriteBatch.Draw(
                        texture,
                        drawPos,
                        sourceRect,
                        whiteSheen,
                        npc.rotation,
                        origin,
                        npc.scale,
                        effects,
                        0f
                    );
                }
            }
        }

        public static Dictionary<int, float> LastKatanaHitAngle = new Dictionary<int, float>();

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            var kp = player.GetModPlayer<KatanaPlayer>();
            if (kp.IsHoldingAnyKatana || item.ModItem is FifteensBlade)
            {
                LastKatanaHitAngle[npc.whoAmI] = (npc.Center - player.Center).ToRotation();
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.friendly && projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player.active)
                {
                    var kp = player.GetModPlayer<KatanaPlayer>();
                    if (kp.IsHoldingAnyKatana || kp.IsHoldingFifteen || kp.IsSlowingTime || projectile.DamageType == DamageClass.Melee)
                    {
                        LastKatanaHitAngle[npc.whoAmI] = projectile.velocity != Vector2.Zero
                            ? projectile.velocity.ToRotation()
                            : (npc.Center - player.Center).ToRotation();
                    }
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.townNPC || npc.isLikeATownNPC || NPCID.Sets.CountsAsCritter[npc.type])
                return;

            if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers)
            {
                Player player = Main.player[npc.lastInteraction];
                if (player.active)
                {
                    var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
                    // If player is currently executing Dragon Dash, DragonDash handles the single dash kill line
                    if (katanaPlayer.IsDragonDashing)
                    {
                        LastKatanaHitAngle.Remove(npc.whoAmI);
                        return;
                    }

                    if (katanaPlayer.IsSlowingTime || katanaPlayer.IsHoldingAnyKatana || katanaPlayer.IsHoldingFifteen)
                    {
                        float angle = 0f;
                        if (LastKatanaHitAngle.TryGetValue(npc.whoAmI, out float recordedAngle))
                        {
                            angle = recordedAngle;
                        }
                        else
                        {
                            angle = (npc.Center - player.Center).ToRotation();
                        }

                        katanaPlayer.SpawnScreenCut(npc.Center, angle);
                    }
                }
            }
            LastKatanaHitAngle.Remove(npc.whoAmI);
        }
    }

    public static class ColorHelper
    {
        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");

            byte r = (byte)(Convert.ToInt32(hex.Substring(0, 2), 16)); // Red
            byte g = (byte)(Convert.ToInt32(hex.Substring(2, 2), 16)); // Green
            byte b = (byte)(Convert.ToInt32(hex.Substring(4, 2), 16)); // Blue

            return new Color(r, g, b);
        }
    }

    public class PowerLevelManager
    {
        private static readonly Dictionary<int, (int damage, int crit)> PowerLevels = new()
        {
            { 1, (10, 1) }, { 2, (20, 2) }, { 3, (30, 4) }, { 5, (42, 7) },
            { 6, (43, 9) }, { 7, (45, 11) }, { 8, (48, 13) }, { 9, (51, 15) },
            { 10, (52, 17) }, { 11, (53, 19) }, { 12, (54, 21) }, { 13, (55, 23) },
            { 14, (60, 25) }, { 15, (64, 27) }, { 16, (69, 29) }, { 17, (71, 31) },
            { 18, (73, 33) }, { 19, (75, 35) }, { 20, (78, 36) }, { 21, (80, 38) },
            { 22, (85, 40) }, { 23, (90, 42) }, { 24, (100, 44) }, { 25, (110, 45) },
            { 26, (125, 47) }, { 27, (140, 49) }, { 28, (170, 51) }, { 29, (220, 53) },
            { 30, (245, 55) }, { 31, (260, 56) }, { 32, (270, 58) }, { 33, (285, 60) },
            { 34, (300, 62) }, { 35, (335, 64) }, { 36, (370, 65) }, { 38, (440, 67) },
            { 39, (495, 69) }, { 40, (560, 71) }, { 41, (630, 20) }, { 42, (650, 25) },
            { 43, (750, 37) }, { 44, (770, 37) }, { 45, (820, 43) }, { 46, (1050, 45) },
            { 47, (1230, 50) }, { 48, (1280, 55) }, { 49, (1315, 60) }, { 50, (1345, 63) },
            { 51, (1515, 68) }, { 52, (2200, 75) }, { 53, (3550, 90) }, { 54, (4015, 100) },
            { 55, (5115, 150) }
        };

        public void Apply(Item item)
        {
            int level = GetPowerLevel();
            if (PowerLevels.TryGetValue(level, out var stats))
            {
                item.damage = stats.damage;
                item.crit = stats.crit;
            }
        }

        private int GetPowerLevel()
        {
            TryGetMod("CalamityMod", out Mod Calamity);
            TryGetMod("ThoriumMod", out Mod thoriumMod);
            bool hasCal = Calamity != null;
            bool hasThor = thoriumMod != null;

            return hasCal && hasThor
                ? GetBoth(Calamity, thoriumMod)
                : hasCal
                    ? GetCalamity(Calamity)
                    : hasThor
                        ? GetThorium(thoriumMod)
                        : GetVanilla();
        }

        private int GetCalamity(Mod cal)
        {
            if ((bool)cal.Call("Downed", "bossrush")) return 55;
            if ((bool)cal.Call("Downed", "calamitas")) return 54;
            if ((bool)cal.Call("Downed", "exomechs")) return 53;
            if ((bool)cal.Call("Downed", "yharon")) return 52;
            if ((bool)cal.Call("Downed", "devourerofgods")) return 51;
            if ((bool)cal.Call("Downed", "oldduke")) return 50;
            if ((bool)cal.Call("Downed", "polterghast")) return 49;
            if ((bool)cal.Call("Downed", "signus")) return 48;
            if ((bool)cal.Call("Downed", "stormweaver")) return 47;
            if ((bool)cal.Call("Downed", "ceaselessvoid")) return 46;
            if ((bool)cal.Call("Downed", "providence")) return 44;
            if ((bool)cal.Call("Downed", "dragonfolly")) return 43;
            if ((bool)cal.Call("Downed", "guardians")) return 42;
            if (NPC.downedMoonlord) return 41;
            if ((bool)cal.Call("Downed", "astrumdeus")) return 40;
            if (NPC.downedAncientCultist) return 39;
            if ((bool)cal.Call("Downed", "ravager")) return 38;
            if (NPC.downedEmpressOfLight) return 36;
            if ((bool)cal.Call("Downed", "plaguebringergoliath")) return 35;
            if (NPC.downedFishron) return 34;
            if (NPC.downedGolemBoss) return 32;
            if ((bool)cal.Call("Downed", "astrumaureus")) return 31;
            if ((bool)cal.Call("Downed", "anahitaleviathan")) return 30;
            if (NPC.downedPlantBoss) return 29;
            if ((bool)cal.Call("Downed", "calamitasclone")) return 28;
            if (NPC.downedMechBoss3) return 26;
            if ((bool)cal.Call("Downed", "brimstoneelemental")) return 25;
            if (NPC.downedMechBoss2) return 24;
            if ((bool)cal.Call("Downed", "aquaticscourge")) return 23;
            if (NPC.downedMechBoss1) return 22;
            if ((bool)cal.Call("Downed", "cryogen")) return 21;
            if (NPC.downedQueenSlime) return 20;
            if (Main.hardMode) return 17;
            if ((bool)cal.Call("Downed", "slimegod")) return 15;
            if (NPC.downedDeerclops) return 12;
            if (NPC.downedBoss3) return 11;
            if (NPC.downedQueenBee) return 10;
            if ((bool)cal.Call("Downed", "hivemind") || (bool)cal.Call("Downed", "perforator")) return 9;
            if (NPC.downedBoss2) return 6;
            if ((bool)cal.Call("Downed", "crabulon")) return 5;
            if (NPC.downedBoss1) return 4;
            if ((bool)cal.Call("Downed", "desertscourge")) return 3;
            if (NPC.downedSlimeKing) return 2;
            return 1;
        }

        private int GetThorium(Mod thor)
        {
            if ((bool)thor.Call("GetDownedBoss", "ThePrimordials")) return 45;
            if (NPC.downedMoonlord) return 41;
            if (NPC.downedAncientCultist) return 39;
            if (NPC.downedEmpressOfLight) return 36;
            if (NPC.downedFishron) return 34;
            if ((bool)thor.Call("GetDownedBoss", "ForgottenOne")) return 33;
            if (NPC.downedGolemBoss) return 32;
            if (NPC.downedPlantBoss) return 29;
            if ((bool)thor.Call("GetDownedBoss", "Lich")) return 27;
            if (NPC.downedMechBoss3) return 26;
            if (NPC.downedMechBoss2) return 24;
            if (NPC.downedMechBoss1) return 22;
            if (NPC.downedQueenSlime) return 20;
            if ((bool)thor.Call("GetDownedBoss", "FallenBeholder")) return 19;
            if ((bool)thor.Call("GetDownedBoss", "BoreanStrider")) return 18;
            if (Main.hardMode) return 17;
            if ((bool)thor.Call("GetDownedBoss", "StarScouter")) return 16;
            if ((bool)thor.Call("GetDownedBoss", "BuriedChampion")) return 14;
            if ((bool)thor.Call("GetDownedBoss", "GraniteEnergyStorm")) return 13;
            if (NPC.downedDeerclops) return 12;
            if (NPC.downedBoss3) return 11;
            if (NPC.downedQueenBee) return 10;
            if ((bool)thor.Call("GetDownedBoss", "Viscount")) return 8;
            if ((bool)thor.Call("GetDownedBoss", "QueenJellyfish")) return 7;
            if (NPC.downedBoss2) return 6;
            if (NPC.downedBoss1) return 4;
            if (NPC.downedSlimeKing) return 2;
            if ((bool)thor.Call("GetDownedBoss", "TheGrandThunderBird")) return 1;
            return 1;
        }

        private int GetBoth(Mod cal, Mod thor)
        {
            int p = GetCalamity(cal);
            int t = GetThorium(thor);
            return Math.Max(p, t);
        }

        private int GetVanilla()
        {
            if (NPC.downedMoonlord) return 41;
            if (NPC.downedAncientCultist) return 39;
            if (NPC.downedEmpressOfLight) return 36;
            if (NPC.downedFishron) return 34;
            if (NPC.downedGolemBoss) return 32;
            if (NPC.downedPlantBoss) return 29;
            if (NPC.downedMechBoss3) return 26;
            if (NPC.downedMechBoss2) return 24;
            if (NPC.downedMechBoss1) return 22;
            if (NPC.downedQueenSlime) return 20;
            if (Main.hardMode) return 17;
            if (NPC.downedDeerclops) return 12;
            if (NPC.downedBoss3) return 11;
            if (NPC.downedQueenBee) return 10;
            if (NPC.downedBoss2) return 6;
            if (NPC.downedBoss1) return 4;
            if (NPC.downedSlimeKing) return 2;
            return 1;
        }
    }
}