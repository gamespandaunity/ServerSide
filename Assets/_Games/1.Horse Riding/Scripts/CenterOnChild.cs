using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
public enum ScrollDir
{
	Horizontal,
	Vertical
}
 
public class CenterOnChild : MonoBehaviour, IEndDragHandler, IDragHandler, IBeginDragHandler
{
	public ScrollDir Dir = ScrollDir.Horizontal;
 
	/// <summary>
	/// Is it centered
	/// </summary>
	private bool _isCentering = false;
 
	 [Header("Movement speed during centering")]
	public float MoveToCenterSpeed = 10f;
	 [Header("center point magnification")]
	public float CenterScale = 1f;
	 [Header("Non-center point magnification")]
	public float UnCenterScale = 0.9f;

	private ScrollRect _scrollView;
 
	private Transform _content;
    bool waitForCentring;
	private List<float> _childrenPos = new List<float>();
	public float _targetPos;
    float TimeToCenter;
    float waitTime;
	/// <summary>
	 /// Current center child index
	/// </summary>
	private int _curCenterChildIndex = -1;
 
	/// <summary>
	 /// Current Center ChildItem
	/// </summary>
	public GameObject CurCenterChildItem
	{
		get
		{
			GameObject centerChild = null;
			if (_content != null && _curCenterChildIndex >= 0 && _curCenterChildIndex < _content.childCount)
			{
				centerChild = _content.GetChild(_curCenterChildIndex).gameObject;
			}
			return centerChild;
		}
	}
	/// <summary>
	 /// Change the zoom of each sub-object according to drag
	/// </summary>
	public void SetCellScale()
	{
		GameObject centerChild = null;
		for (int i = 0; i < _content.childCount; i++)
		{
			centerChild = _content.GetChild(i).gameObject;
			if (i == _curCenterChildIndex)
			{
				//centerChild.GetComponent<Animator>().SetBool("zoom",true);
			centerChild.transform.localScale = CenterScale * Vector3.one;
			}
			else
			{
				//centerChild.GetComponent<Animator>().SetBool("zoom",false);
			centerChild.transform.localScale = UnCenterScale * Vector3.one;
			}
		}
	}
	void Awake()
	{
		_scrollView = GetComponent<ScrollRect>();
		if (_scrollView == null)
		{
			ConstantsData_M.Log("ScrollRect is null");
			return;
		}
		_content = _scrollView.content;
      
        LayoutGroup layoutGroup = null;
		layoutGroup = _content.GetComponent<LayoutGroup>();
 
		if (layoutGroup == null)
		{
			ConstantsData_M.Log("LayoutGroup component is null");
		}
		_scrollView.movementType = ScrollRect.MovementType.Unrestricted;
		float spacing = 0f;
		 //Calculate coordinates according to dir, Horizontal: store x, Vertical: store y
		switch (Dir)
		{
			case ScrollDir.Horizontal:
				if (layoutGroup is HorizontalLayoutGroup)
				{
					float childPosX = _scrollView.GetComponent<RectTransform>().rect.width * 0.5f - GetChildItemWidth(0) * 0.5f;
					spacing = (layoutGroup as HorizontalLayoutGroup).spacing;
					_childrenPos.Add(childPosX);
					for (int i = 1; i < _content.childCount; i++)
					{
						childPosX -= GetChildItemWidth(i) * 0.5f + GetChildItemWidth(i - 1) * 0.5f + spacing;
						_childrenPos.Add(childPosX);
					}
				}
				else if (layoutGroup is GridLayoutGroup)
				{
					GridLayoutGroup grid = layoutGroup as GridLayoutGroup;
					float childPosX = _scrollView.GetComponent<RectTransform>().rect.width * 0.5f - grid.cellSize.x * 0.5f;
					_childrenPos.Add(childPosX);
					for (int i = 0; i < _content.childCount - 1; i++)
					{
						childPosX -= grid.cellSize.x + grid.spacing.x;
						_childrenPos.Add(childPosX);
					}
				}
				else
				{
					ConstantsData_M.Log("Horizontal ScrollView is using VerticalLayoutGroup");
				}
				break;
			case ScrollDir.Vertical:
				if (layoutGroup is VerticalLayoutGroup)
				{
					float childPosY = -_scrollView.GetComponent<RectTransform>().rect.height * 0.5f + GetChildItemHeight(0) * 0.5f;
					spacing = (layoutGroup as VerticalLayoutGroup).spacing;
					_childrenPos.Add(childPosY);
					for (int i = 1; i < _content.childCount; i++)
					{
						childPosY += GetChildItemHeight(i) * 0.5f + GetChildItemHeight(i - 1) * 0.5f + spacing;
						_childrenPos.Add(childPosY);
					}
				}
				else if (layoutGroup is GridLayoutGroup)
				{
					GridLayoutGroup grid = layoutGroup as GridLayoutGroup;
					float childPosY = -_scrollView.GetComponent<RectTransform>().rect.height * 0.5f + grid.cellSize.y * 0.5f;
					_childrenPos.Add(childPosY);
					for (int i = 1; i < _content.childCount; i++)
					{
						childPosY += grid.cellSize.y + grid.spacing.y;
						_childrenPos.Add(childPosY);
					}
				}
				else
				{
					ConstantsData_M.Log("Vertical ScrollView is using HorizontalLayoutGroup");
				}
				break;
		}

      
    }

    void OnEnable()
    {
        SetCurrentLevel();
    }
    public void SetCurrentLevel()
    {
       //MConstants.CurrentLevelNumber = 15;
     //   //Debug.Log("Level number " + MConstants.CurrentLevelNumber);
       // float pos = 63 - 590 * (MConstants.CurrentLevelNumber - 1);
        float pos = 63 - GetChildItemWidth(0) * (PlayerDataController.instance.playerStats.CurrentSelectDubaiChampionLevel - 1);

        _scrollView.scrollSensitivity = PlayerDataController.instance.playerStats.SensivityValue * 6;
        _targetPos = FindClosestChildPos(pos, out _curCenterChildIndex);
        _isCentering = true;
        SetCellScale();
    }
	private float GetChildItemWidth(int index)
	{
		return (_content.GetChild(index) as RectTransform).sizeDelta.x;
	}
 
	private float GetChildItemHeight(int index)
	{
		return (_content.GetChild(index) as RectTransform).sizeDelta.y;
	}
	 //This is directly implemented by interpolation in the update function, and the project can be changed to dotewwn implementation
	void Update()
	{
      //  //Debug.Log("Velocity " + _scrollView.velocity.magnitude);

        if (waitForCentring)
        {
            if(_scrollView.velocity.magnitude <= 30)
            {
                _isCentering = true;
                waitForCentring = false;

            }
            _targetPos = FindClosestChildPos(_content.localPosition.x, out _curCenterChildIndex);
            SetCellScale();
        }

        if (_isCentering )
		{
          
          
            Vector3 v = _content.localPosition;
			switch (Dir)
			{
				case ScrollDir.Horizontal:
					v.x = Mathf.Lerp(_content.localPosition.x, _targetPos, MoveToCenterSpeed * Time.deltaTime);
					_content.localPosition = v;
					if (Math.Abs(_content.localPosition.x - _targetPos) < 0.01f)
					{
                       // _targetPos = FindClosestChildPos(_content.localPosition.x, out _curCenterChildIndex);
                       // SetCellScale();
                        _isCentering = false;

                    }
                    break;
				case ScrollDir.Vertical:
					v.y = Mathf.Lerp(_content.localPosition.y, _targetPos, MoveToCenterSpeed * Time.deltaTime);
					_content.localPosition = v;
					if (Math.Abs(_content.localPosition.y - _targetPos) < 0.01f)
					{
						_isCentering = false;

                    }
                    break;
			}
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		 //Here will always be called to update the subscript of the center point in real time and make zoom changes
		switch (Dir)
		{
			case ScrollDir.Horizontal:
				_targetPos = FindClosestChildPos(_content.localPosition.x, out _curCenterChildIndex);
				break;
			case ScrollDir.Vertical:
				_targetPos = FindClosestChildPos(_content.localPosition.y, out _curCenterChildIndex);
				break;
		}
        ////Debug.Log("OnDrag");
	     SetCellScale();
        TimeToCenter = Time.time;

    }
    
    public void OnEndDrag(PointerEventData eventData)
	{
        //waitTime = eventData.delta.magnitude / 50;
        //TimeToCenter = Time.time;
        ////Debug.Log("Time " + waitTime +" Delta " + eventData.delta.magnitude);

        //If you only need to refresh the center point at the end of the drag, it is better to call here. The center subscript will be refreshed once at the end of the drag
        //switch (Dir)
        //{
        //	case ScrollDir.Horizontal:
        //		_targetPos = FindClosestChildPos(_content.localPosition.x, out _curCenterChildIndex);
        //		break;
        //	case ScrollDir.Vertical:
        //		_targetPos = FindClosestChildPos(_content.localPosition.y, out _curCenterChildIndex);
        //		break;
        //}
        //_isCentering = true;
        waitForCentring = true;

    }

    public void OnBeginDrag(PointerEventData eventData)
	{
		_isCentering = false;
		_curCenterChildIndex = -1;
	}

    //public void OnValueChange(float value)
    //{
    //    //Debug.Log("Value "+ value + " fdgfg "+ _scrollView.velocity);
    //}
 
	private float FindClosestChildPos(float currentPos, out int curCenterChildIndex)
	{
		float closest = 0;
		float distance = Mathf.Infinity;
		curCenterChildIndex = -1;
		for (int i = 0; i < _childrenPos.Count; i++)
		{
			float p = _childrenPos[i];
			float d = Mathf.Abs(p - currentPos);
			if (d < distance)
			{
				distance = d;
				closest = p;
				curCenterChildIndex = i;
			}
			else
				break;
		}
		return closest;
	}

}