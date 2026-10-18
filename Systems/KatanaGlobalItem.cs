using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace KatanaZERO.Systems
{
    public class KatanaGlobalItem : GlobalItem
    {
        public static bool SuppressedVanillaBox = false;
        public static bool OrigOpaqueSetting = true;
        public static bool DrawCustomTooltipBox = true;

        public override bool CanUseItem(Item item, Player player)
        {
            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (katanaPlayer.DragonDashCooldown > 0 && (item.ModItem is IKatanaWeapon || item.ModItem?.Mod == Mod))
            {
                return false;
            }
            return base.CanUseItem(item, player);
        }

        public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
        {
            if (item.ModItem?.Mod != Mod || !KatanaZERO.enableKatanaZeroFont)
                return true;

            if (SuppressedVanillaBox)
            {
                Main.SettingsEnabled_OpaqueBoxBehindTooltips = OrigOpaqueSetting;
                SuppressedVanillaBox = false;
            }

            float maxLineWidth = 0f;
            float totalHeight = 0f;
            float titleSpacing = 30f;
            float lineSpacing = 25f;

            foreach (var line in lines)
            {
                float scale = line.Name == "ItemName" ? 1.2f : 1.05f;
                Vector2 size = KatanaFontRenderer.MeasureString(line.Text, scale);
                if (size.X > maxLineWidth)
                    maxLineWidth = size.X;

                totalHeight += (line.Name == "ItemName" ? titleSpacing : lineSpacing);
            }

            int padLeft = 14;
            int padRight = 14;
            int padTop = 9;
            int padBottom = 7;

            int boxWidth = (int)Math.Ceiling(maxLineWidth) + padLeft + padRight;
            int boxHeight = (int)Math.Ceiling(totalHeight) + padTop + padBottom;

            int boxX = x - padLeft;
            int boxY = y - padTop;

            if (boxX + boxWidth > Main.screenWidth - 6)
                boxX = Main.screenWidth - boxWidth - 6;
            if (boxX < 6)
                boxX = 6;
            if (boxY + boxHeight > Main.screenHeight - 6)
                boxY = Main.screenHeight - boxHeight - 6;
            if (boxY < 6)
                boxY = 6;

            if (DrawCustomTooltipBox)
            {
                Utils.DrawInvBG(Main.spriteBatch, new Rectangle(boxX, boxY, boxWidth, boxHeight), new Color(23, 25, 81, 255) * 0.925f);
            }

            float curY = boxY + padTop;
            foreach (var line in lines)
            {
                float scale = line.Name == "ItemName" ? 1.2f : 1.05f;
                Color baseColor;
                if (line.Name == "ItemName")
                {
                    baseColor = Terraria.GameContent.UI.ItemRarity.GetColor(item.rare);
                }
                else if (line.OverrideColor.HasValue)
                {
                    baseColor = line.OverrideColor.Value;
                }
                else if (line.IsModifier)
                {
                    baseColor = line.IsModifierBad ? new Color(190, 120, 120) : new Color(120, 190, 120);
                }
                else
                {
                    baseColor = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
                }

                KatanaFontRenderer.DrawLineAt(Main.spriteBatch, line.Text, new Vector2(boxX + padLeft, curY), baseColor, scale);
                curY += line.Name == "ItemName" ? titleSpacing : lineSpacing;
            }

            return false;
        }
    }
}
