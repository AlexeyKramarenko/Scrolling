using UnityEngine;

class CameraScript : MonoBehaviour
{

    private CanvasScript canvScript;
    private Transform canvParent;

    void Start()
    {
        canvScript = GameObject.FindGameObjectWithTag("Parent").GetComponent<CanvasScript>();
        canvParent = GameObject.FindGameObjectWithTag("Parent").transform;
    }

    void Update()
    {
        AdaptScreenSize();
    }
    
    #region Methods
    void AdaptScreenSize()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            ResizeToPortrait();
        }
        else if (Screen.orientation == ScreenOrientation.Landscape)
        {
            ResizeToLandscape();
        }
    }
    void ResizeToPortrait()
    {
        transform.position = new Vector3(Screen.width / 2, -350, -100);
        Camera.main.fieldOfView = 148;
        canvParent.localScale = new Vector3(1f, 1f, 1);
        canvScript.SetupYBorders();
    }
    void ResizeToLandscape()
    {
        transform.position = new Vector3(Screen.width / 4 + 25, -130, -100);
        Camera.main.fieldOfView = 105;
        canvParent.localScale = new Vector3(1f, 0.65f, 1);
        canvScript.SetupYBorders();
    }
    #endregion 
}
