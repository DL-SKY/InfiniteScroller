/*
*** INFINITE SCROLLER ***
*** v. 1.2 ***
  * SCROLL ITEM *

[!] Этот компонент будет добавлен автоматически на каждый элемент списка при инициализации компонента "InfiniteScrollerController"
*/

using UnityEngine;

public class InfiniteScrollerItem : MonoBehaviour
{
    #region Variables
    public delegate void OnChangedDelegate(int _indexNew);
    public event OnChangedDelegate OnChanged;
    public int index = 0;
    #endregion

    #region Methods
    public void CallOnChanged(int _indexNew)
    {        
        index = _indexNew;

        //Вызываем событие
        if (OnChanged != null)
            OnChanged(_indexNew);
    }
    #endregion
}