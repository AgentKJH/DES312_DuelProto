using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Unity.VisualScripting;
using UnityEditor;
using NUnit.Framework.Constraints;

public class HeroController : MonoBehaviour, IDamageable 
{

    [SerializeField] float      m_speed = 4.0f;
    //[SerializeField] float      m_jumpForce = 7.5f;
    //[SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;

    // Attack Sensor postion vars
    private Vector3 m_attackSensorPosRight = new Vector3(0.915000021f, 0.722000003f, 0);
    private Vector3 m_attackSensorPosLeft = new Vector3(-0.984000027f, 0.722000003f, 0);

    public int PlayerNumber;

    // ++ character stats ++
    // public
    public EplayerState playerState = EplayerState.Default;
    public float damage = 20f;
    public float maxHealth = 100f;
    public float health;
    public float maxEnergy = 100f;
    public float energy = 100f;

    private float energyAttackCost = 100f;
    private float blockMultiplier = 0.1f;
    private bool m_grounded = false;
    private int m_facingDirection = 1;

    // attack
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;

    // energy
    private float m_timeSinceEnergyGain = 0.0f;
    private float m_energyGainInterval = 0.5f;
    private float m_energyGainAmount = 1f;
    

    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime;

    // compoent refs
    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroAttack   m_attackSensor;
    [SerializeField] ResourceBar m_healthBar;
    [SerializeField] ResourceBar m_energyBar;
    //private bool                m_isWallSliding = false;
    private bool                m_rolling = false;


    // Movement var
    private float moveDir;
    private Vector2 moveVector;
    public bool canMove;

    /// <summary>
    /// Enum for tracking player state
    /// </summary>
    public enum EplayerState
    {
        Default,
        Attacking,
        Blocking,
        Dead
    }

    // Initialization, get compoent refs
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        //m_attackSensor = transform.Find("AttackSensor").GetComponent<Sensor_HeroAttack>();
        m_attackSensor = GetComponentInChildren<Sensor_HeroAttack>();

        health = maxHealth;

        // add player to player manager
        PlayerNumber =  PlayerManager.S_PlayerManager.PlayerJoined(this);
        print("Player Joined: Player " + PlayerNumber);
    }

    // ++ Inputs ++
    /// <summary>
    /// Gets movement value from Movement Input Action
    /// </summary>
    /// <param name="value"></param>
    void OnMovement(InputValue value)
    {
        if (playerState == EplayerState.Default && canMove) // Sets movement value when playerState is Default
        {
            moveDir = value.Get<float>();
            moveVector = new Vector2(moveDir * m_speed, m_body2d.velocity.y); 
            playerState = EplayerState.Default;
        }
    }

    /// <summary>
    /// Called by Attack Input Action
    /// </summary>
    void OnAttack()
    {
        if (m_timeSinceAttack > 0.25f && !m_rolling && energy - energyAttackCost > -0.1f)
        {
            energy -= energyAttackCost;
            m_energyBar.UpdateResourceBar(energy, maxEnergy);
            m_currentAttack++;

            // Loop back to one after third attack
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);
            m_attackSensor.DoAttack();
            playerState = EplayerState.Attacking;

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }
    }

    // ++ Player Join ++
    //public void OnPlayerJoinAction()
    //{
    //    print("player joined");
    //    //PlayerNumber = PlayerManager.S_PlayerManager.PlayerJoined(this);
    //}

    private void FixedUpdate()
    {
        // Movement 
        if (playerState == EplayerState.Default)
        {
            m_body2d.MovePosition(m_body2d.position + moveVector * Time.fixedDeltaTime);
        } else m_body2d.velocity = Vector2.zero; // Stops velocity if not in default state
    }

    //Update is called once per frame
    void Update()
    {
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;
        m_timeSinceEnergyGain += Time.deltaTime;

        if (m_timeSinceAttack > 0.15 && playerState == EplayerState.Attacking)
        {
            playerState = EplayerState.Default;
        }
        

        // energy regen
        if (m_timeSinceEnergyGain > m_energyGainInterval && energy != maxEnergy && playerState != EplayerState.Dead)
        {
            energy += m_energyGainAmount;
            m_energyBar.UpdateResourceBar(energy, maxEnergy);
            if  (energy > maxEnergy)
            {
                energy = maxEnergy;
            }
            m_timeSinceEnergyGain = 0.0f;
            //print("Energy Updated - Player: " + PlayerNumber);
        }

        // Increase timer that checks roll duration
        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Disable rolling if timer extends duration
        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
            //print("Just landed " + m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            //print("Started Falling " + m_grounded);
        }

        // -- Handle input and movement --
        //float inputX = Input.GetAxis("Horizontal");

        // Swap direction of sprite depending on walk direction
        if (moveDir > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
            m_attackSensor.transform.localPosition = m_attackSensorPosRight;
        }
        else if (moveDir < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
            m_attackSensor.transform.localPosition = m_attackSensorPosLeft;
        }

        // Move
        //if (!m_rolling)
        //    m_body2d.velocity = new UnityEngine.Vector2(inputX * m_speed, m_body2d.velocity.y);

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        //// -- Handle Animations --
        ////Wall Slide
        //m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        //m_animator.SetBool("WallSlide", m_isWallSliding);



        //Death
        //if (Input.GetKeyDown("e") && !m_rolling)
        //{
        //    m_animator.SetBool("noBlood", m_noBlood);
        //    m_animator.SetTrigger("Death");

        //}
        ////Hurt
        //else if (Input.GetKeyDown("q") && !m_rolling)
        //    m_animator.SetTrigger("Hurt");


        ////Attack
        //else if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        //{
        //    m_currentAttack++;

        //    // Loop back to one after third attack
        //    if (m_currentAttack > 3)
        //        m_currentAttack = 1;

        //    // Reset Attack combo if time since last attack is too large
        //    if (m_timeSinceAttack > 1.0f)
        //        m_currentAttack = 1;

        //    // Call one of three attack animations "Attack1", "Attack2", "Attack3"
        //    m_animator.SetTrigger("Attack" + m_currentAttack);
        //    AttackEnable();
        //    Invoke("AttackDisable", 0.1f);

        //    // Reset timer
        //    m_timeSinceAttack = 0.0f;
        //}

        //// Block
        //else if (Input.GetMouseButtonDown(1) && !m_rolling)
        //{
        //    m_animator.SetTrigger("Block");
        //    m_animator.SetBool("IdleBlock", true);
        //}

        //else if (Input.GetMouseButtonUp(1))
        //    m_animator.SetBool("IdleBlock", false);

        // Roll
        //else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        //{
        //    m_rolling = true;
        //    m_animator.SetTrigger("Roll");
        //    m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        //}


        //Jump
        //else if (Input.GetKeyDown("space") && m_grounded && !m_rolling)
        //{
        //    m_animator.SetTrigger("Jump");
        //    m_grounded = false;
        //    m_animator.SetBool("Grounded", m_grounded);
        //    m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
        //    m_groundSensor.Disable(0.2f);
        //}

        //Run
        if (Mathf.Abs(moveDir) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        //Idle
        else
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    // damage taken interface
    public void Damage(float damageAmount)
    {
        print("controller Damaged " + playerState);
        switch (playerState)
        {
            case EplayerState.Default:
                TakeDamage(damageAmount);
                break;
            case EplayerState.Attacking:
                print("clash");
                break;
            case EplayerState.Blocking:
                TakeDamage(damageAmount * blockMultiplier);
                break;
            case EplayerState.Dead:
                break;
        }
    }

    /// <summary>
    /// Handles damage taken and death when health reaches 0
    /// </summary>
    /// <param name="damageAmount"></param>
    private void TakeDamage(float damageAmount)
    {
        if (health - damageAmount > 0)
        {
            health -= damageAmount;
            m_healthBar.UpdateResourceBar(health, maxHealth);
            m_animator.SetTrigger("Hurt");
            print("Player " + PlayerNumber + "Damage Taken: " + damageAmount + " Health: " + health);
        }
        else //Death
        {
            playerState = EplayerState.Dead;
            health = 0;
            m_healthBar.UpdateResourceBar(health, maxHealth);
            print("Player " + PlayerNumber + " is Dead");
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");

            PlayerManager.S_PlayerManager.GameOver(PlayerNumber);
        }
            

    }

    // Animation Events
    // Called in slide animation.
    //void AE_SlideDust()
    //{
    //    Vector3 spawnPosition;

    //    if (m_facingDirection == 1)
    //        spawnPosition = m_wallSensorR2.transform.position;
    //    else
    //        spawnPosition = m_wallSensorL2.transform.position;

    //    if (m_slideDust != null)
    //    {
    //        // Set correct arrow spawn position
    //        GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
    //        // Turn arrow in correct direction
    //        dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
    //    }
    //}
}
