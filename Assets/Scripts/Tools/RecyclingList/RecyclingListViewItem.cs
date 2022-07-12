using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RecyclingListViewItem : MonoBehaviour
{
    private RecyclingListView parentList;
    public RecyclingListView ParentList
    {
        get => parentList;
    }
    
    private int currentRow;
    /// <summary>
    /// 当前是第几行
    /// </summary>
    public int CurrentRow
    {
        get => currentRow;
    }
    
    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    
    public virtual void NotifyCurrentAssignment(RecyclingListView v, int row)
    {
        parentList = v;
        currentRow = row;
    }
}
