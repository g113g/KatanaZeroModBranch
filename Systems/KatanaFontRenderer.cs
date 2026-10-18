using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace KatanaZERO.Systems
{
    public static class KatanaFontRenderer
    {
        private const string GlyphString = "\xa0 Q_T%YmMWVpqgyD@?ESGA3B6547CX9L8ORP0H&2UNFK/\\j<>vewsJzrbaZd$hcnfuox*~+kt])=}[{(I1-,i;l^|!.:\"`'ÄäÀàÂâËëÉéÈèÊêÏïÎîŒœÖöÔôÙùÛûÜüẞßÇçÑñÁáÃãÆæÍíÓóÚú…⁃‐‑‒–—―АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяё¿¡Õõª";

        private static readonly Dictionary<char, int> GlyphMap = new Dictionary<char, int>();
        private static Asset<Texture2D> fontTexture;

        public const int GlyphWidth = 11;
        public const int GlyphHeight = 22;
        public const int Padding = 4;
        public const int SlotWidth = GlyphWidth + Padding * 2;
        public const int SlotHeight = GlyphHeight + Padding * 2;
        public const int AtlasCols = 16;

        static KatanaFontRenderer()
        {
            for (int i = 0; i < GlyphString.Length; i++)
            {
                char c = GlyphString[i];
                if (!GlyphMap.ContainsKey(c))
                {
                    GlyphMap[c] = i;
                }
            }
            GlyphMap[' '] = 1;
            GlyphMap['\xa0'] = 0;
            if (!GlyphMap.ContainsKey('Ё')) GlyphMap['Ё'] = GlyphMap.ContainsKey('Е') ? GlyphMap['Е'] : 1;
        }

        public static Texture2D GetTexture()
        {
            if (fontTexture == null || fontTexture.Value == null || fontTexture.Value.IsDisposed)
            {
                fontTexture = ModContent.Request<Texture2D>("KatanaZERO/UI/Fonts/vcr_font_tiny", AssetRequestMode.ImmediateLoad);
            }
            return fontTexture.Value;
        }

        public static int GetGlyphIndex(char c)
        {
            if (GlyphMap.TryGetValue(c, out int index))
                return index;

            if (char.IsLower(c) && GlyphMap.TryGetValue(char.ToUpper(c), out index))
                return index;
            if (char.IsUpper(c) && GlyphMap.TryGetValue(char.ToLower(c), out index))
                return index;

            if (GlyphMap.TryGetValue('?', out index))
                return index;

            return 1;
        }

        public static Rectangle GetSourceRect(int glyphIndex)
        {
            int col = glyphIndex % AtlasCols;
            int row = glyphIndex / AtlasCols;
            return new Rectangle(col * SlotWidth + Padding, row * SlotHeight + Padding, GlyphWidth, GlyphHeight);
        }

        public static Vector2 MeasureString(string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var snippets = ChatManager.ParseMessage(text, Color.White);
            float totalWidth = 0f;
            float step = (float)Math.Round(GlyphWidth * scale);
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrEmpty(snippet.Text))
                    continue;
                totalWidth += snippet.Text.Length * step;
            }
            return new Vector2(totalWidth, (float)Math.Round(GlyphHeight * scale));
        }

        public static void DrawLineAt(SpriteBatch spriteBatch, string text, Vector2 position, Color baseColor, float scale = 1f)
        {
            Texture2D tex = GetTexture();
            if (tex == null || tex.IsDisposed || string.IsNullOrEmpty(text))
                return;

            List<TextSnippet> snippets = ChatManager.ParseMessage(text, baseColor);
            float startX = (float)Math.Round((double)position.X);
            float startY = (float)Math.Round((double)position.Y);
            float charStep = (float)Math.Round(GlyphWidth * scale);

            Vector2 shadowOffset = new Vector2(1f, 1f);
            Color shadowColor = new Color(0, 0, 0, 200);

            float curX = startX;
            float curY = startY;
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrEmpty(snippet.Text))
                    continue;

                for (int i = 0; i < snippet.Text.Length; i++)
                {
                    char c = snippet.Text[i];
                    if (c == ' ' || c == '\xa0')
                    {
                        curX += charStep;
                        continue;
                    }

                    int gIndex = GetGlyphIndex(c);
                    Rectangle src = GetSourceRect(gIndex);
                    Vector2 drawPos = new Vector2((float)Math.Round(curX), (float)Math.Round(curY));
                    spriteBatch.Draw(tex, drawPos + shadowOffset, src, shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    curX += charStep;
                }
            }

            curX = startX;
            curY = startY;
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrEmpty(snippet.Text))
                    continue;

                Color charColor = snippet.Color;

                for (int i = 0; i < snippet.Text.Length; i++)
                {
                    char c = snippet.Text[i];
                    if (c == ' ' || c == '\xa0')
                    {
                        curX += charStep;
                        continue;
                    }

                    int gIndex = GetGlyphIndex(c);
                    Rectangle src = GetSourceRect(gIndex);
                    Vector2 drawPos = new Vector2((float)Math.Round(curX), (float)Math.Round(curY));
                    spriteBatch.Draw(tex, drawPos, src, charColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    curX += charStep;
                }
            }
        }
    }
}
