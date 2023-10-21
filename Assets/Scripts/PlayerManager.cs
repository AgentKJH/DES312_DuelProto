using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] PlayerInputManager InputManager;
    [SerializeField] TextMeshProUGUI bannerText;

    [SerializeField] Transform player1StartPositon;
    [SerializeField] Transform player2StartPositon;

    public List<HeroController> HeroControllers;
    public static PlayerManager S_PlayerManager;

    private void Awake()
    {
        S_PlayerManager = this; // Set up singleton
    }

    public int PlayerJoined(HeroController playerCharacterRef)
    {
        if (HeroControllers.Count < 2)
        {
            List<HeroController> list = HeroControllers; list.Add(playerCharacterRef);
            if (HeroControllers.Count == 1) 
            {
                playerCharacterRef.transform.position = player1StartPositon.transform.position; 
            }
            if (HeroControllers.Count == 2) 
            {
                playerCharacterRef.transform.position = player2StartPositon.transform.position;
                bannerText.text = "Fight!"; //update banner
                foreach (HeroController controller in HeroControllers) { controller.canMove = true; } // enable movement for characters
            } 
            return HeroControllers.Count;
        }
        else 
        {
            print("ERROR - to many players joined - PlayerManager");
            return 0;
        }

    }

    public void GameOver(int deadPlayerNumber)
    {
        if (deadPlayerNumber == 1)
        {
            bannerText.text = "Player 2 Wins";
        } else if (deadPlayerNumber == 2)
        {
            bannerText.text = "Player 1 Wins";
        }
    }
}
