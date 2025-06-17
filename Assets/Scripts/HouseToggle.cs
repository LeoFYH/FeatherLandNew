using System;
using UnityEngine;
using UnityEngine.UI;

public class HouseToggle : MonoBehaviour
{
    public GameObject menu;

    private Toggle thisToggle;

    private void Start()
    {
        thisToggle = GetComponent<Toggle>();
        thisToggle.onValueChanged.AddListener(isOn =>
        {
            menu.SetActive(isOn);
        });

        thisToggle.isOn = false;
        menu.SetActive(false);
    }
}
