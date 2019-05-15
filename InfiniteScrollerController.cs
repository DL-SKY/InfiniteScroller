/*
*** INFINITE SCROLLER ***
*** v. 1.2 ***
  * SCROLL CONTROLLER *  

[+] Этот компонент необходимо добавить к объекту с "ScrollRect"

[!] Необходимо назначить префаб элемента списка "itemPrefab" 
[!] Необходимо задать расстояние между элементами списка "spacing"
[!] Необходимо задать количество элементов виртуального списка "countVirtualItems"
[!] Если "isInitializeUponStarting = FALSE", то необходимо проинициализировать компонент извне, вызвав метод "Initialize()"

[?] Флаг "isInitializeInCoroutine" отвечает за поштучное создание элементов/карточек списка через кадр при инициализации
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/InfiniteScroller/InfiniteScrollerController")]
[RequireComponent(typeof(ScrollRect))]
public class InfiniteScrollerController : MonoBehaviour
{
    #region Enum
    public enum ScrollerDirection
    {
        Vertical = 0,
        Horizontal = 1,
    }
    #endregion

    #region Variables   
    public bool IsInit { get; private set; }

    [Header("Initialization")]    
    public bool isInitializeUponStarting = true;                    //Флаг инициализации при создании объекта
    public bool isInitializeInCoroutine = false;                    //Флаг инициализации в корутине, элементы/карточки создаются по 1 через кадр
    public ScrollerDirection scrollDirection = ScrollerDirection.Vertical;  //Направление движения скроллера

    [Header("Looped list")]
    public bool isLoop = false;

    [Header("Items")]
    public GameObject itemPrefab;                                   //Префаб элемента списка
    public int countVirtualItems = 50;                              //Количество элементов виртуального списка    
    public float spacing = 0.0f;                                    //Расстояние между двумя элементами списка

    private int height = 64;                                        //Высота элемента списка
    private int width = 64;                                         //Ширина элемента списка

    private ScrollRect scroller;
    private RectTransform content;
    private List<RectTransform> items = new List<RectTransform>();  //Видимые элементы списка +2
    private List<InfiniteScrollerItem> components = new List<InfiniteScrollerItem>();

    private int indexFirstOld = -1;
    private int indexFirst = -1;
    private int indexLastOld = -1;
    private int indexLast = -1;

    private bool isVerified = false;
    #endregion

    #region Unity methods
    private void Awake()
    {
        IsInit = false;
        scroller = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        //Верификация
        isVerified = Verification();
        if (!isVerified)
        {
            Debug.LogWarning("[InfiniteScroller] Verification failed. The initialization will not be performed.");
            return;
        }

        scroller.onValueChanged.AddListener(OnScrollChange);
        content = scroller.content;

        if (isInitializeUponStarting)
            Initialize();
    }

    private void OnDestroy()
    {
        ClearItems();
    }
    #endregion

    #region Public methods    
    public void Initialize(int _indexFirst = 0)
    {
        //Проверка ранее пройденной верификации
        if (!isVerified)
        {
            isVerified = Verification();

            try
            {
                scroller.onValueChanged.AddListener(OnScrollChange);
                content = scroller.content;
            }
            catch { }
        }

        //Повторная проверка
        if (!isVerified)
        {
            Debug.LogWarning("[InfiniteScroller] Verification failed. The initialization will not be performed.");
            return;
        }

        //Настраиваем ScrollRect
        switch (scrollDirection)
        {
            case ScrollerDirection.Vertical:
                scroller.horizontal = false;
                scroller.vertical = true;
                content.pivot = new Vector2(0f, 1f);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);   //Left
                content.sizeDelta = new Vector2(0f, content.sizeDelta.y); //Right
                break;
            case ScrollerDirection.Horizontal:
                scroller.horizontal = true;
                scroller.vertical = false;
                content.pivot = new Vector2(0f, 1f);
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);   //Top
                content.sizeDelta = new Vector2(content.sizeDelta.x, 0f); //Bottom                
                break;
        }
        switch (isLoop)
        {
            case true:
                scroller.movementType = ScrollRect.MovementType.Unrestricted;
                break;
            case false:
                if (scroller.movementType == ScrollRect.MovementType.Unrestricted)
                    scroller.movementType = ScrollRect.MovementType.Elastic;
                break;
        }

        ClearItems();

        //Поштучная инициализация/создание элементов через кадр
        if (isInitializeInCoroutine)
            StartCoroutine(InitializingCoroutine(_indexFirst));
        //Инициализация за один кадр
        else
            Initializing(_indexFirst);        
    }

    public Vector2 GetPositionElement(int _index)
    {
        var result = new Vector2();

        //Определяем размеры элемента списка
        var instanceRect = itemPrefab.GetComponent<RectTransform>();
        height = (int)instanceRect.rect.height;
        width = (int)instanceRect.rect.width;

        if (scrollDirection == ScrollerDirection.Vertical)
            result = new Vector2(0.0f, (-_index * (height + spacing)));
        else if (scrollDirection == ScrollerDirection.Horizontal)
            result = new Vector2((_index * (width + spacing)), 0.0f);

        return result;
    }

    public void Clear()
    {
        ClearItems();
    }
    #endregion

    #region Private methods
    private void Initializing(int _indexFirst)
    {
        CreateAllItems(_indexFirst);
        //Первичный просчет индексов видимых элементов
        OnScrollChange(Vector2.zero);

        IsInit = true;
    }

    [ContextMenu("Re-Initialize")]    
    private void Reinitialize()
    {
        Debug.LogWarning("[InfiniteScroller] Start re-initialization. First index: " + indexFirst);
        Initialize(indexFirst);
        UpdateAllItemsPosition();
    }

    private bool Verification()
    {
        bool result = false;

        result = itemPrefab != null;
        result = result && scroller != null;

        if (result)
            result = result && scroller.viewport.transform.childCount > 0;
        else
            return result;

        if (height <= 0)
            height = 64;
        if (width <= 0)
            width = 64;

        return result;
    }

    private InfiniteScrollerItem GetInfiniteScrollerItem(RectTransform _rect)
    {
        InfiniteScrollerItem result = _rect.GetComponent<InfiniteScrollerItem>();

        if (!result)
        {
            _rect.gameObject.AddComponent<InfiniteScrollerItem>();
            result = _rect.GetComponent<InfiniteScrollerItem>();
        }

        return result;
    }

    private void ClearItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i])
                DestroyImmediate(items[i].gameObject);
        }

        items.Clear();
        components.Clear();
    }

    private void CreateAllItems(int _indexFirst = 0)
    {
        //Определяем размеры элемента списка
        var instanceRect = itemPrefab.GetComponent<RectTransform>();
        height = (int)instanceRect.rect.height;
        width = (int)instanceRect.rect.width;

        //Создаем видимые элементы списка + 2 (дополнительно сверху и снизу)
        int countViews = 0;
        if (scrollDirection == ScrollerDirection.Vertical)
            countViews = Mathf.CeilToInt( scroller.viewport.rect.height / (height + spacing) );
        else if (scrollDirection == ScrollerDirection.Horizontal)
            countViews = Mathf.CeilToInt( scroller.viewport.rect.width / (width + spacing) );
        for (int i = -1; i <= countViews; i++)
            CreateOneItem(i);

        //Устанавливаем размер области Content
        if (scrollDirection == ScrollerDirection.Vertical)
            content.sizeDelta = new Vector2( content.sizeDelta.x, countVirtualItems * (height + spacing) - spacing );
        else if (scrollDirection == ScrollerDirection.Horizontal)
            content.sizeDelta = new Vector2( countVirtualItems * (width + spacing) - spacing, content.sizeDelta.y );

        //Скроллим к нужному элементу
        ScrollToElement(_indexFirst, countVirtualItems);
    }

    private void CreateOneItem(int _index = 0)
    {
        GameObject instanceItem;
        RectTransform instanceRect;

        instanceItem = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity);
        instanceItem.name = _index.ToString();
        instanceItem.transform.SetParent(content);
        instanceItem.transform.localScale = Vector3.one;
        instanceItem.transform.localPosition = Vector3.zero;

        instanceRect = instanceItem.GetComponent<RectTransform>();
        if (scrollDirection == ScrollerDirection.Vertical)
        {
            instanceRect.pivot = new Vector2(0.5f, 1f);         //Pivot
            instanceRect.anchorMin = new Vector2(0f, 1f);       //Anchors
            instanceRect.anchorMax = new Vector2(1f, 1f);       //Anchors
            instanceRect.offsetMax = new Vector2(0f, 0f);       //Left & PosY
            instanceRect.offsetMin = new Vector2(0f, -height);  //Right & Height
        }
        else if (scrollDirection == ScrollerDirection.Horizontal)
        {
            instanceRect.pivot = new Vector2(0f, 1f);           //Pivot
            instanceRect.anchorMin = new Vector2(0f, 0f);       //Anchors
            instanceRect.anchorMax = new Vector2(0f, 1f);       //Anchors
            instanceRect.offsetMax = new Vector2(0f, 0f);       //PosX & Top
            instanceRect.offsetMin = new Vector2(-width, 0f);   //Width & Bottom
        }

        items.Add(instanceRect);
        var component = GetInfiniteScrollerItem(instanceRect);
        components.Add(component);
        
        UpdateItemPosition(_index, _index);
    }

    //Переставить все видимые элементы относительно указанного индекса первого видимого
    private void UpdateAllItemsPosition(int _indexFirst)
    {
        for (int i = 0; i < items.Count; i++)
            UpdateItemPosition(int.Parse(items[i].name), _indexFirst - 1 + i);
    }

    //Обновить все видимые элементы относительно текущего индекса первого видимого
    [ContextMenu("Update All Items")]    
    private void UpdateAllItemsPosition()
    {
        UpdateAllItemsPosition(indexFirst);
    }

    private void UpdateItemPosition(int _indexOld, int _indexNew)
    {
        //Находим индекс видимого элемента списка по старому индексу виртуального списка
        int index = items.FindIndex(x => int.Parse(x.name) == _indexOld);
        if (index < 0 || index >= items.Count)
            return;

        //Устанавливаем новые значения
        RectTransform rect = items[index];
        if (scrollDirection == ScrollerDirection.Vertical)
            rect.anchoredPosition = new Vector2( rect.anchoredPosition.x, (-_indexNew * (height + spacing)) );
        else if (scrollDirection == ScrollerDirection.Horizontal)
            rect.anchoredPosition = new Vector2( (_indexNew * (width + spacing)), rect.anchoredPosition.y );
        rect.name = _indexNew.ToString();

        //Устанавливаем видимость элемента списка
        if (_indexNew < 0 || _indexNew >= countVirtualItems)
        {
            if (isLoop)
            {
                //Сообщаем элементу об изменении индекса
                var _i = 0;

                if (_indexNew < 0)
                {
                    var _TMPi = _indexNew % countVirtualItems;
                    _i = countVirtualItems +_TMPi;
                    if (_i == countVirtualItems) _i = 0;
                }
                else if (_indexNew >= countVirtualItems)
                    _i = _indexNew % countVirtualItems;
                else
                    _i = _indexNew;

                components[index].CallOnChanged(_i);
            }
            else
                rect.gameObject.SetActive(false);
        }
        else
        {
            if (!rect.gameObject.activeSelf)
                rect.gameObject.SetActive(true);

            //Сообщаем элементу об изменении индекса 
            components[index].CallOnChanged(_indexNew);
        }
    }

    private void OnScrollChange(Vector2 _vector)
    {
        if (!isVerified)
            return;
        if (scrollDirection == ScrollerDirection.Vertical)
        {
            indexFirst = (int)(content.anchoredPosition.y / (height + spacing));
            if (content.anchoredPosition.y < 0)
                indexFirst -= 1;
        }
        else if (scrollDirection == ScrollerDirection.Horizontal)
        {
            indexFirst = (int)(-content.anchoredPosition.x / (width + spacing));
            if (content.anchoredPosition.x > 0)
                indexFirst -= 1;
        }
       
        //Если индекс верхнего видимог элемента не изменился - возвращаемся
        if (indexFirstOld == indexFirst)
            return;
        indexLast = indexFirst + items.Count - 2;

        var delta = indexFirstOld - indexFirst;
        var deltaAbs = Mathf.Abs(delta);

        //Перемещение всех карточек (т.к. прокрутили видимую область)
        if (deltaAbs >= items.Count - 2)
        {
            UpdateAllItemsPosition(indexFirst);
        }
        //Прокрутка к верхним элементам списка
        else if (delta > 0)
        {
            for (int i = -1; i < deltaAbs; i++)
            {
                int _indexOld = indexLastOld - i;
                int _indexNew = _indexOld - items.Count;
                UpdateItemPosition(_indexOld, _indexNew);
            }
        }
        //Прокрутка к нижним элементам списка
        else if (delta < 0)
        {
            for (int i = -1; i < deltaAbs; i++)
            {
                int _indexOld = indexFirstOld + i;
                int _indexNew = _indexOld + items.Count;
                UpdateItemPosition(_indexOld, _indexNew);
            }
        }

        //Сохраняем значения индексов
        indexFirstOld = indexFirst;
        indexLastOld = indexLast;
    }

    private void ScrollToPosition(float _normalizePosition)
    {
        if (scroller.vertical)
        {
            var y = 0f;
            if (scroller.content.pivot.y == 1)
                y = scroller.content.sizeDelta.y * _normalizePosition;
            if (scroller.content.pivot.y == 0)
                y = -scroller.content.sizeDelta.y * _normalizePosition;
            scroller.content.anchoredPosition = new Vector2(scroller.content.anchoredPosition.x, y);
        }
        else if (scroller.horizontal)
        {
            var x = -scroller.content.sizeDelta.x * _normalizePosition;
            if (scroller.content.pivot.x == 1)
                x += scroller.content.sizeDelta.x;
            scroller.content.anchoredPosition = new Vector2(x, scroller.content.anchoredPosition.y);
        }
    }

    private void ScrollToElement(int _elementIndex, int _totalCount)
    {
        float normalizePosition = 0f;        
        if (scroller.vertical)
        {
            normalizePosition = ((float)_elementIndex) / (float)_totalCount;
        }
        else if (scroller.horizontal)
        {
            normalizePosition = ((float)_elementIndex) / (float)_totalCount;
        }

        ScrollToPosition(normalizePosition);
    }
    #endregion

    #region Coroutines
    private IEnumerator InitializingCoroutine(int _indexFirst)
    {
        //Определяем размеры элемента списка
        var instanceRect = itemPrefab.GetComponent<RectTransform>();
        height = (int)instanceRect.rect.height;
        width = (int)instanceRect.rect.width;

        //Создаем видимые элементы списка + 2 (дополнительно сверху и снизу)
        int countViews = 0;
        if (scrollDirection == ScrollerDirection.Vertical)
            countViews = Mathf.CeilToInt(scroller.viewport.rect.height / (height + spacing));
        else if (scrollDirection == ScrollerDirection.Horizontal)
            countViews = Mathf.CeilToInt(scroller.viewport.rect.width / (width + spacing));

        //Устанавливаем размер области Content
        if (scrollDirection == ScrollerDirection.Vertical)
            content.sizeDelta = new Vector2(content.sizeDelta.x, countVirtualItems * (height + spacing) - spacing);
        else if (scrollDirection == ScrollerDirection.Horizontal)
            content.sizeDelta = new Vector2(countVirtualItems * (width + spacing) - spacing, content.sizeDelta.y);

        for (int i = -1; i <= countViews; i++)
        {
            CreateOneItem(i);
            yield return null;
        }        

        //Скроллим к нужному элементу
        ScrollToElement(_indexFirst, countVirtualItems);

        //Первичный просчет индексов видимых элементов
        OnScrollChange(Vector2.zero);

        IsInit = true;
    }
    #endregion
}