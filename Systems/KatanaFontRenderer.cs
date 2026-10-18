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

        public const int CellWidth = 11;
        public const int CellHeight = 22;
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
            // Explicit aliases & fallbacks
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

            // Try lowercase or uppercase
            if (char.IsLower(c) && GlyphMap.TryGetValue(char.ToUpper(c), out index))
                return index;
            if (char.IsUpper(c) && GlyphMap.TryGetValue(char.ToLower(c), out index))
                return index;

            // Fallback for missing characters
            if (GlyphMap.TryGetValue('?', out index))
                return index;

            return 1; // space
        }

        public static Rectangle GetSourceRect(int glyphIndex)
        {
            int col = glyphIndex % AtlasCols;
            int row = glyphIndex / AtlasCols;
            return new Rectangle(col * CellWidth, row * CellHeight, CellWidth, CellHeight);
        }

        public static Vector2 MeasureString(string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var snippets = ChatManager.ParseMessage(text, Color.White);
            float totalWidth = 0f;
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrEmpty(snippet.Text))
                    continue;
                totalWidth += snippet.Text.Length * (CellWidth * scale);
            }
            return new Vector2(totalWidth, CellHeight * scale);
        }

        public static void DrawLine(SpriteBatch spriteBatch, DrawableTooltipLine line, float scale = 1f)
        {
            Texture2D tex = GetTexture();
            if (tex == null || tex.IsDisposed)
                return;

            List<TextSnippet> snippets = ChatManager.ParseMessage(line.Text, line.Color);
            Vector2 curPos = new Vector2(line.X, line.Y);

            // Subtle Katana Zero dark drop-shadow
            Vector2 shadowOffset = new Vector2(1.5f, 1.5f) * scale;
            Color shadowColor = new Color(0, 0, 0, 200);

            // Pass 1: Draw drop shadows for all characters
            Vector2 passPos = curPos;
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrEmpty(snippet.Text))
                    continue;

                for (int i = 0; i < snippet.Text.Length; i++)
                {
                    char c = snippet.Text[i];
                    if (c == ' ' || c == '\xa0')
                    {
                        passPos.X += CellWidth * scale;
                        continue;
                    }

                    int gIndex = GetGlyphIndex(c);
                    Rectangle src = GetSourceRect(gIndex);
                    spriteBatch.Draw(tex, passPos + shadowOffset, src, shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    passPos.X += CellWidth * scale;
                }
            }

            // Pass 2: Draw colored glyphs
            passPos = curPos;
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
                        passPos.X += CellWidth * scale;
                        continue;
                    }

                    int gIndex = GetGlyphIndex(c);
                    Rectangle src = GetSourceRect(gIndex);
                    spriteBatch.Draw(tex, passPos, src, charColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    passPos.X += CellWidth * scale;
                }
            }
        }
    }
}
