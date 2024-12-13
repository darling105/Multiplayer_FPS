using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;
    public PlayerMovementController localPlayer;

    [Header("Panels")]
    [SerializeField] GameObject UI_Alive;
    [SerializeField] GameObject UI_Death;
    [Header("Death Screen")]
    public TextMeshProUGUI deathCount;
    public TextMeshProUGUI killCount;
    [Header("HUD")]
    public TextMeshProUGUI AmmoCountText;
    public TextMeshProUGUI HPText;
    public Transform HPBar;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            //There's already existing canvas manager.
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);


        HideUI();

    }
    public void ChangePlayerState(bool isAlive)
    {
        UI_Alive.SetActive(isAlive);
        UI_Death.SetActive(!isAlive);

        if (!isAlive)
        {
            deathCount.text = "Deaths: " + localPlayer.deaths;
            killCount.text = "Kills: " + localPlayer.kills;
        }
    }
    public void HideUI()
    {
        UI_Alive.SetActive(false);
        UI_Death.SetActive(false);
    }
    public void UpdateHP(int currentHP, int maxHP)
    {
        float currentHPPercent = (float)currentHP / (float)maxHP;
        HPBar.localScale = new Vector3(currentHPPercent, 1, 1);
        HPText.text = currentHP.ToString() + "/" + maxHP.ToString();
    }
    // public void RespawnButton()
    // {
    //     if (localPlayer != null)
    //     {
    //         localPlayer.CmdRespawn();
    //     }
    // }
}
