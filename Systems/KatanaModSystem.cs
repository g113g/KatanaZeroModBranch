using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace KatanaZERO.Systems
{
    public class KatanaModSystem : ModSystem
    {
        public static Texture2D WhitePixel;

        public override void Load()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                Main.QueueMainThreadAction(() =>
                {
                    WhitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                    WhitePixel.SetData(new[] { Color.White });
                });
            }
        }

        public override void Unload()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                var pixel = WhitePixel;
                WhitePixel = null;
                if (pixel != null)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        pixel.Dispose();
                    });
                }
            }
            else
            {
                WhitePixel = null;
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server)
                return;

            if (WhitePixel == null || WhitePixel.IsDisposed)
            {
                WhitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                WhitePixel.SetData(new[] { Color.White });
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();

            if (katanaPlayer.DragonDashCooldown > 0)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );

                DrawDashCooldownBar(Main.spriteBatch, player, katanaPlayer.DragonDashCooldown);

                Main.spriteBatch.End();
            }

            if (katanaPlayer.ActiveStreaks.Count > 0)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Additive,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );

                DrawDashStreaks(Main.spriteBatch, katanaPlayer);

                Main.spriteBatch.End();
            }

            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity <= 0.001f)
                return;

            float darkenAlpha = intensity * 0.90f;
            int screenW = Math.Max(Main.screenWidth, Main.graphics.GraphicsDevice.Viewport.Width);
            int screenH = Math.Max(Main.screenHeight, Main.graphics.GraphicsDevice.Viewport.Height);

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Matrix.Identity
            );

            Main.spriteBatch.Draw(
                WhitePixel,
                new Rectangle(0, 0, screenW, screenH),
                Color.Black * darkenAlpha
            );

            Main.spriteBatch.End();

            if (katanaPlayer.IsSlowingTime)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );

                DrawAimingReticle(Main.spriteBatch, player, intensity);

                Main.spriteBatch.End();
            }
        }

        private void DrawAimingReticle(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Vector2 playerCenterScreen = player.Center - Main.screenPosition;
            float radius = 464f;
            Color cyanCircle = new Color(0, 240, 255) * (intensity * 0.95f);

            int segments = 180;
            Vector2 prevPoint = playerCenterScreen + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi / segments * i;
                Vector2 nextPoint = playerCenterScreen + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                DrawLine(spriteBatch, prevPoint, nextPoint, cyanCircle, 3.5f);
                prevPoint = nextPoint;
            }

            Vector2 cursorWorld = Main.MouseWorld;
            Vector2 toCursor = cursorWorld - player.Center;
            float dist = toCursor.Length();
            if (dist > 0.001f)
            {
                toCursor.Normalize();
            }
            else
            {
                toCursor = new Vector2(player.direction, 0f);
            }
            float targetDist = Math.Min(dist, radius);
            Vector2 targetWorld = player.Center + toCursor * targetDist;
            Vector2 targetScreen = targetWorld - Main.screenPosition;

            DrawLine(spriteBatch, playerCenterScreen, targetScreen, cyanCircle, 3.5f);

            DrawSolidCircle(spriteBatch, targetScreen, 10f, cyanCircle * 0.35f);
            DrawSolidCircle(spriteBatch, targetScreen, 7f, cyanCircle);
            DrawSolidCircle(spriteBatch, targetScreen, 3.5f, Color.White * intensity);
        }

        private static void DrawSolidCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            if (WhitePixel == null || WhitePixel.IsDisposed)
                return;

            int r = (int)Math.Ceiling(radius);
            for (int y = -r; y <= r; y++)
            {
                float dx = (float)Math.Sqrt(Math.Max(0f, radius * radius - y * y));
                int width = (int)Math.Round(dx * 2f);
                if (width > 0)
                {
                    spriteBatch.Draw(
                        WhitePixel,
                        new Rectangle((int)(center.X - dx), (int)(center.Y + y), width, 1),
                        color
                    );
                }
            }
        }

        private static void DrawDashCooldownBar(SpriteBatch spriteBatch, Player player, int cooldown)
        {
            if (WhitePixel == null || WhitePixel.IsDisposed)
                return;

            Vector2 headPos = player.Center - Main.screenPosition;
            Vector2 barPos = new Vector2(headPos.X - 16f, headPos.Y - (player.height * 0.5f + 20f));

            float fraction = Math.Clamp(cooldown / (float)KatanaPlayer.MaxDashCooldown, 0f, 1f);

            spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X - 1, (int)barPos.Y - 1, 34, 7), Color.Black);

            spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, 32, 5), new Color(20, 20, 20));

            int fillW = (int)Math.Ceiling(32f * fraction);
            if (fillW > 0)
            {
                spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, fillW, 5), new Color(230, 25, 25));
            }
        }

        private static void DrawDashStreaks(SpriteBatch spriteBatch, KatanaPlayer katanaPlayer)
        {
            if (katanaPlayer.ActiveStreaks.Count == 0 || WhitePixel == null || WhitePixel.IsDisposed)
                return;

            foreach (var streak in katanaPlayer.ActiveStreaks)
            {
                float progress = (float)streak.TimeLeft / streak.MaxTimeLeft;
                float alpha = progress * progress;

                Vector2 startScreen = streak.StartCenter - Main.screenPosition;
                Vector2 endScreen = streak.EndCenter - Main.screenPosition;
                Vector2 centerScreen = (startScreen + endScreen) / 2f;
                float length = streak.Distance;
                float rot = streak.Rotation;

                Vector2 perp = streak.Direction.RotatedBy(MathHelper.PiOver2);
                if (perp.Y < 0f)
                {
                    perp = -perp;
                }

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen - perp * 19f,
                    null,
                    new Color(205, 255, 10) * (alpha * 0.95f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 3.5f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen - perp * 13f,
                    null,
                    new Color(165, 255, 30) * (alpha * 0.85f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 10f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen - perp * 6f,
                    null,
                    new Color(110, 255, 190) * (alpha * 0.80f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 8f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen,
                    null,
                    new Color(245, 255, 255) * (alpha * 0.95f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 20f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen + perp * 4f,
                    null,
                    new Color(0, 245, 255) * (alpha * 1.0f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 4f),
                    SpriteEffects.None,
                    0f
                );
                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen + perp * 4f,
                    null,
                    Color.White * (alpha * 0.95f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 1.5f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen + perp * 12f,
                    null,
                    new Color(45, 130, 255) * (alpha * 0.85f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 10f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    centerScreen + perp * 19f,
                    null,
                    new Color(160, 25, 255) * (alpha * 0.95f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(length, 3.5f),
                    SpriteEffects.None,
                    0f
                );

                int ribCount = Math.Max(1, (int)(length / 4f));
                for (int r = 0; r <= ribCount; r++)
                {
                    float ribT = r / (float)ribCount;
                    Vector2 ribPos = startScreen + streak.Direction * (ribT * length);
                    float ribAlpha = alpha * (0.18f + 0.12f * (float)Math.Sin(r * 1.8f));
                    spriteBatch.Draw(
                        WhitePixel,
                        ribPos,
                        null,
                        new Color(210, 245, 255) * ribAlpha,
                        rot + MathHelper.PiOver2,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(36f, 1.5f),
                        SpriteEffects.None,
                        0f
                    );
                }

                DrawSolidCircle(spriteBatch, startScreen, 18f, new Color(140, 255, 100) * (alpha * 0.5f));
                DrawSolidCircle(spriteBatch, startScreen, 9f, Color.White * (alpha * 0.85f));

                DrawSolidCircle(spriteBatch, endScreen, 24f, new Color(0, 245, 255) * (alpha * 0.6f));
                DrawSolidCircle(spriteBatch, endScreen, 12f, Color.White * (alpha * 0.9f));
            }
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            if (WhitePixel == null || WhitePixel.IsDisposed)
                return;

            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.001f)
                return;

            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(
                WhitePixel,
                start,
                null,
                color,
                angle,
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int cutIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Entity Health Bars"));
            if (cutIndex == -1) cutIndex = 0;
            layers.Insert(cutIndex, new LegacyGameInterfaceLayer(
                "KatanaZERO: Screen Cut Line",
                delegate
                {
                    DrawDyingEnemyGlitches(Main.spriteBatch);
                    DrawScreenCutLines(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.Game
            ));

            var batteryLayer = new LegacyGameInterfaceLayer(
                "KatanaZERO: Battery Bar",
                delegate
                {
                    DrawBatteryBar(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI
            );

            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                layers.Insert(resourceBarIndex, batteryLayer);
            }
            else
            {
                layers.Add(batteryLayer);
            }

            int logic4Index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Interface Logic 4"));
            if (logic4Index != -1)
            {
                layers.Insert(logic4Index, new LegacyGameInterfaceLayer(
                    "KatanaZERO: Before Mouse Text Draw",
                    delegate
                    {
                        if (Main.HoverItem != null && !Main.HoverItem.IsAir && (Main.HoverItem.ModItem is IKatanaWeapon || Main.HoverItem.ModItem?.Mod is KatanaZERO) && KatanaZERO.enableKatanaZeroFont)
                        {
                            KatanaGlobalItem.OrigOpaqueSetting = Main.SettingsEnabled_OpaqueBoxBehindTooltips;
                            KatanaGlobalItem.DrawCustomTooltipBox = Main.SettingsEnabled_OpaqueBoxBehindTooltips;
                            Main.SettingsEnabled_OpaqueBoxBehindTooltips = false;
                            KatanaGlobalItem.SuppressedVanillaBox = true;
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));

                layers.Add(new LegacyGameInterfaceLayer(
                    "KatanaZERO: After Mouse Text Draw",
                    delegate
                    {
                        if (KatanaGlobalItem.SuppressedVanillaBox)
                        {
                            Main.SettingsEnabled_OpaqueBoxBehindTooltips = KatanaGlobalItem.OrigOpaqueSetting;
                            KatanaGlobalItem.SuppressedVanillaBox = false;
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }

        private static void DrawDyingEnemyGlitches(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (katanaPlayer.ActiveDyingEnemyGlitches.Count == 0)
                return;

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            foreach (var glitch in katanaPlayer.ActiveDyingEnemyGlitches)
            {
                if (glitch.Texture == null || glitch.Texture.IsDisposed)
                    continue;

                float progress = (float)glitch.TimeLeft / glitch.MaxTimeLeft;
                float alpha = progress * (0.80f + 0.20f * (float)Math.Sin(glitch.TimeLeft * 1.8f));

                Vector2 screenPos = glitch.WorldPosition - Main.screenPosition;
                Color magentaOuter = new Color(245, 45, 235) * (alpha * 0.75f);
                Color cyanOuter = new Color(0, 215, 255) * (alpha * 0.55f);
                Color whiteCore = new Color(255, 240, 255) * (alpha * 0.90f);

                spriteBatch.Draw(glitch.Texture, screenPos + new Vector2(-4f, 0f), glitch.SourceRect, magentaOuter, glitch.Rotation, glitch.Origin, glitch.Scale, glitch.Effects, 0f);
                spriteBatch.Draw(glitch.Texture, screenPos + new Vector2(4f, 0f), glitch.SourceRect, magentaOuter, glitch.Rotation, glitch.Origin, glitch.Scale, glitch.Effects, 0f);
                spriteBatch.Draw(glitch.Texture, screenPos + new Vector2(0f, -2.5f), glitch.SourceRect, cyanOuter, glitch.Rotation, glitch.Origin, glitch.Scale, glitch.Effects, 0f);
                spriteBatch.Draw(glitch.Texture, screenPos + new Vector2(0f, 2.5f), glitch.SourceRect, cyanOuter, glitch.Rotation, glitch.Origin, glitch.Scale, glitch.Effects, 0f);
                spriteBatch.Draw(glitch.Texture, screenPos, glitch.SourceRect, whiteCore, glitch.Rotation, glitch.Origin, glitch.Scale, glitch.Effects, 0f);
            }

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
        }

        private static void DrawScreenCutLines(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (katanaPlayer.ActiveScreenCuts.Count == 0 || WhitePixel == null || WhitePixel.IsDisposed)
                return;

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            foreach (var cut in katanaPlayer.ActiveScreenCuts)
            {
                float progress = (float)cut.TimeLeft / cut.MaxTimeLeft;
                float alpha = progress * progress;

                Vector2 originScreen = cut.WorldOrigin - Main.screenPosition;
                float rot = cut.Angle;
                Vector2 dir = new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot));
                Vector2 perp = new Vector2(-dir.Y, dir.X);

                bool isEvenFrame = (cut.TimeLeft % 2 == 0);
                Color outerColor = isEvenFrame ? new Color(0, 245, 255) : new Color(245, 25, 220);
                Color coreColor = isEvenFrame ? Color.White : new Color(255, 230, 255);
                Color chromaticOffsetColor = isEvenFrame ? new Color(245, 25, 220) : new Color(0, 245, 255);

                float lineLength = Math.Max(Main.screenWidth, Main.screenHeight) * 3f;

                spriteBatch.Draw(
                    WhitePixel,
                    originScreen,
                    null,
                    outerColor * (alpha * 0.45f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 44f * progress),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    originScreen,
                    null,
                    outerColor * (alpha * 0.85f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 18f * progress),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    originScreen,
                    null,
                    coreColor * (alpha * 0.98f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 6f),
                    SpriteEffects.None,
                    0f
                );

                spriteBatch.Draw(
                    WhitePixel,
                    originScreen,
                    null,
                    Color.White * (alpha * 1.0f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 3f),
                    SpriteEffects.None,
                    0f
                );

                float glitchOffset = isEvenFrame ? 5f : -5f;
                spriteBatch.Draw(
                    WhitePixel,
                    originScreen + perp * glitchOffset,
                    null,
                    chromaticOffsetColor * (alpha * 0.70f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 3.5f),
                    SpriteEffects.None,
                    0f
                );
                spriteBatch.Draw(
                    WhitePixel,
                    originScreen - perp * (glitchOffset * 0.6f),
                    null,
                    outerColor * (alpha * 0.60f),
                    rot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(lineLength, 2.5f),
                    SpriteEffects.None,
                    0f
                );

                DrawSolidCircle(spriteBatch, originScreen, 36f * progress, outerColor * (alpha * 0.6f));
                DrawSolidCircle(spriteBatch, originScreen, 18f * progress, Color.White * (alpha * 0.9f));
            }

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
        }

        private void DrawBatteryBar(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead || Main.playerInventory)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (!katanaPlayer.IsHoldingFifteen)
                return;

            Texture2D batteryFrame = ModContent.Request<Texture2D>("KatanaZERO/UI/spr_hud_battery").Value;
            Texture2D partDragon = ModContent.Request<Texture2D>("KatanaZERO/UI/spr_hud_battery_part_dragon").Value;

            if (batteryFrame == null || partDragon == null)
                return;

            int buffCount = 0;
            for (int b = 0; b < player.buffType.Length; b++)
            {
                if (player.buffType[b] > 0)
                    buffCount++;
            }
            int buffRows = buffCount > 0 ? (buffCount - 1) / 11 + 1 : 0;
            float baseY = 76f + buffRows * 50f;
            Vector2 basePos = new Vector2(26f, baseY);
            float scale = 2f;

            spriteBatch.Draw(batteryFrame, basePos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            float battery = katanaPlayer.Battery;
            Color drainedRed = new Color(255, 30, 30);
            int totalCells = 11;
            float cellValue = KatanaPlayer.MaxBattery / totalCells;

            for (int i = 0; i < totalCells; i++)
            {
                Vector2 cellPos = basePos + new Vector2(11 + i * 5, 4) * scale;
                bool isCellCharged = battery >= (i + 1) * cellValue;

                if (isCellCharged)
                {
                    spriteBatch.Draw(partDragon, cellPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    spriteBatch.Draw(partDragon, cellPos, null, drainedRed, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}
