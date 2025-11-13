using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// @ 스프라이트 아틀라스 로더
/// @ Resources 경로에서 SpriteAtlas를 불러오고, 내부 스프라이트를 이름으로 반환
/// </summary>
public static class PokemonSpriteAtlasProvider
{
    private static Dictionary<string, SpriteAtlas> _cache = new Dictionary<string, SpriteAtlas>();

    /// <summary>
    /// @ atlasResourceName: Resources 폴더 기준 아틀라스 리소스 이름
    /// @ spriteName: 아틀라스 내부 스프라이트 이름
    /// </summary>
    public static Sprite GetSprite(string atlasResourceName, string spriteName)
    {
        if (string.IsNullOrEmpty(atlasResourceName))
        {
            return null;
        }

        SpriteAtlas atlas;
        bool ok = _cache.TryGetValue(atlasResourceName, out atlas);
        if (!ok || atlas == null)
        {
            atlas = Resources.Load<SpriteAtlas>(atlasResourceName);
            if (atlas != null)
            {
                _cache[atlasResourceName] = atlas;
            }
        }

        if (atlas == null)
        {
            return null;
        }

        return atlas.GetSprite(spriteName);
    }
}
