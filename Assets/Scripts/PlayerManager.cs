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
    public static PlayerManager Instance;

    private float m_clashWindowTimer;
    private float m_clashWindowDuration = 0.5f;
    public bool m_doClash = false;

    private void Awake()
    {
        Instance = this; // Set up singleton
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
                foreach (HeroController controller in HeroControllers) { controller.m_canMove = true; } // enable movement for characters
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

    public void CanClash()
    {
        if (m_doClash)
        {
            foreach (HeroController controller in HeroControllers)
            {
                controller.ClashReceive();
            }
        } else
        {
            m_clashWindowTimer = 0;
            m_doClash = true;
        }

    }

    private void Update()
    {
        if (m_doClash)
        {
            m_clashWindowTimer += Time.deltaTime;
            if (m_clashWindowTimer > m_clashWindowDuration)
            {
                m_doClash = false;
                m_clashWindowTimer = 0;
            }
        }
    }
}
