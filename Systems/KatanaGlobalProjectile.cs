using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using KatanaZERO.Items.FifteensBlade;
using KatanaZERO.Items.ZerosKatana;
using KatanaZERO.Items.Claymore;
using KatanaZERO.Items.PrismSword;
using KatanaZERO.Items.PhoenixEdge;
using KatanaZERO.Items.SavantKnife;
using KatanaZERO.Items.MasterSword;

namespace KatanaZERO.Systems
{
    public class KatanaGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool HasHitAnyNPC = false;
        public bool HasDeflectedAnyProjectile = false;
        public HashSet<int> HitRootEntityIds = new HashSet<int>();

        public static bool IsKatanaSlash(Projectile projectile)
        {
            return projectile.ModProjectile is FifteensSlash
                || projectile.ModProjectile is ZeroKatanaSlash
                || projectile.ModProjectile is ClaymoreSlash
                || projectile.ModProjectile is PrismSwordSlash
                || projectile.ModProjectile is PhoenixSlash
                || projectile.ModProjectile is SavantSlash
                || projectile.ModProjectile is MasterSwordSlash;
        }

        public static bool CanDeflect(Projectile slash)
        {
            if (slash.TryGetGlobalProjectile<KatanaGlobalProjectile>(out var glob))
            {
                if (glob.HasHitAnyNPC)
                    return false;
            }
            return true;
        }

        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            if (IsKatanaSlash(projectile))
            {
                if (HasHitAnyNPC || HasDeflectedAnyProjectile)
                    return false;

                int rootId = KatanaZERO.GetRootEntityId(target);
                if (HitRootEntityIds.Contains(rootId) || HitRootEntityIds.Contains(target.whoAmI))
                    return false;
            }
            return base.CanHitNPC(projectile, target);
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsKatanaSlash(projectile))
            {
                HasHitAnyNPC = true;
                int rootId = KatanaZERO.GetRootEntityId(target);
                HitRootEntityIds.Add(rootId);
                HitRootEntityIds.Add(target.whoAmI);
                projectile.friendly = false;
            }
        }
    }
}
