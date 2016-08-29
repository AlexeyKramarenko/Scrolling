using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

class RowsInputScript : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        FillColumnsByLines();

        Application.LoadLevel(1);
    }


    private void FillColumnsByLines()
    {
        int[] ROWS = new int[ColumnsConfiguration.COLUMNS];

        GameObject[] RowPanels = GameObject.FindGameObjectsWithTag("RowPanel");

        for (int i = 0; i < RowPanels.Length; i++)
        {
            string txt = RowPanels[i].transform.FindChild("InputField").FindChild("Text").GetComponent<Text>().text;

            int count = 0;

            if (Int32.TryParse(txt, out count))
            {
                ROWS[i] = count;
            }
            else
                ROWS[i] = 30;
        }
        ColumnsConfiguration.ROWS = ROWS;
    }
}
