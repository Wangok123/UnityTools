using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 循环复用列表
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class RecyclingListView : MonoBehaviour
{
    [Tooltip("子节点物体")] public RecyclingListViewItem ChildObj;
    [Tooltip("行间隔")] public float RowPadding = 15f;
    /// <summary>
    /// 这里可是设置最开始预留多少个激活与否的实体，目前用scrollview的高度就好，如果动态改变列表长度的话可能需要调这里
    /// </summary>
    [Tooltip("事先预留的最小列表高度")] public float PreAllocHeight = 0;

    protected ScrollRect scrollRect;
    protected float previousBuildHeight = 0;
    protected const int rowsAboveBelow = 1;
    /// <summary>
    /// 列表中最顶部的item的真实数据索引，比如有一百条数据，复用10个item，当前最顶部是第60条数据，那么sourceDataRowStart就是59（注意索引从0开始）
    /// </summary>
    protected int sourceDataRowStart;
    /// <summary>
    /// 循环列表中（复用的所有item实体里），第一个item的索引，最开始每个item都有一个原始索引，最顶部的item的原始索引就是childBufferStart
    /// 由于列表是循环复用的，所以往下滑动时，childBufferStart会从0开始到n，然后又从0开始，以此往复
    /// 如果是往上滑动，则是从0到-n，再从0开始，以此往复
    /// </summary>
    protected int childBufferStart = 0;
    
    /// <summary>
    /// 复用的item数组
    /// </summary>
    protected RecyclingListViewItem[] childItems;
    
    /// <summary>
    /// item更新回调函数委托
    /// </summary>
    /// <param name="item">子节点对象</param>
    /// <param name="rowIndex">行数</param>
    public delegate void ItemDelegate(RecyclingListViewItem item, int rowIndex);

    /// <summary>
    /// item更新回调函数委托
    /// </summary>
    public ItemDelegate ItemCallback;
    
    
    public enum ScrollPosType
    {
        Top,
        Center,
        Bottom,
    }

    public float VerticalNormalizedPosition
    {
        get => scrollRect.verticalNormalizedPosition;
        set => scrollRect.verticalNormalizedPosition = value;
    }

    /// <summary>
    /// 列表行数
    /// </summary>
    protected int rowCount;

    /// <summary>
    /// 列表行数，赋值时，会执行列表重新计算
    /// </summary>
    public int RowCount
    {
        get => rowCount;
        set
        {
            if (rowCount != value)
            {
                rowCount = value;
                // 先禁用滚动变化
                ignoreScrollChange = true;
                // 更新高度
                UpdateContentHeight();
                // 重新启用滚动变化
                ignoreScrollChange = false;
                // 重新计算item
                ReorganiseContent(true);
            }
        }
    }

    protected bool ignoreScrollChange = false;

    protected virtual void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        ChildObj.gameObject.SetActive(false);
    }
    
    protected virtual void OnEnable()
    {
        scrollRect.onValueChanged.AddListener(OnScrollChanged);
        ignoreScrollChange = false;
    }

    protected virtual void OnDisable()
    {
        scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }

    protected virtual void UpdateContentHeight()
    {
        // 列表高度
        float height = ChildObj.RectTransform.rect.height * rowCount + (rowCount - 1) * RowPadding;
        // 更新content的高度
        var sz = scrollRect.content.sizeDelta;
        scrollRect.content.sizeDelta = new Vector2(sz.x, height);
    }

    /// <summary>
    /// 重新计算列表内容
    /// </summary>
    /// <param name="clearContents">是否要清空列表重新计算</param>
    protected virtual void ReorganiseContent(bool clearContents)
    {
        if (clearContents)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1;
        }

        bool childrenChanged = CheckChildItems();
        // 是否要更新整个列表
        bool populateAll = childrenChanged || clearContents;


        float ymin = scrollRect.content.localPosition.y;

        // 第一个可见item的索引
        int firstVisibleIndex = (int) (ymin / RowHeight());


        int newRowStart = firstVisibleIndex - rowsAboveBelow;

        // 滚动变化量
        int diff = newRowStart - sourceDataRowStart;
        if (populateAll || Mathf.Abs(diff) >= childItems.Length)
        {
            sourceDataRowStart = newRowStart;
            childBufferStart = 0;
            int rowIdx = newRowStart;
            foreach (var item in childItems)
            {
                UpdateChild(item, rowIdx++);
            }
        }
        else if (diff != 0)
        {
            int newBufferStart = (childBufferStart + diff) % childItems.Length;

            if (diff < 0)
            {
                // 向前滑动
                for (int i = 1; i <= -diff; ++i)
                {
                    // 得到复用item的索引
                    int wrapIndex = WrapChildIndex(childBufferStart - i);
                    int rowIdx = sourceDataRowStart - i;
                    UpdateChild(childItems[wrapIndex], rowIdx);
                }
            }
            else
            {
                // 向后滑动
                int prevLastBufIdx = childBufferStart + childItems.Length - 1;
                int prevLastRowIdx = sourceDataRowStart + childItems.Length - 1;
                for (int i = 1; i <= diff; ++i)
                {   
                    int wrapIndex = WrapChildIndex(prevLastBufIdx + i);
                    int rowIdx = prevLastRowIdx + i;
                    UpdateChild(childItems[wrapIndex], rowIdx);
                }
            }

            sourceDataRowStart = newRowStart;

            childBufferStart = newBufferStart;
        }
    }

    protected virtual bool CheckChildItems()
    {
        // 列表视口高度
        float vpHeight = ViewportHeight();
        float buildHeight = Mathf.Max(vpHeight, PreAllocHeight);
        bool rebuild = childItems == null || buildHeight > previousBuildHeight;
        if (rebuild)
        {
            int childCount = Mathf.RoundToInt(0.5f + buildHeight / RowHeight());
            //上下用于多出的俩
            childCount += rowsAboveBelow * 2;

            if (childItems == null)
                childItems = new RecyclingListViewItem[childCount];
            else if (childCount > childItems.Length)
                Array.Resize(ref childItems, childCount);

            // 创建item
            for (int i = 0; i < childItems.Length; ++i)
            {
                if (childItems[i] == null)
                {
                    var item = Instantiate(ChildObj);
                    childItems[i] = item;
                }

                childItems[i].RectTransform.SetParent(scrollRect.content, false);
                childItems[i].gameObject.SetActive(false);
            }

            previousBuildHeight = buildHeight;
        }

        return rebuild;
    }

    protected virtual void UpdateChild(RecyclingListViewItem child, int rowIdx)
    {
        if (rowIdx < 0 || rowIdx >= rowCount)
        {
            child.gameObject.SetActive(false);
        }
        else
        {
            if (ItemCallback == null)
            {
                Debug.Log("RecyclingListView is missing an ItemCallback, cannot function", this);
                return;
            }

            // 移动到正确的位置
            var childRect = ChildObj.RectTransform.rect;
            Vector2 pivot = ChildObj.RectTransform.pivot;
            float ytoppos = RowHeight() * rowIdx;
            float ypos = ytoppos + (1f - pivot.y) * childRect.height;
            float xpos = 0 + pivot.x * childRect.width;
            child.RectTransform.anchoredPosition = new Vector2(xpos, -ypos);
            child.NotifyCurrentAssignment(this, rowIdx);

            // 更新数据
            ItemCallback(child, rowIdx);

            child.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 列表滚动时，会回调此函数
    /// </summary>
    /// <param name="normalisedPos">归一化的位置</param>
    protected virtual void OnScrollChanged(Vector2 normalisedPos)
    {
        if (!ignoreScrollChange)
        {
            ReorganiseContent(false);
        }
    }
    
    private float ViewportHeight()
    {
        return scrollRect.viewport.rect.height;
    }
    
    /// <summary>
    /// 获取一行的高度，注意要加上RowPadding
    /// </summary>
    private float RowHeight()
    {
        return RowPadding + ChildObj.RectTransform.rect.height;
    }

    /// <summary>
    /// 根据实际数据中的索引，得到复用的item中对应的索引
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    private int WrapChildIndex(int idx)
    {
        while (idx < 0)
            idx += childItems.Length;

        return idx % childItems.Length;
    }
}