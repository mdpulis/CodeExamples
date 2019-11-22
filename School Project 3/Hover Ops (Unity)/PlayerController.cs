using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EAE.Race.Player
{
    /// <summary>
    /// Controls the player
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerController : MonoBehaviour
    {
        public Transform Models;
        public Transform FlipRotationPoint;

        public Transform HoverboardLocation;
        public Transform CharacterLocation;

        public GameObject RegularBoxCollider;
        public GameObject SlidingBoxCollider;
        public GameObject SlidingBoxColliderTopTest;

        //public modifiable variables
        public float BoardSpeed = 3.0f;
        public float RotateSpeed = 30.0f;
        public float BoostMod = 2.0f;
        public float JumpForce = 7500.0f;
        public float straightDeadZone;

        //private gameplay variables
        private bool racing = false;

        private bool movingLeft = false;
        private bool movingRight = false;

        private bool boosting = false;
        private float currentBoostTime = 0.0f;
        private const float MAX_BOOST_TIME = 3.0f;

        private bool flipping = false;
        private float currentFlipTime = 0.0f;
        private const float MAX_FLIP_TIME = 1.0f;
        private int airFlips = 0;

        private float startRotation = 0.0f;
        private float endRotation = 0.0f;

        private bool sliding = false;
        private float currentSlideTime = 0.0f;
        private const float MAX_SLIDE_TIME = 1.0f;

        private float distToGround = 1f;
        private bool isGrounded;
        private bool beganJump = false;

        //private component references
        private Rigidbody playerRigidbody;
        private Animator playerAnimator;


        //private object references
        private Transform levelStartPosition;
        private LevelFinishUI levelFinishUI;

        //visual components
        public AnimationManager anim;
        private TimedEffect speedEffect;
        public PlayerVoiceManager playerVoice;


        #region Setup
        private void Awake()
        {
            racing = true;
            playerRigidbody = this.GetComponent<Rigidbody>();

            if(GameObject.FindGameObjectWithTag("LevelStartPosition") != null)
                levelStartPosition = GameObject.FindGameObjectWithTag("LevelStartPosition").GetComponent<Transform>();
            levelFinishUI = FindHelper.FindObjectOfTypeEvenIfInactive<LevelFinishUI>();

            SlidingBoxCollider.SetActive(false); //turn off just in case it's on

            LookForReferences();
            SetLocationToDefault();
        }

        /// <summary>
        /// Get references after things have loaded
        /// </summary>
        public void LookForReferences()
        {
            playerAnimator = this.GetComponentInChildren<Animator>();

            anim = GetComponentInChildren<AnimationManager>();
            speedEffect = Camera.main.GetComponentInChildren<TimedEffect>();
            playerVoice = GetComponent<PlayerVoiceManager>();
        }

        /// <summary>
        /// Sets the location to the level's default
        /// </summary>
        public void SetLocationToDefault()
        {
            this.transform.SetPositionAndRotation(levelStartPosition.position, levelStartPosition.rotation);
        }

        /// <summary>
        /// Sets the character model for the object
        /// </summary>
        public void SetCharacterModel(GameObject newCharacterModel)
        {
            Instantiate(newCharacterModel).transform.SetParent(CharacterLocation, false);
        }

        /// <summary>
        /// Sets the hoverboard model for the object
        /// </summary>
        public void SetHoverboardModel(GameObject newHoverboardModel)
        {
            Instantiate(newHoverboardModel).transform.SetParent(HoverboardLocation, false);
        }
        #endregion Setup


        private void Update()
        {
            if (!racing)
                return;

            if (isGrounded) //if grounded
            {
                if (!flipping && !sliding && Input.GetKeyDown(KeyCode.W))
                {
                    Jump();
                }
                else
                if (!flipping && !sliding && Input.GetKeyDown(KeyCode.S))
                {
                    StartSlide();
                }
            }
            else //if in the air
            {
                if (!flipping && !sliding && Input.GetKeyDown(KeyCode.W))
                {
                    StartFlip();
                }
            }

        }

        // Fixed Update is called 50 times per second
        void FixedUpdate()
        {
            if (!racing)
                return;

            CheckGrounded();

            if (movingLeft || Input.GetKey(KeyCode.A))
            {
                transform.Rotate(-Vector3.up * RotateSpeed * Time.deltaTime);
            }
            else if (movingRight || Input.GetKey(KeyCode.D))
            {
                transform.Rotate(Vector3.up * RotateSpeed * Time.deltaTime);
            }

            if (!boosting)
                transform.Translate(Vector3.forward * BoardSpeed * Time.deltaTime);
            else
                transform.Translate(Vector3.forward * BoardSpeed * BoostMod * Time.deltaTime);

            if (boosting)
            {
                currentBoostTime += Time.fixedDeltaTime;
                if (currentBoostTime > MAX_BOOST_TIME)
                {
                    EndBoost();
                }
            }

            if (flipping)
            {
                currentFlipTime += Time.fixedDeltaTime;

                float rotVal = (Time.fixedDeltaTime / MAX_FLIP_TIME) * 360.0f;
                Models.RotateAround(FlipRotationPoint.position, Models.right, rotVal);


                if (currentFlipTime > MAX_FLIP_TIME)
                {
                    EndFlip();
                    Models.localRotation = Quaternion.Euler(0, 0, 0);
                    Models.localPosition = new Vector3(0, 0, 0);
                }
            }

            if (sliding)
            {
                currentSlideTime += Time.fixedDeltaTime;
                if (currentSlideTime > MAX_SLIDE_TIME)
                {
                    EndSlide();
                }
            }

        }


        #region Movement
        public void setMovingLeft(bool state)
        {
            this.movingLeft = state;
        }

        public void setMovingRight(bool state)
        {
            this.movingRight = state;
        }
        #endregion

        #region Grounded
        public void CheckGrounded()
        {
            isGrounded = Physics.Raycast(transform.position + new Vector3(0, distToGround, 0), -Vector3.up, distToGround + 0.5f);

            if (!beganJump && !isGrounded)
            {
                beganJump = true;
                airFlips = 0;
            }
            else if (beganJump && isGrounded)
            {
                beganJump = false;
                if (airFlips > 0)
                    StartBoost();

                airFlips = 0;
            }
        }

        public bool IsGrounded()
        {
            return isGrounded;
        }
        #endregion


        #region Boost
        /// <summary>
        /// Starts the boost for the player
        /// </summary>
        public void StartBoost()
        {
            currentBoostTime = 0.0f;
            boosting = true;

            //Visual Effects
            if (anim != null)
                anim.TriggerState(AnimationManager.states.Win);
            speedEffect.triggerEffect();
            playerVoice.TriggerVoiceEffect(PlayerVoiceManager.Emotion.Excited);
            playerVoice.TriggerVoiceEffect(PlayerVoiceManager.Emotion.Boost);
        }

        /// <summary>
        /// Ends the boost for the player
        /// </summary>
        public void EndBoost()
        {
            currentBoostTime = 0.0f;
            boosting = false;
        }
        #endregion Boost

        #region Jump
        /// <summary>
        /// Pushes the player upwards
        /// </summary>
        public void Jump()
        {
            playerRigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);

            if (playerAnimator != null)
                playerAnimator.SetTrigger("JumpT");
            else
                Debug.Log("No player animator found");

            playerVoice.TriggerVoiceEffect(PlayerVoiceManager.Emotion.Jump);
        }

        /// <summary>
        /// Can the player jump right now?
        /// </summary>
        public bool CanJump()
        {
            if (isGrounded && !flipping && !sliding)
                return true;
            else
                return false;
        }
        #endregion Jump

        #region Slide
        /// <summary>
        /// Starts the slide
        /// </summary>
        public void StartSlide()
        {
            RegularBoxCollider.SetActive(false);
            SlidingBoxCollider.SetActive(true);

            currentSlideTime = 0.0f;
            sliding = true;

            if (playerAnimator != null)
                playerAnimator.SetTrigger("Slide");
            else
                Debug.Log("No player animator found");

            playerVoice.TriggerVoiceEffect(PlayerVoiceManager.Emotion.Slide);
        }

        /// <summary>
        /// Ends the slide
        /// </summary>
        public void EndSlide()
        {
            RegularBoxCollider.SetActive(true);
            SlidingBoxCollider.SetActive(false);

            currentSlideTime = 0.0f;
            sliding = false;
        }

        /// <summary>
        /// Can the player slide right now?
        /// </summary>
        public bool CanSlide()
        {
            if (isGrounded && !flipping && !sliding)
                return true;
            else
                return false;
        }
        #endregion Slide

        #region Flip
        /// <summary>
        /// Starts the flip for the player
        /// </summary>
        public void StartFlip()
        {
            currentFlipTime = 0.0f;
            flipping = true;

            startRotation = Models.localEulerAngles.x;
            endRotation = startRotation + 360.0f;
        }

        /// <summary>
        /// Ends the flip for the player
        /// </summary>
        public void EndFlip()
        {
            currentFlipTime = 0.0f;
            flipping = false;

            airFlips++;
        }

        /// <summary>
        /// Can the player flip right now?
        /// </summary>
        public bool CanFlip()
        {
            if (!isGrounded && !flipping && !sliding)
                return true;
            else
                return false;
        }
        #endregion Flip

        #region End Racing
        /// <summary>
        /// Ends the race and movement
        /// </summary>
        public void EndRacing()
        {
            racing = false;
            levelFinishUI.gameObject.SetActive(true);
        }

        /// <summary>
        /// Resets racing and sets to default position
        /// </summary>
        public void ResetRacing()
        {
            levelFinishUI.gameObject.SetActive(false);
            SetLocationToDefault();
            racing = true;
        }
        #endregion End Racing

        #region Getters
        /// <summary>
        /// Is the player currently racing?
        /// </summary>
        public bool IsRacing()
        {
            return racing;
        }

        #endregion
    }
}