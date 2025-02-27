using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PopupPanelController))]
public class StagePopupPanelController : MonoBehaviour
{
    [SerializeField] private GameObject stageCellPrefab;
    [SerializeField] private Transform contentTransform;

    [SerializeField] private GameObject scrollView;
    [SerializeField] private int cellColumnCount;

    [SerializeField] private float cellWidth;
    [SerializeField] private float cellHeight;
    [SerializeField] private Vector2 spacing;

    private ScrollRect _scrollViewScrollRect;
    private RectTransform _scrollViewRectTransform;

    private List<(int index, StageCellButton[] stageCellButtons)> _visibleCells;    // 화면에 나타나는 셀 영역을 담고있는 리스트

    private float _previousScrollRectYValue = 1f;

    private int _maxStageCount = 100;

    private void Awake()
    {
        _scrollViewScrollRect = scrollView.GetComponent<ScrollRect>();
        _scrollViewRectTransform = scrollView.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 타이틀 지정
        GetComponent<PopupPanelController>().SetTitleText("STAGE");

        // 데이터 로드
        ReloadData();
    }

    private (int start, int count) GetVisibleIndexRange()
    {
        var visibleRect = new Rect(
            _scrollViewScrollRect.content.anchoredPosition.x,
            _scrollViewScrollRect.content.anchoredPosition.y,
            _scrollViewRectTransform.rect.width,
            _scrollViewRectTransform.rect.height);

        var start = Mathf.FloorToInt(visibleRect.y / (cellHeight + spacing.y));
        var visibleCount = Mathf.CeilToInt(visibleRect.height / (cellHeight + spacing.y));

        start = Mathf.Max(0, start - 1);

        // 버퍼 추가

        // Count 값 설정
        var count = Mathf.CeilToInt(_maxStageCount / cellColumnCount);
        count = Mathf.Min(count, start + visibleCount);

        return (start, count);
    }

    /// <summary>
    /// 특정 인덱스가 화면에 나와야 할 인덱스인지 확인
    /// </summary>
    /// <param name="index">인덱스</param>
    /// <returns>true: 나와야 함, false: 안 나와도 됨</returns>
    private bool IsVisibleIndex(int index)
    {
        var (start, end) = GetVisibleIndexRange();
        end = Mathf.Min(end, _maxStageCount - 1);
        return start <= index && index <= end;
    }

    private StageCellButton CreateStageCellButton(int index, int row, int col)
    {
        var stageCellButtonObject = ObjectPool.Instance.GetObject();
        var stageCellButton = stageCellButtonObject.GetComponent<StageCellButton>();
        stageCellButton.SetStageCell(index, StageCellButton.StageCellType.Normal);

        // StageCellButton 위치 지정
        float centerIndex = 0;
        if (cellColumnCount % 2 == 0)
        {
            centerIndex = cellColumnCount / 2 - 0.5f;
        }
        else
        {
            centerIndex = cellColumnCount / 2;
        }

        var offset = col - centerIndex;

        var x = cellWidth * offset + spacing.x * offset;
        var y = -(cellHeight + spacing.y) * row;
        stageCellButton.RectTransform.anchoredPosition = new Vector2(x, y);

        return stageCellButton;
    }

    private void ReloadData()
    {
        // Scroll View의 Content 사이즈 조절
        _scrollViewScrollRect.content.sizeDelta =
            new Vector2(0, Mathf.CeilToInt((float)_maxStageCount / cellColumnCount) * (cellHeight + spacing.y));

        // 화면에 보이는 셀을 담고있는 _visibleCell 리스트 초기화
        _visibleCells = new List<(int index, StageCellButton[] stageCellButtons)>();

        ////////////////////////////////////////
        // 셀 생성

        // 만들어야 하는 셀 index 찾기
        var (start, count) = GetVisibleIndexRange();

        for (int i = start; i < count; i++)             // 셀 영역 하나씩 생성
        {
            List<StageCellButton> stageCellButtons = new List<StageCellButton>();

            for (int j = 0; j < cellColumnCount; j++)   // 한 줄의 셀 영역 생성
            {
                var index = i * cellColumnCount + j;
                if (index < _maxStageCount)
                {
                    // 셀 생성
                    var stageCellButton = CreateStageCellButton(index, i, j);
                    stageCellButtons.Add(stageCellButton);
                }
            }

            _visibleCells.Add((i, stageCellButtons.ToArray()));
        }
        
    }

    /// <summary>
    /// 스크롤 뷰가 스크롤 될 때 호출되는 메서드
    /// </summary>
    /// <param name="value">스크롤 정도</param>
    public void OnValueChanged(Vector2 value)
    {
        if (_previousScrollRectYValue < value.y)
        {
            ////////////////////////////////////////
            // 위로 스크롤

            // 상단에 새로운 셀을 만들 필요가 있으면 만들기
            var firstRow = _visibleCells.First();
            var newFirstIndex = firstRow.index - 1;

            if (IsVisibleIndex(newFirstIndex))
            {
                List<StageCellButton> stageCellButtons = new List<StageCellButton>();
                for (int j = 0; j < cellColumnCount; j++)
                {
                    var index = newFirstIndex * cellColumnCount + j;
                    if (index < _maxStageCount)
                    {
                        var stageCellButton = CreateStageCellButton(index, newFirstIndex, j);
                        stageCellButtons.Add(stageCellButton);
                    }
                }
                _visibleCells.Insert(0, (newFirstIndex, stageCellButtons.ToArray()));
            }

            // 하단에 더이상 보이지 않는 셀이 있으면 제거하기
            var lastRow = _visibleCells.Last();
            if (!IsVisibleIndex(lastRow.index))
            {
                var stageCellButtons = lastRow.stageCellButtons;
                foreach (var stageCellButton in stageCellButtons)
                {
                    ObjectPool.Instance.ReturnObject(stageCellButton.gameObject);
                }
                _visibleCells.RemoveAt(_visibleCells.Count - 1);
            }
        }
        else
        {
            ////////////////////////////////////////
            // 아래로 스크롤

            // 하단에 새로운 셀을 만들 필요가 있으면 만들기
            var lastRow = _visibleCells.Last();
            var newLastIndex = lastRow.index + 1;
            if (IsVisibleIndex(newLastIndex))
            {
                List<StageCellButton> stageCellButtons = new List<StageCellButton>();
                for (int j = 0; j < cellColumnCount; j++)
                {
                    var index = newLastIndex * cellColumnCount + j;
                    if (index < _maxStageCount)
                    {
                        var stageCellButton = CreateStageCellButton(index, newLastIndex, j);
                        stageCellButtons.Add(stageCellButton);
                    }
                }
                _visibleCells.Add((newLastIndex, stageCellButtons.ToArray()));
            }

            // 상단에 더이상 보이지 않는 셀이 있으면 제거하기
            var firstRow = _visibleCells.First();
            if (!IsVisibleIndex(firstRow.index))
            {
                var stageCellButtons = firstRow.stageCellButtons;
                foreach (var stageCellButton in stageCellButtons)
                {
                    ObjectPool.Instance.ReturnObject(stageCellButton.gameObject);
                }
                _visibleCells.RemoveAt(0);
            }
        }
        _previousScrollRectYValue = value.y;
    }
}