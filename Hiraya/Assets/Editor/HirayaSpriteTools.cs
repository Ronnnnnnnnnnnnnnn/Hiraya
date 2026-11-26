using System.Collections.Generic;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>Small pixel helpers used when the Fin / Fiona sprites are prepared (see HirayaCharacterSetup).</summary>
    public static class HirayaSpriteTools
    {
        /// <summary>
        /// Makes every near-white pixel that can be reached from the image border fully transparent.
        /// The white shirt and shoes inside the black outline are not reachable, so they stay white.
        /// </summary>
        public static void ClearEdgeWhite(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var stack = new Stack<int>();

            void Push(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                int i = y * width + x;
                if (visited[i]) return;
                Color32 c = pixels[i];
                if (c.a == 0 || (c.r >= 245 && c.g >= 245 && c.b >= 245))
                {
                    visited[i] = true;
                    stack.Push(i);
                }
            }

            for (int x = 0; x < width; x++) { Push(x, 0); Push(x, height - 1); }
            for (int y = 0; y < height; y++) { Push(0, y); Push(width - 1, y); }

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                pixels[i] = new Color32(255, 255, 255, 0);
                int x = i % width;
                int y = i / width;
                Push(x + 1, y);
                Push(x - 1, y);
                Push(x, y + 1);
                Push(x, y - 1);
            }
        }
    }
}
