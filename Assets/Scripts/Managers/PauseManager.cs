using DG.Tweening;
using UnityEngine;

public class PauseManager : PopupPanelBase
{
    private bool isPause = false;
    public GameObject PauseMenu;

    protected override void Awake()
    {
        if (!gameObject.TryGetComponent(out _group))
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _originScale = PauseMenu.transform.localScale;
    }

    public void OpenPauseMenu()
    {
        //PauseMenu.SetActive(true);
        OnShowPanel();
        isPause = true;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPause)
            {
                PauseMenu.SetActive(false);
                isPause = false;
            }
            else
            {
                PauseMenu.SetActive(true);
                isPause = true;
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void closeTab()
    {
        PauseMenu.SetActive(false);
        isPause = false;
    }


    public override void OnShowPanel()
    {
        PauseMenu.SetActive(true);
        _group.alpha = 0;
        PauseMenu.transform.localScale = new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z);
        PauseMenu.transform.DOScale(_originScale, 0.2f).SetEase(Ease.InSine);
        _group.DOFade(1f, 0.2f).SetEase(Ease.InSine);
    }

    public override void OnHidePanel()
    {
        PauseMenu.transform.DOScale(new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z), 0.2f).SetEase(Ease.OutSine);
        _group.DOFade(0f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            PauseMenu.SetActive(false);
        });
    }
}
