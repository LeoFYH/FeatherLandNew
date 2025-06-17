using UnityEngine;
using UnityEngine.UI;

public class NotebookButton : MonoBehaviour
{
    public NotebookUI notebookUI;  // 改为public以便在Inspector中查看
    private Button button;

    private void Awake()
    {
        Debug.Log("NotebookButton Awake");
        button = GetComponent<Button>();
    }

    private void Start()
    {
        
    }

    public void MenuNotebookButtonClick()
    {
        Debug.Log("NotebookButton clicked!");
        
        if (notebookUI == null)
        {
            Debug.LogError("NotebookUI is null!");
            return;
        }

        //notebookUI.notebookPanel.SetActive(true);
        notebookUI.OnShowPanel();
    }
    
    public void CloseNotebookButtonClick()
    {
        Debug.Log("NotebookButton clicked!");
        
        if (notebookUI == null)
        {
            Debug.LogError("NotebookUI is null!");
            return;
        }

        //notebookUI.notebookPanel.SetActive(false);
        notebookUI.OnHidePanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
