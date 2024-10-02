using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject GameOverPanel;
    public GameObject GameOverText;
    public GameObject RestartText;

    private bool IsGameOver = false;

    // Start is called before the first frame update
    void Start()
    {
        GameOverPanel.SetActive(false);
        GameOverText.gameObject.SetActive(false);
        RestartText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.Find("Frog").GetComponent<Frog>().GetHealth() <= 0 && !IsGameOver)
        {
            IsGameOver = true;
            GameOverText.GetComponent<TMPro.TextMeshProUGUI>().text = "You Died!";
            GameOverSequence();
        }

        if (GameObject.Find("Frog").GetComponent<Frog>().GetFliesCaught() >= 10 && !IsGameOver)
        {
            IsGameOver = true;
            GameOverText.GetComponent<TMPro.TextMeshProUGUI>().text = "You Won!";
            GameOverSequence();
        }

        if (IsGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    // couldn't figure out how coroutines/IEnumerators work from the example
    // this is much easier
    void GameOverSequence()
    {
        Time.timeScale = 0.0f;

        GameOverPanel.SetActive(true);
        GameOverText.gameObject.SetActive(true);
        RestartText.gameObject.SetActive(true);
    }
}
