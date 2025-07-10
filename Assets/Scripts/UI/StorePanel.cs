using UnityEngine;
using UnityEngine.UI;

public class StorePanel : PopupPanelBase
{
    //public Text coinTxt;
    public Button btn_Buy;
    public GameObject HatchPage;

    protected override void Awake()
    {
        base.Awake();
        btn_Buy.onClick.AddListener(Buy);
    }

    public void Init()
    {
        //coinTxt.text = GameManager.Instance.coin.ToString();
        //gameObject.SetActive(true);
        OnShowPanel();
    }

    public void Close()
    {
        //gameObject.SetActive(false);
        OnHidePanel();
    }

    private void Buy()
    {
        Debug.Log("buy");
        // if (GameManager.Instance.noOpenEggs > 0)
        // {
        //     UIManager.Instance.CreatePrompt("There are also eggs that have not hatched");
        //     return;
        // }
        //
        // if (GameManager.Instance.coin >= GameManager.Instance.eggPackage)
        // {
        //     GameManager.Instance.coin -= GameManager.Instance.eggPackage;
        //     //coinTxt.text = GameManager.Instance.coin.ToString();
        //     UIManager.Instance.coinTxt.text = GameManager.Instance.coin.ToString();
        //     GameManager.Instance.CreateBrid();
        //     Close();
        //     //HatchPage.SetActive(true);
        // }
        // else
        // {
        //     UIManager.Instance.CreatePrompt("Insufficient coins");
        // }
    }
}
