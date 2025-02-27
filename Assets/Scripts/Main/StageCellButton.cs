using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StageCellButton : MonoBehaviour
{
    [SerializeField] private GameObject normalImageObject;
    [SerializeField] private GameObject lockImageObject;
    [SerializeField] private GameObject clearImageObject;
    [SerializeField] private TMP_Text[] stageIndexText;

    private RectTransform _rectTransform;
    public RectTransform RectTransform => _rectTransform;

    public enum StageCellType { Normal, Lock, Clear }
    private StageCellType _stageCellType;
    private int _stageIndex;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetStageCell(int stageIndex, StageCellType stageCellType)
    {
        _stageIndex = stageIndex;
        _stageCellType = stageCellType;

        // Stage Index 텍스트에 출력
        foreach (var stageIndexText in stageIndexText)
        {
            var indexText = _stageIndex + 1;
            stageIndexText.text = indexText.ToString();
        }

        // 클리어 상태에 따라 Cell 이미지 변경
        switch (_stageCellType)
        {
            case StageCellType.Normal:
                normalImageObject.SetActive(true);
                clearImageObject.SetActive(false);
                lockImageObject.SetActive(false);
                break;
            case StageCellType.Clear:
                normalImageObject.SetActive(false);
                clearImageObject.SetActive(true);
                lockImageObject.SetActive(false);
                break;
            case StageCellType.Lock:
                normalImageObject.SetActive(false);
                clearImageObject.SetActive(false);
                lockImageObject.SetActive(true);
                break;
        }
    }

    public void OnClickStageCellButton()
    {
        if (_stageCellType != StageCellType.Clear) return;
        
        // TODO: _stageIndex에 해당하는 게임 시작
    }
}
