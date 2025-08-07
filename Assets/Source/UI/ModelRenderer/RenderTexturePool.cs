using System.Collections.Generic;
using UnityEngine;

namespace BrawlLine.ModelRenderer
{
    public class RenderTexturePool
    {
        private readonly int maxCapacity;
        private readonly List<RenderTexture> textures;

        private static readonly RenderTexturePool SharedPool = new(maxCapacity: 10);

        public RenderTexturePool(int maxCapacity)
        {
            this.maxCapacity = maxCapacity;
            textures = new List<RenderTexture>(this.maxCapacity);
        }

        public static RenderTexture AcquireTexture(int width, int height, int depth, RenderTextureFormat format)
        {
            return SharedPool.AcquireTextureInternal(width, height, depth, format);
        }

        public static void ReleaseTexture(RenderTexture texture)
        {
            if (texture != null)
            {
                SharedPool.ReleaseTextureInternal(texture);
            }
        }

        private RenderTexture AcquireTextureInternal(int width, int height, int depth, RenderTextureFormat format)
        {
            RemoveNullTextures();

            for (int i = textures.Count - 1; i >= 0; i--)
            {
                RenderTexture texture = textures[i];

                if (texture.width == width &&
                    texture.height == height &&
                    texture.depth == depth &&
                    texture.format == format)
                {
                    textures.RemoveAt(i);
                    return texture;
                }
            }

            return new RenderTexture(width, height, depth, format)
            {
                name = "Pooled Render Texture " + Random.Range(1000, 9999)
            };
        }

        private void RemoveNullTextures()
        {
            for (int i = textures.Count - 1; i >= 0; i--)
            {
                if (textures[i] == null)
                {
                    textures.RemoveAt(i);
                }
            }
        }

        private void EnforcePoolCapacity()
        {
            while (textures.Count > maxCapacity)
            {
                if (textures[0] != null)
                {
                    textures[0].Release();
                    Object.Destroy(textures[0]);
                }

                textures.RemoveAt(0);
            }
        }

        private void ReleaseTextureInternal(RenderTexture texture)
        {
            if (!textures.Contains(texture))
            {
                texture.Release();
                textures.Add(texture);
            }

            EnforcePoolCapacity();
        }
    }
}