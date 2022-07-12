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
    [Tooltip("事先预留的最小列表高度")] public float PreAllocHeight = 0;

    protected ScrollRect scrollRect;
    protected float previousBuildHeight = 0;
    /// <summary>
    /// 复用的item数组
    /// </summary>
    protected RecyclingListViewItem[] childItems;
    
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

    private float ViewportHeight()
    {
        return scrollRect.viewport.rect.height;
    }
}