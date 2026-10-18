using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using KatanaZERO.Dusts;
using KatanaZERO.Systems;
using System;
using Player = Terraria.Player;
using System.IO;
using System.Collections.Generic;
using static Terraria.ModLoader.ModLoader;

namespace KatanaZERO.Items.FifteensBlade
{
    public class FifteensBlade : ModItem, IKatanaWeapon
    {
        //Please forgive me
        public static SoundStyle GetSlashSound(string swordName, int slashNumber)
        {
            return new SoundStyle($"KatanaZERO/Sounds/Items/{swordName}/slash{slashNumber}");
        }

        public static readonly SoundStyle Slash1 = GetSlashSound("Katana", 1);
        public static readonly SoundStyle Slash2 = GetSlashSound("Katana", 2);
        public static readonly SoundStyle Slash3 = GetSlashSound("Katana", 3);

        public static readonly SoundStyle PrismSlash1 = GetSlashSound("PrismSword", 1);
        public static readonly SoundStyle PrismSlash2 = GetSlashSound("PrismSword", 2);
        public static readonly SoundStyle PrismSlash3 = GetSlashSound("PrismSword", 3);

        public static readonly SoundStyle MasterSlash1 = GetSlashSound("MasterSword", 1);
        public static readonly SoundStyle MasterSlash2 = GetSlashSound("MasterSword", 2);
        public static readonly SoundStyle MasterSlash3 = GetSlashSound("MasterSword", 3);

        public static readonly SoundStyle SavantSlash1 = GetSlashSound("SavantKnife", 1);
        public static readonly SoundStyle SavantSlash2 = GetSlashSound("SavantKnife", 2);
        public static readonly SoundStyle SavantSlash3 = GetSlashSound("SavantKnife", 3);

        public static readonly SoundStyle ClaymoreSlash1 = GetSlashSound("Claymore", 1);
        public static readonly SoundStyle ClaymoreSlash2 = GetSlashSound("Claymore", 2);
        public static readonly SoundStyle ClaymoreSlash3 = GetSlashSound("Claymore", 3);

        public static readonly SoundStyle PhoenixSlash1 = GetSlashSound("PhoenixEdge", 1);
        public static readonly SoundStyle PhoenixSlash2 = GetSlashSound("PhoenixEdge", 2);
        public static readonly SoundStyle PhoenixSlash3 = GetSlashSound("PhoenixEdge", 3);

        public static readonly SoundStyle FifteenSlash1 = GetSlashSound("FifteensBlade", 1);
        public static readonly SoundStyle FifteenSlash2 = GetSlashSound("FifteensBlade", 2);
        public static readonly SoundStyle FifteenSlash3 = GetSlashSound("FifteensBlade", 3);

        public static readonly SoundStyle Special1 = new SoundStyle("KatanaZERO/Sounds/Items/FifteensBlade/fifteen_special1");
        public static readonly SoundStyle Special2 = new SoundStyle("KatanaZERO/Sounds/Items/FifteensBlade/fifteen_special2");
        public static readonly SoundStyle Special3 = new SoundStyle("KatanaZERO/Sounds/Items/FifteensBlade/fifteen_special3");

        public static readonly SoundStyle SlowEngage = new SoundStyle("KatanaZERO/Sounds/slomo_engage");
        public static readonly SoundStyle SlowDisengage = new SoundStyle("KatanaZERO/Sounds/slomo_disengage");

        public float attackCooldown = 0f;
        public void ResetAttackCooldown() => attackCooldown = 0f;
        public float dragonCooldown = 0f;

        public int SlowMoCounter = 0;
        public int PowerLevel;
        public float Y;
        public float X;


        public Color glitchColor = ColorHelper.HexToColor("#00ccff");

        public bool hasAttacked = false;
        public bool playedSlomoEngage = false;
        public bool hasRightClicked = false;
        public bool hasReleasedRightClick = false;
        public bool rightClickActivated = false;
        public bool playerFreeze = false;




        public override void SetDefaults()
        {
            string selectedWeaponSound = KatanaZERO.fifteensKatanaSound;
            PowerLevelManager power = new();

            power.Apply(Item); //THIS. IS. POWER. 

            Item.DamageType = DamageClass.Melee; //That was Raw Metal reference?
            Item.width = 40;
            Item.height = 40;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.sellPrice(gold: 99, silver: 99);
            Item.UseSound = GetRandomWeaponSound(selectedWeaponSound);
            Item.knockBack = 4;

            Item.useTime = 20;
            if (HasMod("FargowiltasSouls"))
            {
                Item.useAnimation = 20;
            }
            else
            {
                Item.useAnimation = 1;
            }

            Item.useStyle = ItemUseStyleID.Rapier;
            Item.holdStyle = ItemHoldStyleID.HoldRadio;

            Item.autoReuse = false;
            Item.noUseGraphic = true;
            Item.noMelee = true;

            Item.shoot = ModContent.ProjectileType<FifteensSlash>();
        }

        public override bool? UseItem(Player player)
        {
            Vector2 direction = Main.MouseWorld - player.position;
            direction.Normalize();

            player.direction = direction.X > 0 ? 1 : -1;

            if (KatanaZERO.enableLunge)
            {
                if (hasAttacked)
                {
                    player.velocity = direction * 5f;
                }
                else
                {
                    player.velocity = direction * 12f;
                    hasAttacked = true;
                }
            }

            attackCooldown = 20f; //artificial cooldown

            Random random = new Random();
            int randomNumber = random.Next(1, 4);

            string selectedWeaponSound = KatanaZERO.fifteensKatanaSound;

            SoundStyle weaponSound = GetRandomWeaponSound(selectedWeaponSound);

            switch (randomNumber)
            {
                case 1:
                    Item.UseSound = weaponSound;
                    break;
                case 2:
                    Item.UseSound = weaponSound;
                    break;
                case 3:
                    Item.UseSound = weaponSound;
                    break;
            }
            SetDefaults();
            return true;
        }

        public override void HoldItem(Player player)
        {
            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            katanaPlayer.IsHoldingFifteen = true;

            if (!katanaPlayer.IsSlowingTime && !Main.mouseRight)
            {
                hasRightClicked = false;
            }

            if (dragonCooldown <= 0 && katanaPlayer.DragonDashCooldown <= 0)
            {
                if (!Main.mouseItem.IsAir || Main.HoveringOverAnNPC || Main.playerInventory || Main.InGuideCraftMenu || Main.InReforgeMenu)
                {
                    if (hasRightClicked)
                    {
                        hasRightClicked = false;
                        katanaPlayer.EndSlowMo();
                    }
                    return;
                }

                if (Main.mouseRight && !hasRightClicked)
                {
                    if (katanaPlayer.CanUseSlowMo())
                    {
                        hasRightClicked = true;
                        katanaPlayer.StartSlowMo();
                    }
                }

                if (Main.mouseRight && hasRightClicked)
                {
                    if (katanaPlayer.CanUseSlowMo())
                    {
                        player.ChangeDir(Main.MouseWorld.X >= player.Center.X ? 1 : -1);
                        if (KatanaZERO.enableTimeShift)
                        {
                            katanaPlayer.SlowDownWorld();
                        }
                    }
                    else
                    {
                        hasRightClicked = false;
                        katanaPlayer.EndSlowMo();
                    }
                }
                else if (!Main.mouseRight && hasRightClicked)
                {
                    hasRightClicked = false;
                    katanaPlayer.EndSlowMo();

                    if (dragonCooldown <= 0f && katanaPlayer.CanDash())
                    {
                        DragonDash(player);
                        katanaPlayer.ConsumeDashBattery();
                    }
                }
            }
        }

        public override void UpdateInventory(Player player)
        {
            if (player.velocity.Y == 0f)
            {
                hasAttacked = false;
            }

            if (attackCooldown > 0f)
            {
                attackCooldown -= 1f;
            }

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (katanaPlayer.DragonDashCooldown > 0)
            {
                dragonCooldown = katanaPlayer.DragonDashCooldown;
            }
            else if (dragonCooldown > 0f)
            {
                dragonCooldown -= 1f;
                katanaPlayer.DragonDashCooldown = (int)dragonCooldown;
            }
        }
        public override bool CanUseItem(Player player)
        {
            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();
            if (Main.mouseRight == false && attackCooldown <= 0f && dragonCooldown <= 0f && katanaPlayer.DragonDashCooldown <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool AltFunctionUse(Player player) => false;

        public void DragonDash(Player player)
        {
            if (Main.myPlayer != player.whoAmI || Main.HoveringOverAnNPC || Main.playerInventory || Main.InGuideCraftMenu || Main.InReforgeMenu)
            {
                return;
            }
            SetDefaults();

            var katanaPlayer = player.GetModPlayer<KatanaPlayer>();

            Vector2 cursorPosition = Main.MouseWorld;
            Vector2 playerPosition = player.Center;

            float maxRadius = 464f;
            float distanceToCursor = Vector2.Distance(playerPosition, cursorPosition);
            Vector2 directionToCursor;
            if (distanceToCursor < 1f)
            {
                directionToCursor = new Vector2(player.direction, 0f);
                distanceToCursor = 0f;
            }
            else
            {
                directionToCursor = (cursorPosition - playerPosition).SafeNormalize(new Vector2(player.direction, 0f));
                if (directionToCursor.X != 0f)
                {
                    player.direction = directionToCursor.X > 0 ? 1 : -1;
                }
                if (distanceToCursor > maxRadius)
                {
                    distanceToCursor = maxRadius;
                }
            }
            cursorPosition = playerPosition + directionToCursor * distanceToCursor;
            float moveDistance = distanceToCursor;

            float dashSpeed = 520f;
            float dashTime = moveDistance / dashSpeed;
            float dashTicks = dashTime * 60;

            player.immune = true;
            player.immuneTime = ((int)dashTicks < 30) ? 30 : ((int)dashTicks * 2);

            int hitboxWidth = player.width * 3;
            int hitboxHeight = player.height * 3;

            Vector2 dashStep = (directionToCursor * (dashSpeed / 60f));
            Vector2 dashStartCenter = player.Center;

            Dictionary<int, bool> hitEnemies = new Dictionary<int, bool>();
            List<Vector2> dashTrailPositions = new List<Vector2>();

            dashTrailPositions.Add(player.position);

            float accumulatedDistance = 0f;
            float trailStepDistance = 2.5f;

            katanaPlayer.IsDragonDashing = true;
            bool anyKilledDuringDash = false;
            Vector2 killOriginSum = Vector2.Zero;
            int killCount = 0;

            for (float i = 0; i < dashTicks; i++)
            {
                Vector2 nextPosition = player.position + dashStep;

                if (Collision.SolidCollision(nextPosition, player.width, player.height))
                {
                    break;
                }

                player.position = nextPosition;
                accumulatedDistance += dashStep.Length();

                if (accumulatedDistance >= trailStepDistance)
                {
                    dashTrailPositions.Add(player.position);
                    accumulatedDistance = 0f;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
                }

                Rectangle playerHitbox = new Rectangle((int)player.position.X - hitboxWidth / 2, (int)player.position.Y - hitboxHeight / 2, hitboxWidth, hitboxHeight);

                foreach (NPC enemy in Main.npc)
                {
                    if (!enemy.friendly && enemy.Hitbox.Intersects(playerHitbox) && enemy.netID != 549)
                    {
                        int rootEntityId = KatanaZERO.GetRootEntityId(enemy);

                        if (hitEnemies.ContainsKey(rootEntityId) || hitEnemies.ContainsKey(enemy.whoAmI))
                        {
                            continue;
                        }

                        hitEnemies[rootEntityId] = true;
                        hitEnemies[enemy.whoAmI] = true;

                        int prevLife = enemy.life;
                        if (!enemy.boss)
                        {
                            enemy.SimpleStrikeNPC(Item.damage * 5, 0, false, 0, DamageClass.Melee, true, 0, false);
                        }
                        else
                        {
                            double FivePercentDamage = enemy.lifeMax * 0.025;
                            enemy.SimpleStrikeNPC((Item.damage * 5) + (int)FivePercentDamage, 0, true, 0, DamageClass.Melee, false, 0, false);
                        }

                        if (!enemy.active || enemy.life <= 0 || (prevLife > 0 && enemy.life <= 0))
                        {
                            anyKilledDuringDash = true;
                            killOriginSum += enemy.Center;
                            killCount++;
                            katanaPlayer.SpawnDyingEnemyGlitch(enemy);
                        }
                    }
                }
            }

            katanaPlayer.IsDragonDashing = false;

            if (anyKilledDuringDash && killCount > 0)
            {
                Vector2 avgKillOrigin = killOriginSum / killCount;
                katanaPlayer.SpawnScreenCut(avgKillOrigin, directionToCursor.ToRotation());
            }

            if (dashTrailPositions.Count == 0 || Vector2.Distance(dashTrailPositions[^1], player.position) > 10f)
            {
                dashTrailPositions.Add(player.position);
            }

            Vector2 dashEndCenter = player.Center;

            katanaPlayer.SpawnDashStreak(dashStartCenter, dashEndCenter, directionToCursor);
            katanaPlayer.SpawnGhostTrails(dashTrailPositions, directionToCursor);

            if (player.whoAmI == Main.myPlayer)
            {
                int slashProj = Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    directionToCursor * 0.05f,
                    ModContent.ProjectileType<FifteensSlash>(),
                    Item.damage * 4,
                    Item.knockBack,
                    player.whoAmI
                );

                if (slashProj >= 0 && slashProj < Main.maxProjectiles)
                {
                    var gp = Main.projectile[slashProj].GetGlobalProjectile<KatanaGlobalProjectile>();
                    foreach (var id in hitEnemies.Keys)
                    {
                        gp.HitRootEntityIds.Add(id);
                    }
                }
            }

            Random random = new Random();
            int randomNumber = random.Next(1, 3);
            switch (randomNumber)
            {
                case 1:
                    SoundEngine.PlaySound(Special1);
                    break;
                case 2:
                    SoundEngine.PlaySound(Special2);
                    break;
            }

            player.velocity = Vector2.Zero;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
            }

            dragonCooldown = 180;
            katanaPlayer.DragonDashCooldown = (int)dragonCooldown;
        }

        public override void NetReceive(BinaryReader reader)
        {
            base.NetReceive(reader);
            hasAttacked = reader.ReadBoolean();
            attackCooldown = reader.ReadSingle();
            dragonCooldown = reader.ReadSingle();
            hasRightClicked = reader.ReadBoolean();
            hasReleasedRightClick = reader.ReadBoolean();

            byte packetType = reader.ReadByte();
            switch (packetType)
            {
                case 1: // NPC velocity change
                    int npcID = reader.ReadInt32();
                    Vector2 npcVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    Main.npc[npcID].velocity = npcVelocity;
                    break;
                case 2: // Projectile velocity change
                    int projID = reader.ReadInt32();
                    Vector2 projVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    Main.projectile[projID].velocity = projVelocity;
                    break;
                case 3: // Dust creation
                    Vector2 dustPosition = reader.ReadVector2();
                    int dustType = reader.ReadInt32();
                    Dust.NewDustPerfect(dustPosition, dustType);
                    break;
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            base.NetSend(writer);
            writer.Write(hasAttacked);
            writer.Write(attackCooldown);
            writer.Write(dragonCooldown);
            writer.Write(hasRightClicked);
            writer.Write(hasReleasedRightClick);
        }

        public void SlowDown(Player player) => player.GetModPlayer<KatanaPlayer>().SlowDownWorld();
        public void RevertNormalSpeed() => Main.LocalPlayer?.GetModPlayer<KatanaPlayer>()?.RevertNormalSpeed();
        public void RevertNormalSpeed(Player player) => player.GetModPlayer<KatanaPlayer>().RevertNormalSpeed();
        public void SlomoSoundEffect(Player player)
        {
        }

        public static SoundStyle GetRandomWeaponSound(string weaponSoundOption)
        {
            Random random = new Random();
            int randomSlash = random.Next(1, 4);

            switch (weaponSoundOption)
            {
                case "Zero's Katana":
                    return randomSlash switch
                    {
                        1 => Slash1,
                        2 => Slash2,
                        3 => Slash3,
                        _ => Slash1
                    };
                case "Prism Sword":
                    return randomSlash switch
                    {
                        1 => PrismSlash1,
                        2 => PrismSlash2,
                        3 => PrismSlash3,
                        _ => PrismSlash1
                    };
                case "Master Sword":
                    return randomSlash switch
                    {
                        1 => MasterSlash1,
                        2 => MasterSlash2,
                        3 => MasterSlash3,
                        _ => MasterSlash1
                    };
                case "Savant Knife":
                    return randomSlash switch
                    {
                        1 => SavantSlash1,
                        2 => SavantSlash2,
                        3 => SavantSlash3,
                        _ => SavantSlash1
                    };
                case "Claymore Prototype":
                    return randomSlash switch
                    {
                        1 => ClaymoreSlash1,
                        2 => ClaymoreSlash2,
                        3 => ClaymoreSlash3,
                        _ => ClaymoreSlash1
                    };
                case "Phoenix Edge":
                    return randomSlash switch
                    {
                        1 => PhoenixSlash1,
                        2 => PhoenixSlash2,
                        3 => PhoenixSlash3,
                        _ => PhoenixSlash1
                    };
                case "Dragon's Whisper":
                    return randomSlash switch
                    {
                        1 => FifteenSlash1,
                        2 => FifteenSlash2,
                        3 => FifteenSlash3,
                        _ => FifteenSlash1
                    };
                default:
                    return Slash1;
            }
        }
    }
}