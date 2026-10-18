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
            WhitePixel?.Dispose();
            WhitePixel = null;
        }

        public override void PostDrawTiles()
        {
            if (WhitePixel == null || WhitePixel.IsDisposed)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();

            // Render overhead dash cooldown bar (Katana Zero red bar above Fifteen's head)
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

            float intensity = KatanaPlayer.LocalSlowMoIntensity;
            if (intensity <= 0.001f)
                return;

            // R2. Layer-based darkening behind NPCs and projectiles.
            // Screen space (Matrix.Identity) ensures complete 100% viewport coverage without edge bleed.
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

            // 1. Draw range circle (bold circumference, 3.5f width)
            int segments = 180;
            Vector2 prevPoint = playerCenterScreen + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi / segments * i;
                Vector2 nextPoint = playerCenterScreen + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                DrawLine(spriteBatch, prevPoint, nextPoint, cyanCircle, 3.5f);
                prevPoint = nextPoint;
            }

            // 2. Target vector towards cursor
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

            // 3. Single solid continuous bold cyan laser line
            DrawLine(spriteBatch, playerCenterScreen, targetScreen, cyanCircle, 3.5f);

            // 4. Solid bold cyan dot at trajectory end (with soft outer glow and bright center)
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

            // Black outer border
            spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X - 1, (int)barPos.Y - 1, 34, 7), Color.Black);

            // Dark inner background
            spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, 32, 5), new Color(20, 20, 20));

            // Katana Zero red cooldown fill (shrinks as cooldown completes)
            int fillW = (int)Math.Ceiling(32f * fraction);
            if (fillW > 0)
            {
                spriteBatch.Draw(WhitePixel, new Rectangle((int)barPos.X, (int)barPos.Y, fillW, 5), new Color(230, 25, 25));
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
        }

        private void DrawBatteryBar(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead || Main.playerInventory)
                return;

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            // Only display battery HUD when holding FifteensBlade
            if (!katanaPlayer.IsHoldingFifteen)
                return;

            Texture2D batteryFrame = ModContent.Request<Texture2D>("KatanaZERO/UI/spr_hud_battery").Value;
            Texture2D partDragon = ModContent.Request<Texture2D>("KatanaZERO/UI/spr_hud_battery_part_dragon").Value;

            if (batteryFrame == null || partDragon == null)
                return;

            // R3. Render authentic Katana Zero segmented battery bar
            // Dynamically position below player buffs to avoid HUD overlap
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

            // Draw outer battery frame
            spriteBatch.Draw(batteryFrame, basePos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Draw 11 battery segments centered inside 9..66 chamber (starts at X=10)
            // Discharges discretely cell-by-cell exactly like Katana Zero
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
                    // Full charged cell: authentic gold/amber with magenta borders
                    spriteBatch.Draw(partDragon, cellPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    // Drained cell: authentic Katana Zero bright red block
                    spriteBatch.Draw(partDragon, cellPos, null, drainedRed, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}
