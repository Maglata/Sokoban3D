using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject winMenu;

    public void winGame()
    {
        winMenu.SetActive(true);
        Time.timeScale = 0f;
    }
    public void resumeGame()
    {
        winMenu.SetActive(false);
        Time.timeScale = 1f;
    }

}
