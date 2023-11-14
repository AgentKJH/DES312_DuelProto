using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    // data
    public Dictionary<string, object> DuelOverData = new Dictionary<string, object>();
    bool d_PMtrackData = false;
    public float d_totalDuelTime = 0;
    int d_numberOfClashes = 0;


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
                foreach (HeroController controller in HeroControllers) { controller.m_canMove = false; } // enable movement for characters
                Invoke("StartGame", 0.5f); // start game with a short delay
            } 
            return HeroControllers.Count;
        }
        else 
        {
            Debug.LogError("ERROR - to many players joined - PlayerManager");
            return 0;
        }

    }

    /// <summary>
    /// Start the duel and start tracking data;
    /// </summary>
    public void StartGame()
    {
        HeroControllers[0].transform.position = player1StartPositon.transform.position;
        HeroControllers[1].transform.position = player2StartPositon.transform.position;
        d_PMtrackData = true;
        foreach (HeroController controller in HeroControllers) { controller.m_canMove = true; controller.d_trackData = true;  } // enable movement and start tracking data on each character
        bannerText.text = "Fight!"; //update banner
    }

    /// <summary>
    /// Called when a character dies, displays victory text and sends game data
    /// </summary>
    /// <param name="deadPlayerNumber"></param>
    public void DuelOver(int deadPlayerNumber)
    {
        if (deadPlayerNumber == 1)
        {
            bannerText.text = "Player 2 Wins";
        } else if (deadPlayerNumber == 2)
        {
            bannerText.text = "Player 1 Wins";
        }
        // data
        d_PMtrackData = false;
        foreach (HeroController controller in HeroControllers) { controller.StoreData(); } // store collected data in HeroControllers

        DuelOverData.Add("PlayerDefeated", deadPlayerNumber);
        DuelOverData.Add("TotalDuelTime", d_totalDuelTime);
        DuelOverData.Add("NumberOfClashes", d_numberOfClashes);

        GameManager.Instance.SendGameData("DuelOverData", DuelOverData);
    }

    private int clashPlayerNumber = 0;
    public void CanClash(int playerNumber)
    {
        if (m_doClash && clashPlayerNumber != playerNumber)
        {
            foreach (HeroController controller in HeroControllers)
            {
                if (d_PMtrackData) { d_numberOfClashes++; }
                controller.ClashReceive();
            }
        } else
        {
            m_clashWindowTimer = 0;
            clashPlayerNumber = playerNumber;
            m_doClash = true;
        }

    }

    public void BlockClash(int playerNumber)
    {
        if (playerNumber == 1)
        {
            HeroControllers[1].ClashReceive();
        } else if (playerNumber == 2)
        {
            HeroControllers[0].ClashReceive();
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

        if (d_PMtrackData) { d_totalDuelTime += Time.deltaTime; } // track time dueling 

    }
}
