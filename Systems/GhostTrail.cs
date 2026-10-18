using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KatanaZERO.Systems
{
    public class GhostSilhouette
    {
        public Vector2 WorldPosition;
        public Vector2 BasePlayerPosition;
        public Vector2 BaseScreenPosition;
        public List<DrawData> DrawDataList;
        public int TimeLeft;
        public int MaxTimeLeft;
        public float GlitchOffset;
        public Vector2 DashDirection;
    }

    public class GhostTrailLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => PlayerDrawLayers.BeforeFirstVanillaLayer;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            // Do not draw for shadow clones or if player is not local/active
            if (drawInfo.shadow != 0f)
                return;

            var katanaPlayer = drawInfo.drawPlayer.GetModPlayer<KatanaPlayer>();
            if (katanaPlayer.ActiveGhosts.Count == 0 && katanaPlayer.ActiveSlowMoGhosts.Count == 0)
            {
                katanaPlayer.LastGhostDrawDataCount = 0;
                return;
            }

            int addedGhostDrawData = 0;
            int colorOnlyShader = Terraria.ID.ContentSamples.CommonlyUsedContentSamples.ColorOnlyShaderIndex;

            // 1. Render constant slow-mo trailing ghosts (~5-10 short ghosts tightly trailing Fifteen and fading fast)
            foreach (var smGhost in katanaPlayer.ActiveSlowMoGhosts)
            {
                if (smGhost.DrawDataList == null || smGhost.DrawDataList.Count == 0)
                    continue;

                float smProgress = (float)smGhost.TimeLeft / smGhost.MaxTimeLeft;
                float smAlpha = smProgress * smProgress * 0.70f;

                Vector2 currentGhostScreenPos = smGhost.WorldPosition - Main.screenPosition;
                Vector2 capturedPlayerScreenPos = smGhost.BasePlayerPosition - smGhost.BaseScreenPosition;

                foreach (var dd in smGhost.DrawDataList)
                {
                    if (dd.texture == null)
                        continue;

                    Vector2 localOffset = dd.position - capturedPlayerScreenPos;
                    DrawData smDd = dd;
                    smDd.position = currentGhostScreenPos + localOffset;
                    smDd.shader = colorOnlyShader;
                    smDd.color = new Color(0, 195, 255) * smAlpha;
                    smDd.ignorePlayerRotation = true;
                    drawInfo.DrawDataCache.Add(smDd);
                    addedGhostDrawData++;
                }
            }

            // 2. Render dense Dragon Dash ghost silhouettes (5-frame snappy fade)
            foreach (var ghost in katanaPlayer.ActiveGhosts)
            {
                if (ghost.DrawDataList == null || ghost.DrawDataList.Count == 0)
                    continue;

                float progress = (float)ghost.TimeLeft / ghost.MaxTimeLeft;
                float alpha = progress; // Linear 5-frame fade: frames 3, 2, 1 are semitransparent

                // World position anchored screen offset (accounts for camera scroll and player dash)
                Vector2 currentGhostScreenPos = ghost.WorldPosition - Main.screenPosition;
                Vector2 capturedPlayerScreenPos = ghost.BasePlayerPosition - ghost.BaseScreenPosition;

                Vector2 cyanOffset = new Vector2(-ghost.GlitchOffset, 0f);
                Vector2 magentaOffset = new Vector2(ghost.GlitchOffset, 0f);

                // Cyan offset ghost silhouette (Katana Zero chromatic glitching)
                foreach (var dd in ghost.DrawDataList)
                {
                    if (dd.texture == null)
                        continue;

                    Vector2 localOffset = dd.position - capturedPlayerScreenPos;
                    DrawData cyanDd = dd;
                    cyanDd.position = currentGhostScreenPos + localOffset + cyanOffset;
                    cyanDd.shader = colorOnlyShader;
                    cyanDd.color = new Color(0, 240, 255) * (alpha * 0.80f);
                    cyanDd.ignorePlayerRotation = true;
                    drawInfo.DrawDataCache.Add(cyanDd);
                    addedGhostDrawData++;
                }

                // Magenta offset ghost silhouette (Katana Zero chromatic glitching)
                foreach (var dd in ghost.DrawDataList)
                {
                    if (dd.texture == null)
                        continue;

                    Vector2 localOffset = dd.position - capturedPlayerScreenPos;
                    DrawData magDd = dd;
                    magDd.position = currentGhostScreenPos + localOffset + magentaOffset;
                    magDd.shader = colorOnlyShader;
                    magDd.color = new Color(255, 20, 200) * (alpha * 0.80f);
                    magDd.ignorePlayerRotation = true;
                    drawInfo.DrawDataCache.Add(magDd);
                    addedGhostDrawData++;
                }

                // Whitened core silhouette
                foreach (var dd in ghost.DrawDataList)
                {
                    if (dd.texture == null)
                        continue;

                    Vector2 localOffset = dd.position - capturedPlayerScreenPos;
                    DrawData whiteDd = dd;
                    whiteDd.position = currentGhostScreenPos + localOffset;
                    whiteDd.shader = colorOnlyShader;
                    whiteDd.color = Color.White * (alpha * 0.95f);
                    whiteDd.ignorePlayerRotation = true;
                    drawInfo.DrawDataCache.Add(whiteDd);
                    addedGhostDrawData++;
                }
            }
            katanaPlayer.LastGhostDrawDataCount = addedGhostDrawData;
        }
    }

    public class GhostCaptureLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f)
                return;

            var katanaPlayer = drawInfo.drawPlayer.GetModPlayer<KatanaPlayer>();
            int ghostCount = katanaPlayer.LastGhostDrawDataCount;
            if (drawInfo.DrawDataCache.Count > ghostCount)
            {
                var cleanPlayerData = new List<DrawData>();
                for (int i = ghostCount; i < drawInfo.DrawDataCache.Count; i++)
                {
                    cleanPlayerData.Add(drawInfo.DrawDataCache[i]);
                }
                katanaPlayer.CapturePlayerDrawData(cleanPlayerData, drawInfo.Position, Main.screenPosition);
            }
            else if (ghostCount == 0 && drawInfo.DrawDataCache.Count > 0)
            {
                katanaPlayer.CapturePlayerDrawData(drawInfo.DrawDataCache, drawInfo.Position, Main.screenPosition);
            }
        }
    }
}
