using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] PlayerInputManager InputManager;

    public static PlayerManager S_PlayerManager;

    private void Awake()
    {
        S_PlayerManager = this; // Set up singleton
    }
    

}
