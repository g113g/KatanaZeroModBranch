using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.Localization;

namespace KatanaZERO.Systems
{
    public class RecipeGroups : ModSystem
    {
        public override void AddRecipeGroups()
        {
            RecipeGroup silverBarGroup = new RecipeGroup(
                () => Language.GetTextValue("LegacyMisc.37") + " Silver Bar",
                new int[]
                {
                ItemID.SilverBar,
                ItemID.TungstenBar
                }
            );
            RecipeGroup.RegisterGroup("KatanaZERO:SilverBars", silverBarGroup);
        }
    }

}
