using System;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP_InputField 컴포넌트를 래핑하여 스크립트를 통해 제어합니다.
/// UI_Image를 상속받아 배경(Image) 조작 API(Color/Sprite/Alpha)도 함께 제공합니다.
/// Placeholder 텍스트는 로컬라이징 대상이라 Refs에 UI_Text로 캐싱해두고 사용합니다.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class UI_Input : UI_Image
{
    #region Serializable Types

    [Serializable]
    public class Refs
    {
        public UI_Text txtText; // 실제 입력 텍스트 (TMP_InputField.textComponent와 동일 대상 — 폰트/색/사이즈 동적 변경용)
        public UI_Text txtPlaceholder; // 플레이스홀더 안내 텍스트 (로컬라이징 대상)
    }

    #endregion

    #region Fields

    public Refs refs;

    [SerializeField, HideInInspector] private TMP_InputField _inputField; // 직렬화 캐시 — Reset/OnValidate가 자동 채움

    #endregion

    #region Properties

    public TMP_InputField InputField
    {
        get
        {
            if (_inputField == null)
            {
                _inputField = GetComponent<TMP_InputField>();
                UnityEngine.Debug.LogError($"[UI_Input] _inputField not serialized — runtime GetComponent fallback. Run Tools/Package/UI Foundation/Migrate Component Refs. (gameObject={name})", this);
            }
            return _inputField;
        }
    }
    public string Text
    {
        get => InputField.text;
        set => InputField.text = value;
    }
    public bool Interactable
    {
        get => InputField.interactable;
        set => InputField.interactable = value;
    }
    public TMP_InputField.ContentType ContentType
    {
        get => InputField.contentType;
        set => InputField.contentType = value;
    }
    public bool IsFocused => InputField.isFocused;

    #endregion

    #region Unity Lifecycle

    protected override void Reset()
    {
        base.Reset();
#if UNITY_EDITOR
        _inputField = GetComponent<TMP_InputField>();
#endif
    }

    protected override void OnValidate()
    {
        base.OnValidate();
#if UNITY_EDITOR
        if (_inputField == null) _inputField = GetComponent<TMP_InputField>();
#endif
    }

    #endregion

    #region Public Methods

    /// <summary>입력 필드의 텍스트를 비웁니다.</summary>
    public void Clear() => InputField.text = string.Empty;

    /// <summary>입력 필드에 키보드 포커스를 즉시 부여합니다.</summary>
    public void Focus() => InputField.ActivateInputField();

    #endregion
}
