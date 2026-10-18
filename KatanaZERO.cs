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
                // R2. Stylized deep navy/cyan body colors during Chronos time-slow
                Color navyTint = new Color(20, 65, 120, drawColor.A);
                drawColor = Color.Lerp(drawColor, navyTint, intensity * 0.85f);
            }
        }

        public static (Vector2 drawPos, Vector2 origin, Rectangle sourceRect, SpriteEffects effects) GetNPCDrawInfo(NPC npc, Texture2D texture, Vector2 screenPos)
        {
            if (npc.type == NPCID.Deerclops)
            {
                int frameX = npc.frame.Y / 5;
                int frameY = npc.frame.Y % 5;
                Rectangle sourceRect = Terraria.Utils.Frame(texture, 5, 5, frameX, frameY, 2, 2);
                Vector2 origin = sourceRect.Size() * new Vector2(0.5f, 1f) - new Vector2(0f, 4f);
                Vector2 drawPos = npc.Bottom - screenPos;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                return (drawPos, origin, sourceRect, effects);
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
                int frameCount = Math.Max(1, Main.npcFrameCount[npc.type]);
                Vector2 frameDimensions = new Vector2(texture.Width, (float)texture.Height / frameCount);

                Vector2 drawPos = new Vector2(
                    npc.position.X - screenPos.X + (npc.width / 2f) - (frameDimensions.X * npc.scale / 2f) + (halfSize.X * npc.scale),
                    npc.position.Y - screenPos.Y + npc.height - (frameDimensions.Y * npc.scale) + (halfSize.Y * npc.scale) + npc.gfxOffY
                );

                Vector2 origin = halfSize;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                return (drawPos, origin, sourceRect, effects);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity > 0.01f && npc.active && !npc.friendly && !npc.townNPC && !npc.isLikeATownNPC && !NPCID.Sets.CountsAsCritter[npc.type] && (npc.damage > 0 || npc.lifeMax > 5))
            {
                Main.instance.LoadNPC(npc.type);
                if (!Terraria.GameContent.TextureAssets.Npc[npc.type].IsLoaded)
                    return true;

                Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
                if (texture == null)
                    return true;

                float alphaFactor = Math.Max(0f, (255f - npc.alpha) / 255f);
                float strokeSize = Math.Clamp(Math.Max(npc.width, npc.height) / 32f, 2.5f, 6.5f);
                Color outlineColor = new Color(60, 40, 245, 250) * (intensity * alphaFactor);

                var (drawPos, origin, sourceRect, effects) = GetNPCDrawInfo(npc, texture, screenPos);

                int sampleCount = 12;
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
            return true; // Allow Terraria to draw the muted body on top of the outline
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