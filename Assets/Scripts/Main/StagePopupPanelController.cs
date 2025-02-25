using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PopupPanelController))]
public class StagePopupPanelController : MonoBehaviour
{
    [SerializeField] private GameObject stageCellPrefab;
    [SerializeField] private Transform contentTransform;
    
    private void Start()
    {
        GetComponent<PopupPanelController>().SetTitleText("STAGE");

        var lastStageIndex = 90;             // UserInformations.LastStageIndex;
        var maxStageCount = 100;            // Constants.MAX_STAGE_COUNT;
        
        // Stage Cell 만들기
        for (int i = 0; i < maxStageCount; i++)
        {
            GameObject stageCellObject = Instantiate(stageCellPrefab, contentTransform);
            StageCellButton stageCellButton = stageCellObject.GetComponent<StageCellButton>();

            if (i < lastStageIndex)
            {
                stageCellButton.SetStageCell(i, StageCellButton.StageCellType.Clear);                
            }
            else if (i == lastStageIndex)
            {
                stageCellButton.SetStageCell(i, StageCellButton.StageCellType.Normal);
            }
            else
            {
                stageCellButton.SetStageCell(i, StageCellButton.StageCellType.Lock);
            }
        }
        
    }
}
