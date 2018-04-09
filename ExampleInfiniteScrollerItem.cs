/*
*** INFINITE SCROLLER ***
*** v. 1.2 ***
  * EXAMPLE ITEM *

[!] Это пример кода, который отвечает за отображение нужной информации на элементе списка
*/

using UnityEngine;
using UnityEngine.UI;

public class ExampleInfiniteScrollerItem : MonoBehaviour
{
    public Text text;
    private int index;

    private void Awake()
    {
        if (!text)
            text = GetComponentInChildren<Text>();
    }

    private void Start()
    {
        var item = GetComponent<InfiniteScrollerItem>();
        if (item)
        {
            item.OnChanged += Changed;
            item.CallOnChanged(item.index);
        }
    }

    private void OnDestroy()
    {
        var item = GetComponent<InfiniteScrollerItem>();
        if (item)
            item.OnChanged -= Changed;
    }

    public void OnClick()
    {
        Debug.Log("Click button #" + index);
    }

    private void Changed(int _index)
    {
        index = _index;
        text.text = index.ToString();
    }    
}
