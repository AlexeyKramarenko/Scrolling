using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Linq;

class ColumnsInputScript : MonoBehaviour, IPointerClickHandler
{
    #region Variables
    int columnsCount = 0;

    public GameObject RowPanel;
    public GameObject button;

    Animator anim;
    GameObject errorText;
    GameObject imgInputRows;
    GameObject inputText;
    GameObject txtRowName;
    GameObject imgInputColumns;
    GameObject inputPanel;
    GameObject verticalLayout;

    #endregion 

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        GameObject canvas = GameObject.FindGameObjectWithTag("Canvas");
        anim = canvas.GetComponent<Animator>();
        errorText = GameObject.FindGameObjectWithTag("TextError");
        imgInputRows = GameObject.FindGameObjectWithTag("ImgInputRows");
        imgInputColumns = GameObject.FindGameObjectWithTag("ImgInputColumns");
        inputText = GameObject.FindGameObjectWithTag("InputText");
        txtRowName = GameObject.FindGameObjectWithTag("TextRowName");
        inputPanel = GameObject.Find("InputPanel");
        verticalLayout = GameObject.Find("VerticalLayout");
        
        imgInputColumns.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, imgInputColumns.GetComponent<RectTransform>().sizeDelta.y);

        imgInputRows.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, imgInputColumns.GetComponent<RectTransform>().sizeDelta.y);
        imgInputRows.transform.position = new Vector3(Screen.width, imgInputRows.transform.position.y, imgInputRows.transform.position.z);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ProcessUsersInput();
    }

    #region Methods
    private void ProcessUsersInput()
    {
        string input = inputText.transform.GetComponent<Text>().text;

        if (Int32.TryParse(input, out columnsCount))
        {
            errorText.GetComponent<Text>().text = "";

            ColumnsConfiguration.COLUMNS = columnsCount;

            //переход на следующий экран и создание полей и кнопки "OK" на лету
            PlayTransitionAnimation();
            CreateInputRows();
        }

        else
        {
            errorText.GetComponent<Text>().text = "* только цифры";
        }
    }
    private void PlayTransitionAnimation()
    {
        anim.SetTrigger("Transition");
    }
    private void CreateInputRows()
    {        
        float Y = verticalLayout.transform.position.y;
        for (int i = 0; i < columnsCount; i++)
        {
            GameObject go = (GameObject)Instantiate(RowPanel,
                new Vector3(verticalLayout.transform.position.x+100, Y - i * 90, verticalLayout.transform.position.z),
                Quaternion.identity);

            go.transform.SetParent(verticalLayout.transform);

            go.name = "RowPanel" + (i + 1).ToString();
            go.transform.FindChild("TextRowName").GetComponent<Text>().text = string.Format("{0}-я колонка:", i + 1);
        }
    }

    #endregion 
}