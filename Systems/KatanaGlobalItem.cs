using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace KatanaZERO.Systems
{
    public class KatanaGlobalItem : GlobalItem
    {
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.ModItem?.Mod != Mod)
                return true;

            if (!KatanaZERO.enableKatanaZeroFont)
                return true;

            float scale = line.Name == "ItemName" ? 1.1f : 1.0f;
            KatanaFontRenderer.DrawLine(Main.spriteBatch, line, scale);
            return false;
        }
    }
}
