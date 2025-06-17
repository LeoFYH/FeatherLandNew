using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nest : MonoBehaviour
{
    Brid brid;
    
    public float createTime = 10;
    public BoxCollider bc;
    GameObject smallBrid;
    bool isIn;
    bool haveSmallBrid;
    public GameObject egg;
    
    public float reduceCreateTime = 0.5f;
    public bool isFarest;

    public void Init(Brid brid)
    {
        haveSmallBrid = false;
        this.brid = brid;
        createTime = 10;
        isIn = true;
    }

    private void OnMouseDown()
    {
        createTime -= reduceCreateTime;
        GameManager.Instance.CreateNum("speed up", Input.mousePosition);
    }

    private void Update()
    {
        if (brid == null && smallBrid == null)
        {
            if (!GameManager.Instance.nests.Contains(this))
            {
                GameManager.Instance.nests.Add(this);
            }
            isIn = false;
        }
        if (!isIn || haveSmallBrid) return;
        if (createTime <= 0)
        {
            if (smallBrid == null)
            {
                haveSmallBrid = true;
                bc.enabled = false;
                egg.SetActive(false);
                smallBrid = Instantiate(brid.gameObject);
                smallBrid.GetComponent<Brid>().enabled = false;
                smallBrid.transform.position = transform.position;
                smallBrid.transform.localScale = isFarest? Vector3.one * 0.04f : Vector3.one * 0.06f;
            }
        }
        else
        {
            if (brid.isInNest)
            {
                createTime -= Time.deltaTime;
                bc.enabled = false;
            }
            else
            {
                bc.enabled = true;
            }
        }
    }
}
