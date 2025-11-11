using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public static class PokemonSpriteAtlasProvider
{
    private static readonly Dictionary<string, SpriteAtlas> CachedAtlases = new Dictionary<string, SpriteAtlas>();
    private static bool _initialized;
    private static SpriteAtlas _tmpAtlas;
    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        SpriteAtlasManager.atlasRegistered += OnAtlasRegistered;
        SpriteAtlasManager.atlasRequested += OnAtlasRequested;
        _initialized = true;
    }

    private static void OnAtlasRegistered(SpriteAtlas atlas)
    {
        if (atlas == null)
        {
            return;
        }

        CachedAtlases[atlas.name] = atlas;
    }

    private static void AssignAtlas(SpriteAtlas loaded)
    {
        _tmpAtlas = loaded;
    }
    private static void OnAtlasRequested(string atlasName, System.Action<SpriteAtlas> onCompleted)
    {
        SpriteAtlas atlas = Resources.Load<SpriteAtlas>(atlasName);
        if (atlas == null)
        {
            onCompleted?.Invoke(null);
            return;
        }

        CachedAtlases[atlas.name] = atlas;
        onCompleted?.Invoke(atlas);
    }

    public static SpriteAtlas GetAtlas(string atlasName)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(atlasName))
        {
            return null;
        }

        if (CachedAtlases.TryGetValue(atlasName, out SpriteAtlas cached))
        {
            return cached;
        }

        SpriteAtlas atlas = Resources.Load<SpriteAtlas>(atlasName);
        if (atlas != null)
        {
            CachedAtlases[atlas.name] = atlas;
            return atlas;
        }
        OnAtlasRequested(atlasName, AssignAtlas);
        atlas = _tmpAtlas;
        _tmpAtlas = null;
        return atlas;
    }

    public static void Clear()
    {
        CachedAtlases.Clear();
    }
}
