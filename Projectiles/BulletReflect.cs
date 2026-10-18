using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace KatanaZERO.Projectiles
{
    public class BulletReflect : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 15;
            Main.projFrames[Projectile.type] = 5;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.9f, 0.95f, 1.0f);

            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 5)
                {
                    Projectile.Kill();
                    return;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.frame < 0 || Projectile.frame >= 5)
                return false;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRect,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
