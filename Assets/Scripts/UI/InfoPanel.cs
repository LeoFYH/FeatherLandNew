using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : PopupPanelBase
{
    public static InfoPanel instance;
    public Text titleTxt;
    public Text descTxt;
    public Text levelTxt;
    public Text priceText;
    public Image progressFill;
    public Image IntimacyFill;
    public Image cursor;
    int price;
    GameObject go;

    public void Init(GameObject go, int price, string s1, string s2, int level, float progress,float progress2,bool cursorOn)
    {
        //gameObject.SetActive(true);
        OnShowPanel();
        this.go = go;
        this.price = price;
        titleTxt.text = s1;
        descTxt.text = s2;
        levelTxt.text = $"<color=yellow>{level}</color>/min";
        progressFill.fillAmount = progress;
        IntimacyFill.fillAmount= progress2;
        priceText.text = $"Sale x{price}";
        cursor.gameObject.SetActive(cursorOn);
    }

    public void Sell()
    {
        // GameManager.Instance.coin += price;
        // UIManager.Instance.coinTxt.text = GameManager.Instance.coin.ToString();
        Close();
        Destroy(go);
    }

    public void Close()
    {
        //gameObject.SetActive(false);
        OnHidePanel();
    }

    public void ToggleBar()
    {
        progressFill.gameObject.SetActive(false);
        IntimacyFill.gameObject.SetActive(true);
        cursor.gameObject.SetActive(true);

    }
    
    public override void OnShowPanel()
    {
        gameObject.SetActive(true);
        var rect = transform as RectTransform;
        rect.anchoredPosition = new Vector2(-rect.sizeDelta.x * transform.localScale.x * 0.5f, rect.anchoredPosition.y);
        rect.DOAnchorPosX(rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.InSine);
    }

    public override void OnHidePanel()
    {
        var rect = transform as RectTransform;
        rect.DOAnchorPosX(-rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
