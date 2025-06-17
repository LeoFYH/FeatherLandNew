using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Panel界面动画基类
/// </summary>
public class PopupPanelBase : MonoBehaviour
{
    protected CanvasGroup _group;
    protected Vector3 _originScale;

    protected virtual void Awake()
    {
        if (!gameObject.TryGetComponent(out _group))
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _originScale = transform.localScale;
    }

    /// <summary>
    /// 展示界面动画
    /// </summary>
    public virtual void OnShowPanel()
    {
        gameObject.SetActive(true);
        _group.alpha = 0;
        transform.localScale = new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z);
        transform.DOScale(_originScale, 0.2f).SetEase(Ease.InSine);
        _group.DOFade(1f, 0.2f).SetEase(Ease.InSine);
    }

    /// <summary>
    /// 隐藏界面动画
    /// </summary>
    public virtual void OnHidePanel()
    {
        transform.DOScale(new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z), 0.2f).SetEase(Ease.OutSine);
        _group.DOFade(0f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
