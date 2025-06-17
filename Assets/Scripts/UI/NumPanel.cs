using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class NumPanel : MonoBehaviour
{
    public CanvasGroup cg;
    float y;
    Text contentTxt;

    private void Awake()
    {
        contentTxt = GetComponent<Text>();
        
    }


    public void Init(string s)
    {
        y = transform.localPosition.y;
        contentTxt.text = s;
        transform.DOLocalMoveY(y, 0.2f).OnComplete(delegate
        {
            cg.DOFade(0, 1);
            transform.DOLocalMoveY(y+25f, 1).OnComplete(delegate
            {
                Destroy(gameObject);
            });
        });
    }
}
