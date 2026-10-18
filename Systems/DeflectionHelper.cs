using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using KatanaZERO.Projectiles;

namespace KatanaZERO.Systems
{
    public static class DeflectionHelper
    {
        private static readonly SoundStyle DeflectSound = new SoundStyle("KatanaZERO/Sounds/bullet_deflect")
        {
            Volume = 0.65f,
            PitchVariance = 0.08f,
            MaxInstances = 2
        };

        public static bool Deflect(Projectile slash, Projectile bullet, int damageMultiplier)
        {
            if (!KatanaGlobalProjectile.CanDeflect(slash))
                return false;

            if (slash.TryGetGlobalProjectile<KatanaGlobalProjectile>(out var glob))
            {
                glob.HasDeflectedAnyProjectile = true;
            }

            // In Katana Zero, deflecting consumes the swing's attack capability against enemies
            slash.friendly = false;

            // Deflection immediately resets attack animation and weapon cooldown, allowing instant parry spamming
            Player player = Main.player[slash.owner];
            player.itemAnimation = 0;
            player.itemTime = 0;
            player.reuseDelay = 0;

            if (player.HeldItem?.ModItem is IKatanaWeapon kw)
            {
                kw.ResetAttackCooldown();
            }

            bullet.hostile = false;
            bullet.friendly = true;
            bullet.owner = slash.owner;
            bullet.DamageType = DamageClass.Melee;
            bullet.timeLeft = System.Math.Max(bullet.timeLeft, 180);

            if (bullet.velocity == Vector2.Zero)
            {
                bullet.velocity = (bullet.Center - slash.Center).SafeNormalize(new Vector2(1f, 0f)) * 14f;
            }
            else
            {
                Vector2 defDir = -bullet.velocity.SafeNormalize(slash.Center - bullet.Center);
                float defSpeed = MathHelper.Clamp(bullet.velocity.Length() * 1.5f, 14f, 28f);
                bullet.velocity = defDir * defSpeed;
            }
            bullet.damage = slash.damage * damageMultiplier;
            bullet.netUpdate = true;
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(Terraria.ID.MessageID.SyncProjectile, -1, -1, null, bullet.whoAmI);
            }

            // Find exact contact point between slash and incoming projectile
            Rectangle intersect = Rectangle.Intersect(slash.getRect(), bullet.getRect());
            Vector2 contactPoint = intersect.IsEmpty ? bullet.Center : intersect.Center.ToVector2();

            // Parry angle: direction bullet is deflected towards
            float parryAngle = bullet.velocity.ToRotation();

            // Spawn dedicated 5-frame spr_bulletreflect animation projectile
            int proj = Projectile.NewProjectile(
                slash.GetSource_FromThis(),
                contactPoint,
                Vector2.Zero,
                ModContent.ProjectileType<BulletReflect>(),
                0,
                0f,
                slash.owner
            );

            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].Center = contactPoint;
                Main.projectile[proj].rotation = parryAngle;
            }

            // Audio sync with Katana Zero's deflection sound
            SoundEngine.PlaySound(DeflectSound, contactPoint);
            return true;
        }
    }
}
