using System.Collections.Generic;
using UnityEngine;

namespace MageLock.ModelRenderer
{
    public class RenderTexturePool
    {
        private readonly int _maxCapacity;
        private readonly List<RenderTexture> _textures;

        private static readonly RenderTexturePool SharedPool = new(maxCapacity: 10);

        public RenderTexturePool(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _textures = new List<RenderTexture>(_maxCapacity);
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

            for (int i = _textures.Count - 1; i >= 0; i--)
            {
                RenderTexture texture = _textures[i];

                if (texture.width == width &&
                    texture.height == height &&
                    texture.depth == depth &&
                    texture.format == format)
                {
                    _textures.RemoveAt(i);
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
            for (int i = _textures.Count - 1; i >= 0; i--)
            {
                if (_textures[i] == null)
                {
                    _textures.RemoveAt(i);
                }
            }
        }

        private void EnforcePoolCapacity()
        {
            while (_textures.Count > _maxCapacity)
            {
                if (_textures[0] != null)
                {
                    _textures[0].Release();
                    Object.Destroy(_textures[0]);
                }

                _textures.RemoveAt(0);
            }
        }

        private void ReleaseTextureInternal(RenderTexture texture)
        {
            if (!_textures.Contains(texture))
            {
                texture.Release();
                _textures.Add(texture);
            }

            EnforcePoolCapacity();
        }
    }
}