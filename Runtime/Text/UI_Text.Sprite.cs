using TMPro;

/// <summary>UI_Text — TMP Sprite Asset 지정과 인라인 sprite 태그 생성을 지원합니다.</summary>
public partial class UI_Text
{
    #region Properties

    public TMP_SpriteAsset SpriteAsset
    {
        get
        {
            EnsureInit();
            return _txt.spriteAsset;
        }
        set
        {
            EnsureInit();
            _txt.spriteAsset = value;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>TMP가 인라인 sprite 태그를 조회할 Sprite Asset을 설정합니다.</summary>
    public UI_Text SetSpriteAsset(TMP_SpriteAsset spriteAsset)
    {
        SpriteAsset = spriteAsset;
        return this;
    }

    /// <summary>앞뒤 텍스트 사이에 이름 기반 sprite 태그를 넣고 Rich Text를 활성화합니다.</summary>
    public UI_Text SetTextWithSprite(string prefix, string spriteName, string suffix = "", bool tint = false)
    {
        string spriteTag = BuildSpriteTag(spriteName, tint);
        if (string.IsNullOrEmpty(spriteTag)) return this;

        EnsureInit();
        _txt.richText = true;
        Text = string.Concat(prefix, spriteTag, suffix);
        return this;
    }

    /// <summary>현재 TMP Sprite Asset에서 이름으로 조회할 인라인 sprite 태그를 만듭니다.</summary>
    public static string BuildSpriteTag(string spriteName, bool tint = false)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            UnityEngine.Debug.LogError("[UI_Text] Sprite name is empty");
            return string.Empty;
        }

        return tint
            ? $"<sprite name=\"{spriteName}\" tint=1>"
            : $"<sprite name=\"{spriteName}\">";
    }

    /// <summary>현재 TMP Sprite Asset에서 인덱스로 조회할 인라인 sprite 태그를 만듭니다.</summary>
    public static string BuildSpriteTag(int spriteIndex, bool tint = false)
    {
        if (spriteIndex < 0)
        {
            UnityEngine.Debug.LogError($"[UI_Text] Sprite index is invalid: {spriteIndex}");
            return string.Empty;
        }

        return tint
            ? $"<sprite={spriteIndex} tint=1>"
            : $"<sprite={spriteIndex}>";
    }

    #endregion
}
