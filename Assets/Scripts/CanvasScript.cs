using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

class CanvasScript : MonoBehaviour
{
    #region Variables

    public GameObject imageLine = null;
    public float x_speed = 0.45f;
    public float y_speed = 0.25f;
    public float verticalStep = 1f;

    private float horizontalStepsOnOneClick = 1f;

    //Для удобства самый левый угол списка контактов должен быть точно посередине канваса в нулевой точке.
    //Для этого смещаем первый елемент Image наполовину(от ширины и высоты) вправо и вниз.
    //Так верхний левый угол списка всегда будет привязан к нулевым координатам Canvas'а.
    public static float halfImageWidth = 0;
    private float halfImageHeight = 0;
    private float imageWidth = 0;
    private float imageHeight = 0;
    private float sidestep = 0;

    private float targetAfterReleaseTouchX = 0;
    private float targetAfterReleaseTouchY = 0;


    //сокращения имен для осей transform.position
    private float x;
    private float y;
    private float z;

    private float[] points = null;//точки фиксации по горизонтальной оси

    private float screenHight = 0;

    private float topBorderY = 0;
    private float bottomBorderY;// = 0;
    private Transform ParentOfCanvases = null;

    private GameObject[] canvases = null;

    public GameObject CANVAS = null;
    public int COLUMNS = 3;
    public int[] rowsInColumns = null;

    private Dictionary<GameObject, int> rowsInColumnDictionary;
    private GameObject selectedCanvas = null;

    private float minSwipeDistY = 0;
    private float minSwipeDistX = 0;
    private Vector2 startPos = Vector3.zero;
    private bool verticalMov = false;

    #endregion

    private void Start()
    {
        //Считывание данных, введенных в сцене "Requirements"
        if (ColumnsConfiguration.ROWS != null && ColumnsConfiguration.ROWS.Length > 0)
            rowsInColumns = ColumnsConfiguration.ROWS;

        //Если приложение запускать со сцены "First", а не "Requirements",
        //можно вводить тестовые данные в инспекторе для объекта "Parent".
        //Если в инспекторе не установлены пользовательские значения строк в колонках:
        else if (rowsInColumns.Length == 0)
            AddDefaultRowCountToColumns(200);

        GetImageDimensions();
        CreateCanvasObjects();

        BaseTargetPointInit();

        InitColumnRowsDependancy();
        SetupYBorders();
        CreateXBoundaryPoints(Direction.ToLeft);
        GenerateContactLists(canvases.Length);

        ChooseCanvas();
    }


    private void Update()
    {
        GetTransformAxis();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startPos = touch.position;
                    break;

                case TouchPhase.Ended:
                    float swipeDistVertical = (new Vector3(0, touch.position.y, 0) - new Vector3(0, startPos.y, 0)).sqrMagnitude;
                    float swipeDistHorizontal = (new Vector3(touch.position.x, 0, 0) - new Vector3(startPos.x, 0, 0)).sqrMagnitude;


                    if (swipeDistHorizontal > swipeDistVertical)
                    {
                        verticalMov = false;
                    }
                    else
                    {
                        verticalMov = true;
                    }

                    //left-right
                    if (verticalMov == false)
                    {
                        ChooseCanvas();

                        float swipeValue = Mathf.Sign(touch.position.x - startPos.x);

                        if (swipeDistHorizontal > minSwipeDistX && swipeValue > 0 && EnableMovingRight())//right
                        {
                            Vector3 additionalPath = CalculateLerpPath(Vector3.right);
                            CalculateTargets(additionalPath, Direction.ToRight);
                        }
                        else if (swipeDistHorizontal > minSwipeDistX && swipeValue < 0 && EnableMovingLeft())//left
                        {
                            Vector3 additionalPath = CalculateLerpPath(Vector3.left);
                            CalculateTargets(additionalPath, Direction.ToLeft);
                        }
                    }

                    //scrolling up-down
                    else if (swipeDistVertical > minSwipeDistY && verticalMov && CheckBoundariesX())
                    {
                        float swipeValue = Mathf.Sign(touch.position.y - startPos.y);

                        if (swipeValue > 0)//up 
                        {
                            XValueCorrection();
                            if (ReachedAnyDestinationPointX())
                            {
                                Vector3 additionalPath = CalculateLerpPath(Vector3.up);
                                targetAfterReleaseTouchY = selectedCanvas.transform.position.y + additionalPath.y;
                            }
                        }
                        else if (swipeValue < 0)//down 
                        {
                            XValueCorrection();
                            if (ReachedAnyDestinationPointX())
                            {
                                Vector3 additionalPath = CalculateLerpPath(Vector3.down);
                                targetAfterReleaseTouchY = selectedCanvas.transform.position.y + additionalPath.y;
                            }
                        }
                    }
                    break;
            }
        }
    #region Боковое перемещение
        if (TargetOfXMovementIsNotAchievedYet())//Пока вектор transform.position не сравнялся с целевым вектором targetAfterReleaseTouch 
        {
            QualifyXCoords();
            SmoothTransitionX();
            SetupYBorders();
        }
        else
        {
            XValueCorrection();
        }
    #endregion
    #region Вертикальное перемещение

        QualifyXCoords();

        EnableYMovement();
        CheckAndArrangeDistanceYToCorrectEnding();
    
        SmoothTransitionY();

    #endregion
    }


    #region Methods
    private void GetTransformAxis()
    {
        x = transform.position.x;
        z = transform.position.z;
        ParentOfCanvases = GameObject.FindGameObjectWithTag("Parent").transform;
    }
    private void CalculateTargets(Vector3 additionalPath, Direction dir)
    {
        int lastCanvasSelectedIndex = canvases.ToList().IndexOf(selectedCanvas);

        if (dir == Direction.ToLeft)
            selectedCanvas = canvases[++lastCanvasSelectedIndex];

        else if (dir == Direction.ToRight)
            selectedCanvas = canvases[--lastCanvasSelectedIndex];

        targetAfterReleaseTouchX = selectedCanvas.transform.position.x + additionalPath.x;

        //при перемещении в сторону мы должны исключить преждвременное прибавление по оси Y, 
        //полученное от предыдущего листания по вертикали
        targetAfterReleaseTouchY = selectedCanvas.transform.position.y - additionalPath.y;
    }
    private Vector3 CalculateLerpPath(Vector3 currentDirection)
    {
        float speed = 0;

        if (currentDirection == Vector3.left || currentDirection == Vector3.right)
        {
            speed = horizontalStepsOnOneClick;
        }
        else if (currentDirection == Vector3.up || currentDirection == Vector3.down)
        {
            speed = verticalStep;
        }
        return currentDirection * sidestep * speed;
    }
    private bool TargetOfXMovementIsNotAchievedYet()
    {
        return targetAfterReleaseTouchX != Mathf.Round(x);
    }
    private bool CheckBoundariesX()
    {
        foreach (float point in points)
        {
            if (Mathf.RoundToInt(x) == point)
            {
                return true;
            }
        }
        return false;
    }
    private bool ReachedAnyDestinationPointX()
    {
        foreach (float point in points)
        {
            if (x == point)
            {
                return true;
            }
        }
        return false;
    }
    void InitColumnRowsDependancy()
    {
        rowsInColumnDictionary = new Dictionary<GameObject, int>();
        for (int i = 0; i < canvases.Length; i++)
        {
            rowsInColumnDictionary.Add(canvases[i], rowsInColumns[i]);
        }
    }
    void CreateCanvasObjects()
    {
        if (ColumnsConfiguration.COLUMNS > 0)
            canvases = new GameObject[ColumnsConfiguration.COLUMNS];
        else
            canvases = new GameObject[this.COLUMNS];

        for (int i = 0; i < canvases.Length; i++)
        {
            GameObject canvas_ = (GameObject)Instantiate(CANVAS, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
            canvas_.name = "Canvas" + i;
            canvases[i] = canvas_;
            canvas_.transform.SetParent(this.transform);
        }
    }
    void AddDefaultRowCountToColumns(int count)
    {
        rowsInColumns = new int[COLUMNS];

        for (int i = 0; i < rowsInColumns.Length; i++)
        {
            rowsInColumns[i] = count;
        }
    }
    private void XValueCorrection()
    {
        transform.position = new Vector3(Mathf.RoundToInt(x), y, z);
    }
    //переопределяем значение по оси Х до нужного граничного (point_...), если значение Х
    //находится в поле допуска (-5...+5) т.е. вблизи от граничного     (Lerp() может давать погрешность)
    private bool QualifyXCoords()
    {
        foreach (float point in points)
        {
            if (IsValueInXDiapason(x, point))
            {
                transform.position = new Vector3(point, y, z);
                return true;
            }
        }
        return false;
    }
    //Близко ли текущее значение Х к целевому (+-5).
    //Если да, то сработает триггер QualifyXCoords
    private bool IsValueInXDiapason(float val, float point)
    {
        return (val != point && (point - 5) < val && val < (point + 5));
    }
    private bool EnableMovingLeft()
    {
        return ParentOfCanvases.position.x >= (points[points.Length - 2]);//points[points.Length - 2] - предпоследняя позиция, можно двигатся влево как минимум в последнюю позицию points[points.Length - 1]
    }
    private bool EnableMovingRight()
    {
        return ParentOfCanvases.position.x <= (points[1]); //points[1] - 2-я позиция, можно двигаться вправо как минимум в позицию points[0]
    }
    private void CreateXBoundaryPoints(Direction dir)
    {
        points = new float[canvases.Length];

        if (dir == Direction.ToRight)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = canvases[0].transform.position.x + i * imageWidth;
            }
        }
        else if (dir == Direction.ToLeft)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = -1 * (canvases[0].transform.position.x + i * imageWidth);
            }
        }
    }
    private void ChooseCanvas()
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (x == points[i])
            {
                selectedCanvas = canvases[i];
                return;
            }
        }
    }
    private bool EnableYMovement()
    {
        if (IsValueInXDiapason(y, bottomBorderY))
        {
            transform.position = new Vector3(x, bottomBorderY, z);
            return false; //запрет на перемещение вниз и на дальнейшие выполнение Lerp()  
        }
        return true;
    }
    public void SetupYBorders()
    {  
        topBorderY = 0;
        //при старте программы первая колонка должна бы предопределена
        int rowsCount = rowsInColumnDictionary[canvases[0]];

        //далее при каждом переходе будет происходить поиск в словаре rowsInColumnDict
        for (int i = 0; i < canvases.Length; i++)
        {
            if (selectedCanvas == canvases[i])
            {
                rowsCount = rowsInColumnDictionary[canvases[i]];

                if (rowsCount * imageHeight >= screenHight)
                    bottomBorderY = imageHeight * rowsCount - screenHight;
                else
                    // запрет на скроллинг, так как в колонке строки занимают
                    // меньше пространства, для которого он нужен(скроллинг)
                    bottomBorderY = 0;
            }
        }
    }
    private void BaseTargetPointInit()
    {
        targetAfterReleaseTouchX = x;
        targetAfterReleaseTouchY = y;
    }
    private void GetImageDimensions()
    {
        //подгонка ширины строчки списка под размеры экрана
        //imageWidth = imageLine.GetComponent<RectTransform>().rect.width;
        imageWidth = Screen.width;
        sidestep = imageWidth;

        imageHeight = imageLine.GetComponent<RectTransform>().rect.height;//90
         
        screenHight = imageHeight * 7f;

        halfImageWidth = imageWidth / 2;
        halfImageHeight = imageHeight / 2;
    }
    private void CreateContactListColumn(int rows, float pointX, Transform _transform, Color backgroundColor)
    {
        for (int i = 0; i < rows; i++)
        {
            GameObject go = (GameObject)Instantiate(imageLine, _transform.position, Quaternion.identity);
            go.transform.SetParent(_transform);

            float Y = go.GetComponent<RectTransform>().sizeDelta.y;
            float X = Screen.width;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(X, Y);


            go.transform.localPosition = new Vector3(Mathf.Abs(pointX) + halfImageWidth, i * (-imageHeight) - halfImageHeight, 0);
            go.name = string.Format("line_{0}", i);
            go.GetComponentInChildren<Text>().text = Database.names[i];

            if (i % 2 == 0)
            {
                go.GetComponent<CanvasRenderer>().SetColor(backgroundColor);
            }
        }
    }
    private void GenerateContactLists(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Color color = ColorDatabase.colors[UnityEngine.Random.Range(0, ColorDatabase.colors.Length - 1)];
            CreateContactListColumn(rowsInColumns[i], points[i], canvases[i].transform, color);
        }
    }
    private void SmoothTransitionX()
    {
        transform.position = Vector3.Lerp(
            transform.position,
          new Vector3(targetAfterReleaseTouchX, transform.position.y, transform.position.z),
            x_speed);
    }
    private void SmoothTransitionY()
    {
        selectedCanvas.transform.position = Vector3.Lerp(
            selectedCanvas.transform.position,
           new Vector3(selectedCanvas.transform.position.x, targetAfterReleaseTouchY, selectedCanvas.transform.position.z),
           y_speed);
    }

    //targetAfterReleaseTouch - точка достижения для Lerp()
    //Этот метод гарантирует, что targetAfterReleaseTouch не выйдет за пределы
    //списка контактов, т.е. скроллинг не поднимет обзор выше и не опустит ниже допустимых границ,
    //зависящих от количество строк в списке контактов
    private void CheckAndArrangeDistanceYToCorrectEnding()
    {
        if (targetAfterReleaseTouchY < topBorderY - 10)
            targetAfterReleaseTouchY = topBorderY;

        else if (targetAfterReleaseTouchY > bottomBorderY - 10)
            targetAfterReleaseTouchY = bottomBorderY;
    }

    #endregion
}



//#if UNITY_STANDALONE_WIN

//    //for Windows
//    private float scrolling;
//    private void Update()
//    {
//        GetTransformAxis();

//        scrolling = Input.GetAxis("Mouse ScrollWheel");

//        if (scrolling == 0)
//        {
//            ChooseCanvas();

//            if (Input.GetKeyUp(KeyCode.RightArrow) && EnableMovingRight())
//            {
//                Vector3 additionalPath = CalculateLerpPath(Vector3.right);
//                CalculateTargets(additionalPath, Direction.ToRight);

//            }
//            else if (Input.GetKeyUp(KeyCode.LeftArrow) && EnableMovingLeft())
//            {
//                Vector3 additionalPath = CalculateLerpPath(Vector3.left);
//                CalculateTargets(additionalPath, Direction.ToLeft);
//            }
//        }

//        else if (scrolling > 0 && CheckBoundariesX())
//        {
//            XValueCorrection();

//            if (ReachedAnyDestinationPointX())
//            {
//                Vector3 additionalPath = CalculateLerpPath(Vector3.up);
//                targetAfterReleaseTouchY = selectedCanvas.transform.position.y + additionalPath.y;
//            }
//        }
//        else if (scrolling < 0 && CheckBoundariesX())
//        {
//            XValueCorrection();

//            if (ReachedAnyDestinationPointX())
//            {
//                Vector3 additionalPath = CalculateLerpPath(Vector3.down);
//                targetAfterReleaseTouchY = selectedCanvas.transform.position.y + additionalPath.y;
//            }
//        }
//    #region Боковое перемещение
//        if (TargetOfXMovementIsNotAchievedYet())//Пока вектор transform.position не сравнялся с целевым вектором targetAfterReleaseTouch 
//        {
//            QualifyXCoords();
//            SmoothTransitionX();
//            SetupYBorders();
//        }
//        else
//        {
//            XValueCorrection();
//        }
//    #endregion
//    #region Вертикальное перемещение

//        QualifyXCoords();
//        EnableYMovement();
//        CheckAndArrangeDistanceYToCorrectEnding();

//        SmoothTransitionY();
//    #endregion
//    }
//#endif


