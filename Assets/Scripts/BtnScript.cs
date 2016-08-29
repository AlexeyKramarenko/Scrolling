using UnityEngine;
using UnityEngine.EventSystems;


class BtnScript : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Application.Quit();
    }
}
