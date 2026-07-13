using System;
using UnityEngine;

/// <summary>
/// UI_Input + UI_Button 조합 컴포넌트.
/// 버튼을 누르면 Input의 현재 텍스트를 등록된 액션(Action&lt;string&gt;)으로 전달하여
/// 텍스트 기반 처리를 실행합니다.
/// </summary>
public class UI_InputBtn : MonoBehaviour
{
    #region Serializable Types

    [Serializable]
    public class Refs
    {
        public UI_Input input; // 입력 필드
        public UI_Button button; // 실행 버튼
    }

    #endregion

    #region Fields

    public Refs refs;

    private Action<string> _onSubmit; // 버튼 클릭 시 Input 텍스트를 전달받아 실행할 액션

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (refs.button == null)
        {
            UnityEngine.Debug.LogError("[UI_InputBtn] refs.button is null — check Inspector binding");
            return;
        }
        refs.button.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (refs.button != null) refs.button.RemoveListener(OnClick);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 버튼 클릭 시 실행할 액션을 등록합니다.
    /// 액션의 인자로 Input의 현재 텍스트가 전달되며, 재호출 시 기존 액션을 대체합니다.
    /// </summary>
    public void SetAction(Action<string> onSubmit) => _onSubmit = onSubmit;

    #endregion

    #region Event Handlers

    // 버튼 클릭 → Input의 현재 텍스트를 등록된 액션에 전달
    private void OnClick()
    {
        if (refs.input == null)
        {
            UnityEngine.Debug.LogError("[UI_InputBtn] refs.input is null — check Inspector binding");
            return;
        }
        _onSubmit?.Invoke(refs.input.Text);
    }

    #endregion
}
