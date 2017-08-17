using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;
#pragma warning disable 0414 // Private field is assigned but never used

public class PlayerController : Photon.MonoBehaviour {
  [System.Serializable] public class Sounds {
    public AudioPlayer Player;
    public AudioClip JumpSound;
    public AudioClip LandSound;
    public AudioClip CollectibleSound;
    public AudioClip Pain;
    public AudioClip LevelFail;
    public AudioClip[] FootSteps;
  }

  [Header ("Movement")]
  public float moveSpeed = 5f;
  public float jumpMultiplier = 5f;
  public float jumpFrequency = 0.25f;
  [Tooltip ("Percent per second")]
  public float staminaDepletionRate = 0.1f;
  [Tooltip ("Seconds")]
  public float staminaRechargeDelay = 3.0f;
  public float staminaRechargeMultiplier = 1.5f;
  [Header ("Camera")]
  public GameObject Camera;
  public bool CameraObjectAvoidance = true;
  public bool rotateWithCamera = false;
  public bool firstPerson = true;
  public float MaxCameraDistance = 3f;
  public float playerHeight = 1.5f;
  public float crouchedHeight = 1.0f;
  [Header ("OSDs/HUD")]
  public GUIText collectedCounter;
  [Tooltip ("LifeOSD")]
  public GUIText lifeCounter;
  public GUIText timer;
  public GUIText stamina;
  [Tooltip ("LevelOSD")]
  public GUIText levelDisplay;
  [Tooltip ("UsernameOSD")]
  public GUIText usernameOSD;
  public float staminaCountBars = 20f;
  public GUIText debug;
  [Header ("MiniMap")]
  public Camera MiniMapCamera;
  public Vector3 miniMapRelativePosition;
  public bool miniMapRelativeY = false;
  [Header ("Look and Sound")]
  public bool useRenderSettingsFog = true;
  public float footstepSize = 0.5f;
  public float footstepSizeCrouched = 0.7f;
  public float footstepSizeSprinting = 0.3f;
  public Sounds sounds;
  [Header ("Misc.")]
  public float GameTime = 10f;
  public GameObject RagdollTemplate;
  public bool isDead = false;
  public bool spawned = false;
  public float flyDownTime = 6.0f;
  public float flyDownEndTime = 1.5f;
  public float deltaSendTime = 0.0f;
  public float deltaReceiveTime = 0.0f;
  public bool waitForSpawnLoading = true;

  public bool isLocalPlayer = false;

  Animator anim;
  Cinematic cinematic;
  Color startColor;
  GameObject Ragdoll;
  PostProcessingController PPC;
  Quaternion startCameraRotation, cameraSpawnRotation;
  Rigidbody rbody;
  TextMesh nameplate;
  Transform lastFloorTransform;
  Vector3 spawnLocation;
  Vector3 lastFloorTransformPosition;
  bool isGrounded = true;
  bool isCrouched = false;
  bool isSprinting = false;
  bool isUnderwater = false;
  bool isJumping = false;
  bool godMode = false;
  bool cinematicsFinished = false;
  float moveHorizontal = 0f;
  float moveVertical = 0f;
  float lookHorizontal = 0f;
  float lookVertical = 0f;
  float spawnCameraDistance = 1500f;
  float intendedCameraDistance;
  float turn = 0f;
  float forward = 0f;
  float moveAngle = 0f;
  float CurrentCameraDistance = 3f;
  float endTime = 0f;
  float staminaRemaining = 1.0f;
  float levelStartTime = 0f;
  float lastGroundedTime = 0f;
  float lastSprintTime = 0f;
  float lastJumpSoundTimejump = 0.0f;
  float lastJumpTime = 0.0f;
  float lastFootstepTime = 0.0f;
  float lastSprintInput = 0.0f;
  float timeInVehicle = 0.0f;
  float deathTime = 0.0f;
  float colliderStartPosition = 0f;

  void Awake() {
    isLocalPlayer = false;

    if (photonView.isMine) {
      LevelController.LocalPlayerInstance = gameObject;
      PhotonNetwork.playerName = GameData.username;
      isLocalPlayer = true;
    }
  }

  void Start() {
    isLocalPlayer = false;

    if (!photonView.isMine && PhotonNetwork.connected) { return; }

    isLocalPlayer = true;
    spawned = false;

    if (sounds.LandSound != null) { sounds.LandSound.LoadAudioData(); }

    if (sounds.JumpSound != null) { sounds.JumpSound.LoadAudioData(); }

    foreach (AudioClip step in sounds.FootSteps) {
      if (step != null) { step.LoadAudioData(); }
    }

    GameData.showCursor = false;
    cinematic = FindObjectOfType<Cinematic>();
    anim = GetComponent<Animator>();
    rbody = GetComponent<Rigidbody>();
    startColor = RenderSettings.fogColor;
    Camera = Instantiate (Camera);
    Camera.transform.parent = null;
    Camera.transform.rotation = Quaternion.identity;
    Camera.GetComponent<Camera>().enabled = true;
    Camera.GetComponent<AudioListener>().enabled = true;
    Camera.name = "CameraFor" + GameData.username;

    if (GetComponent<CapsuleCollider>() != null) {
      colliderStartPosition = GetComponent<CapsuleCollider>().center.y;
    }

    foreach (Camera cam in UnityEngine.Camera.allCameras) {
      cam.layerCullSpherical = true;
    }

    startCameraRotation = Camera.transform.rotation;
    intendedCameraDistance = MaxCameraDistance;
    PPC = GameObject.FindObjectOfType<PostProcessingController>();

    if (MiniMapCamera != null) { MiniMapCamera = Instantiate (MiniMapCamera); }

    GameObject temp;

    if (lifeCounter != null) {
      lifeCounter = Instantiate(lifeCounter);
    } else {
      temp = GameObject.Find ("LifeOSD");

      if (temp != null) { lifeCounter = temp.GetComponent<GUIText>(); }
    }

    if (levelDisplay != null) {
      levelDisplay = Instantiate(levelDisplay);
    } else {
      temp = GameObject.Find ("LevelOSD");

      if (temp != null) { levelDisplay = temp.GetComponent<GUIText>(); }
    }

    if (usernameOSD != null) {
      usernameOSD = Instantiate(usernameOSD);
    } else {
      temp = GameObject.Find ("UsernameOSD");

      if (temp != null) { usernameOSD = temp.GetComponent<GUIText>(); }
    }

    if (debug != null) { debug = Instantiate (debug); }

    levelStartTime = Time.time;
    lastGroundedTime = Time.time;
    lastSprintTime = Time.time;
    lastJumpSoundTimejump = Time.time;
    lastJumpTime = jumpFrequency;
    lastFootstepTime = Time.time;
  }

  void Update() {
    isLocalPlayer = photonView.isMine || !PhotonNetwork.connected;

    if (!GameData.loading && !Camera.activeSelf) { Camera.SetActive (true); }
    else if (GameData.loading && Camera.activeSelf) { Camera.SetActive (false); }

    rbody = GetComponent<Rigidbody>();

    if (!GameData.loading && UnityEngine.Camera.main != null) {
      nameplate = GetComponentInChildren<TextMesh>();
      nameplate.transform.LookAt (UnityEngine.Camera.main.transform.position);
      nameplate.transform.rotation *= Quaternion.Euler (0, 180f, 0);

      if (photonView.owner != null)
      { nameplate.text = photonView.owner.NickName; }
    }

    if (isLocalPlayer && intendedCameraDistance == 0 &&
        Time.time - levelStartTime > flyDownTime + flyDownEndTime) {
      if (!GameData.loading)
      { nameplate.GetComponent<MeshRenderer>().enabled = false; }
    }

    if (usernameOSD != null && !GameData.loading)
    { usernameOSD.text = nameplate.text; }

    // Cinematics
    if (cinematic != null && !cinematic.isDone)
    { return; }
    else if (cinematic != null)
    { cinematicsFinished = true; }

    // Death
    if (isDead) {
      if (Time.realtimeSinceStartup - deathTime >= 8f) {
        if (GameData.health > 0)
        { UnDead(); }
        else {
          if (GameData.tries > 0)
          { UnDead(); }
          else
          { GameData.MainMenu(); }
        }
      } else {
        Time.timeScale = Mathf.Lerp (
                           0.1f, 0.5f, (Time.realtimeSinceStartup - deathTime) / 8f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
      }
    }

    // Inputs and Player Controls
    float interact = 0f;
    float sprintInput = 0f;
    bool wasUnderwater = false;
    RaycastHit hitinfo = new RaycastHit();
    isGrounded =
      Physics.Raycast (transform.position + Vector3.up*0.05f, Vector3.down, out hitinfo, 0.2f);

    if (isLocalPlayer && !GameData.isPaused && !GameData.isChatOpen) {
      moveHorizontal = Input.GetAxis ("Horizontal");
      moveVertical = Input.GetAxis ("Vertical");
      lookHorizontal =
        Input.GetAxis ("Mouse X") + Input.GetAxis ("Joystick X") * 3f;
      lookVertical =
        Input.GetAxis ("Mouse Y") + Input.GetAxis ("Joystick Y") * 3f;
      interact = Input.GetAxis ("Interact");

      if (Input.GetButtonDown ("GodMode")) { godMode = !godMode; }

      isCrouched = Input.GetAxis ("Crouch") > 0.5;
      isJumping = Input.GetAxis ("Jump") > 0.5 && isGrounded && !isCrouched &&
                  lastJumpTime >= jumpFrequency;
      sprintInput = Input.GetAxis ("Sprint");
      isSprinting =
        (sprintInput > 0.5 && !isCrouched) || (isSprinting && !isGrounded);
      wasUnderwater = isUnderwater;
      isUnderwater = transform.position.y < TerrainGenerator.waterHeight;

      if (Input.GetButtonDown ("Toggle Third Person")) { ToggleThirdPerson(); }
    }

    if (wasUnderwater && !isUnderwater) { lastGroundedTime = Time.time; }

    if (isUnderwater) { isGrounded = false; }

    lastJumpTime += Time.deltaTime;

    if (isJumping) { lastJumpTime = 0.0f; }

    // Standing on platform
    // This is necessary to ensure the player moves with the platform they are
    // standing on. The position obtained from this is used to offset the
    // player's position.
    if (lastFloorTransform == null ||
        (hitinfo.transform != null &&
         hitinfo.transform.name != lastFloorTransform.name)) {
      lastFloorTransform = hitinfo.transform;

      if (lastFloorTransform != null)
      { lastFloorTransformPosition = lastFloorTransform.position; }
    }

    if (isGrounded && hitinfo.transform != null) {
      transform.position +=
        hitinfo.transform.position - lastFloorTransformPosition;
      lastFloorTransform = hitinfo.transform;
      lastFloorTransformPosition = lastFloorTransform.position;
    }

    // Debug HUD
    if (debug != null) {
      debug.text = "Horizontal: " + moveHorizontal + "\nVertical: " +
                   moveVertical + "\nMouse X: " + lookHorizontal +
                   "\nMouseY: " + lookVertical + "\nTime: " + Time.time;
    }

    if (!TerrainGenerator.doneLoadingSpawn && !spawned && waitForSpawnLoading) {
      levelStartTime = Time.time;
      Camera.transform.rotation = Quaternion.Euler (70f, 30f, 0f);
      cameraSpawnRotation = Camera.transform.rotation;
      spawnLocation = transform.position;
    } else if (cinematicsFinished && !spawned) {
      levelStartTime = Time.time;
      cameraSpawnRotation = Camera.transform.rotation;
      spawnLocation = transform.position;
      spawned = true;
    } else
    { spawned = true; }

    // Vehicles
    timeInVehicle += Time.deltaTime;

    if (GameData.Vehicle != null && GameData.Vehicle.fuelRemaining < 0)
    { ExitVehicle(); }

    if (GameData.Vehicle != null) {
      GameData.Vehicle.UpdateInputs (moveVertical, moveHorizontal,
                                     lookHorizontal, lookVertical, sprintInput,
                                     Camera.GetComponent<Camera>());
      transform.position =
        GameData.Vehicle.gameObject.transform.position + Vector3.up * 0.25f;
      transform.rotation = GameData.Vehicle.gameObject.transform.rotation;

      if (MiniMapCamera != null) {
        Vector3 mapPos = transform.position + miniMapRelativePosition;
        if (!miniMapRelativeY) mapPos.y = miniMapRelativePosition.y;
        MiniMapCamera.transform.position = mapPos;
      }

      if (GameData.Vehicle.fuelRemaining < 100) {
        usernameOSD.text +=
          "\n" + (GameData.Vehicle.isBoat
                  ? "Boat"
                  : "Car" + " has " +
                  Mathf.Round (GameData.Vehicle.fuelRemaining) +
                  " Fuel Remaining");
      }

      moveHorizontal = 0;
      moveVertical = 0;
      lookHorizontal = 0;
      lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = true;
      isGrounded = true;
      isJumping = false;

      if (timeInVehicle < 1.0f) {
        GameData.Vehicle.UpdateInputs (moveVertical, moveHorizontal,
                                       lookHorizontal, lookVertical, sprintInput,
                                       Camera.GetComponent<Camera>());
      }

      if (interact > 0.5 && timeInVehicle > 0.5f)
      { ExitVehicle(); }
    } else {
      RaycastHit raycast;
      Physics.Raycast (Camera.transform.position,
                       Camera.transform.rotation * Vector3.forward, out raycast,
                       10f);

      if (raycast.transform != null &&
          raycast.transform.CompareTag ("Vehicle")) {
        if (raycast.transform.gameObject.GetComponent<VehicleController>()
            .fuelRemaining > 0) {
          if (interact > 0.5f && timeInVehicle > 0.5f) {
            EnterVehicle (
              raycast.transform.gameObject.GetComponent<VehicleController>());
          } else {
            usernameOSD.text +=
              "\nPress \"E\" to enter " +
              (raycast.transform.gameObject.GetComponent<VehicleController>()
               .isBoat
               ? "boat"
               : "car");
          }
        } else {
          usernameOSD.text +=
            "\n" +
            (raycast.transform.gameObject.GetComponent<VehicleController>()
             .isBoat
             ? "boat"
             : "car" + " is out of fuel!");
        }
      }
    }

    // Prevent movement in first few seconds of the level or if dead, or if
    // paused, or if chat is open.
    if (Time.time - levelStartTime < flyDownTime || isDead ||
        GameData.isPaused || GameData.isChatOpen) {
      if (Input.GetKeyDown ("enter"))
      { levelStartTime = Time.time - flyDownTime; }

      if (GameData.isPaused)
      { levelStartTime += Time.deltaTime; }

      if (cameraSpawnRotation.w == 0) {
        Camera.transform.rotation = Quaternion.Euler (70f, 30f, 0f);
        cameraSpawnRotation = Camera.transform.rotation;
      }

      if (!GameData.isPaused && Time.time - levelStartTime < flyDownTime) {
        Camera.transform.rotation =
          Quaternion.Lerp (cameraSpawnRotation, startCameraRotation,
                           (Time.time - levelStartTime) / flyDownTime);
        intendedCameraDistance =
          Mathf.Lerp (spawnCameraDistance, MaxCameraDistance,
                      (Time.time - levelStartTime) / flyDownTime);
      }

      if (GameData.Vehicle != null) {
        timeInVehicle += Time.deltaTime;
        transform.position = GameData.Vehicle.gameObject.transform.position;

        if (interact > 0.5 && timeInVehicle > 0.5f) { GameData.Vehicle = null; }
      }

      moveHorizontal = 0;
      moveVertical = 0;
      lookHorizontal = 0;
      lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = false;
      isJumping = false;
    } else {
      if (Time.time - levelStartTime <
          flyDownTime + flyDownEndTime + Time.deltaTime &&
          Time.time - levelStartTime > flyDownTime + flyDownEndTime) {
        moveVertical = 1.0f;

        if (firstPerson) {
          intendedCameraDistance = 0f;
          rotateWithCamera = true;
        } else {
          intendedCameraDistance = MaxCameraDistance;
          rotateWithCamera = false;
        }

        if (intendedCameraDistance == 0 && isLocalPlayer) {
          SkinnedMeshRenderer[] renderers =
            GetComponentsInChildren<SkinnedMeshRenderer>();

          foreach (SkinnedMeshRenderer r in renderers) {
            r.shadowCastingMode =
              UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
          }
        } else if (!isLocalPlayer) {
          SkinnedMeshRenderer[] renderers =
            GetComponentsInChildren<SkinnedMeshRenderer>();

          foreach (SkinnedMeshRenderer r in renderers)
          { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        }
      }
    }

    // Collider
    CapsuleCollider collider = GetComponent<CapsuleCollider>();

    if (isCrouched && collider != null) {
      collider.height = crouchedHeight;
      if (colliderStartPosition != 0) {
        Vector3 temp = collider.center;
        temp.y = colliderStartPosition - (playerHeight - crouchedHeight) / 2f;
        collider.center = temp;
      }
    } else if (collider != null) {
      collider.height = playerHeight;
      if (colliderStartPosition != 0) {
        Vector3 temp = collider.center;
        temp.y = colliderStartPosition;
        collider.center = temp;
      }
    }

    // Stamina
    if (isSprinting) { lastSprintTime = Time.time; }

    if (Time.time - lastSprintTime >= staminaRechargeDelay) {
      staminaRemaining +=
        staminaDepletionRate * Time.deltaTime * staminaRechargeMultiplier;

      if (staminaRemaining > 1.0) { staminaRemaining = 1.0f; }
    }

    if (staminaRemaining <= 0) {
      isSprinting = false;
      sprintInput = lastSprintInput -= Time.deltaTime;
    } else if (isGrounded && isSprinting &&
               (moveHorizontal != 0 || moveVertical != 0))
    { staminaRemaining -= staminaDepletionRate * Time.deltaTime; }

    lastSprintInput = sprintInput;

    // Start countdown once player moves.
    if (! (moveHorizontal == 0 && moveVertical == 0 && !isJumping)
        && endTime == 0f)
    { endTime = Time.time + GameTime; }

    if (GameData.health <= 0)
    { Dead(); }

    // HUD
    if (!GameData.isPaused) {
      if (collectedCounter != null) {
        collectedCounter.text =
          "Bombs Remaining: " + GameData.collectedCollectibles;
      }

      if (lifeCounter != null) {
        lifeCounter.text =
          GameData.health + " Health, " + GameData.tries + " Tries";
      }

      if (GameData.Vehicle == null) {
        if (stamina != null) {
          stamina.text = "Stamina: ";

          for (int i = 0; i < (int) (staminaRemaining * staminaCountBars); i++)
          { stamina.text += "|"; }

          for (int i = (int) (staminaCountBars * staminaRemaining);
               i < staminaCountBars; i++)
          { stamina.text += "!"; }
        }
      } else if (stamina != null)
      { stamina.text = ""; }

      if (GameData.Vehicle == null) {
      }

      if (timer != null) {
        float timeRemaining = Mathf.Round ( (endTime - Time.time) * 10f) / 10f;

        if (endTime == 0f) { timeRemaining = GameTime; }

        string timeRemaining_ = "";

        if (timeRemaining > 0)
        { timeRemaining_ += timeRemaining; }
        else
        { timeRemaining_ += "0.0"; }

        if (timeRemaining % 1 == 0 && timeRemaining > 0) { timeRemaining_ += ".0"; }

        timer.text = timeRemaining_;

        if (!isDead && timeRemaining <= 0.7f * 4f) {
          Time.timeScale = timeRemaining / 4f + 0.3f;
          Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        if (!isDead && timeRemaining <= 0) {
          GameData.health--;
          Dead();
        }
      }
    } else {
      if (collectedCounter != null)
      { collectedCounter.text = ""; }

      if (lifeCounter != null)
      { lifeCounter.text = ""; }

      if (stamina != null)
      { stamina.text = ""; }

      if (timer != null) {
        endTime += Time.deltaTime;
        timer.text = "";
      }

      if (usernameOSD != null)
      { usernameOSD.text = ""; }
    }

    if (levelDisplay != null)
    { levelDisplay.text = "Level: " + GameData.getLevel(); }

    if (GameData.Vehicle == null) {
      // Movement
      Vector3 movement =
        moveHorizontal * Vector3.right + moveVertical * Vector3.forward;
      movement = Vector3.ClampMagnitude (movement, 1.0f);

      if (isCrouched) {
        forward = movement.magnitude;
        movement *= moveSpeed * 0.6f;
      } else {
        forward = movement.magnitude / Mathf.Lerp (2.0f, 1.0f, sprintInput);
        movement *= moveSpeed * Mathf.Lerp (1.0f, 2.5f, sprintInput);
      }

      if (godMode) {
        movement += Input.GetAxis ("Jump") * Vector3.up;
        movement -= Input.GetAxis ("Crouch") * Vector3.up;
        movement *= 30f;
      } else {
        if (isUnderwater) {
          if (transform.position.y < TerrainGenerator.waterHeight - 0.8f) {
            movement +=
              Mathf.Clamp (
                rbody.velocity.y +
                2.0f * Time.deltaTime * (TerrainGenerator.waterHeight -
                                         transform.position.y),
                -moveSpeed *
                ( (TerrainGenerator.waterHeight - transform.position.y) /
                  2.5f),
                moveSpeed *
                ( (TerrainGenerator.waterHeight - transform.position.y) /
                  2.5f)) *
              Vector3.up;
          } else {
            movement +=
              (rbody.velocity.y - 9.81f * 3f * Time.deltaTime) * Vector3.up;
          }
        } else {
          rbody.velocity = new Vector3 (
            rbody.velocity.x,
            ( (isJumping ? (moveSpeed * jumpMultiplier) : 0.0f) +
              ( (/*isGrounded ||*/ isJumping)
                ? rbody.velocity.y
                : (rbody.velocity.y - 9.81f * 4f * Time.deltaTime))),
            rbody.velocity.z);
          movement += rbody.velocity.y * Vector3.up;
        }
      }

      movement =
        Quaternion.Euler (0, Camera.transform.eulerAngles.y, 0) * movement;

      if (movement.magnitude <= 0.02) { movement = Vector3.zero; }

      rbody.velocity = movement;

      // Camera
      if (isLocalPlayer) {
        if (MiniMapCamera != null) {
          Vector3 mapPos = transform.position + miniMapRelativePosition;
          if (!miniMapRelativeY) mapPos.y = miniMapRelativePosition.y;
          MiniMapCamera.transform.position = mapPos;
        }

        Camera.transform.rotation = Quaternion.Euler (
                                      Camera.transform.eulerAngles.x - lookVertical,
                                      Camera.transform.eulerAngles.y + lookHorizontal, 0);

        if (CameraObjectAvoidance && spawned) {
          RaycastHit hit;
          Physics.Linecast (
            transform.position +
            Vector3.up * (isCrouched ? crouchedHeight : playerHeight),
            Camera.transform.position, out hit, LayerMask.GetMask ("Ground"));

          if (hit.transform != Camera.transform && hit.transform != transform &&
              hit.transform != null)
          { CurrentCameraDistance = hit.distance; }
          else {
            CurrentCameraDistance += 5.0f * Time.deltaTime;

            if (CurrentCameraDistance > intendedCameraDistance)
            { CurrentCameraDistance = intendedCameraDistance ; }
          }
        }

        if (!spawned || isDead) { CurrentCameraDistance = intendedCameraDistance ; }

        Vector3 newCameraPos =
            Vector3.up * (isCrouched ? crouchedHeight : playerHeight) +
            Vector3.ClampMagnitude(
                (Vector3.left *
                     (Mathf.Sin(Camera.transform.eulerAngles.y / 180f *
                                Mathf.PI) -
                      Mathf.Sin(Camera.transform.eulerAngles.y / 180f *
                                Mathf.PI) *
                          Mathf.Sin((-45f + Camera.transform.eulerAngles.x) /
                                    90f * Mathf.PI)) +
                 Vector3.back *
                     (Mathf.Cos(Camera.transform.eulerAngles.y / 180f *
                                Mathf.PI) -
                      Mathf.Cos(Camera.transform.eulerAngles.y / 180f *
                                Mathf.PI) *
                          Mathf.Sin((-45f + Camera.transform.eulerAngles.x) /
                                    90f * Mathf.PI)) +
                 Vector3.up *
                     Mathf.Sin(Camera.transform.eulerAngles.x / 180f *
                               Mathf.PI)),
                1.0f) *
                CurrentCameraDistance;

        if (!isDead || Ragdoll == null)
        { newCameraPos += transform.position; }
        else
        { newCameraPos += Ragdoll.transform.position; }

        if (isDead) {
          Vector3 velocity = Vector3.zero;
          newCameraPos = Vector3.SmoothDamp (Camera.transform.position,
                                             newCameraPos, ref velocity, 0.05f);
        } else if (GameData.cameraDamping && spawned &&
                   (intendedCameraDistance != 0 ||
                    Time.time - levelStartTime <
                    flyDownTime + flyDownEndTime)) {
          Vector3 velocity = Vector3.zero;
          newCameraPos = Vector3.SmoothDamp (Camera.transform.position,
                                             newCameraPos, ref velocity, 0.05f);
        }

        if (newCameraPos.y <= TerrainGenerator.waterHeight + 0.4f)
        { newCameraPos += Vector3.down * 0.8f; }

        Camera.transform.position = newCameraPos;

        if (Camera.transform.eulerAngles.x > 75.0f &&
            Camera.transform.eulerAngles.x < 90.0f) {
          Camera.transform.rotation =
            Quaternion.Euler (75.0f, Camera.transform.eulerAngles.y, 0f);
        } else if (Camera.transform.eulerAngles.x < 360f - 75.0f &&
                   Camera.transform.eulerAngles.x > 90.0f) {
          Camera.transform.rotation =
            Quaternion.Euler (-75.0f, Camera.transform.eulerAngles.y, 0f);
        }

        if (isDead && Ragdoll != null) {
          Transform[] children = Ragdoll.GetComponentsInChildren<Transform>();
          Transform target = transform;
          target.position += Vector3.up * 1.2f;

          foreach (Transform target_ in children) {
            if (target_.name.Contains ("Head_M")) {
              target = target_;
              break;
            }
          }

          Quaternion startRot = Camera.transform.rotation;
          Camera.transform.LookAt (target.position + Vector3.up * 0.2f);
          Camera.transform.rotation =
            Quaternion.Lerp (startRot, Camera.transform.rotation, 0.1f);
        }

        // Rotation
        if (rotateWithCamera) {
          rbody.transform.rotation = Quaternion.identity;
          transform.rotation =
            Quaternion.Euler (0, Camera.transform.eulerAngles.y, 0);
        } else {
          moveAngle =
            Mathf.Atan (moveHorizontal / moveVertical) * 180f / Mathf.PI;

          if (! (moveAngle >= 0 || moveAngle < 0))
          { moveAngle = 0f; }

          if (moveVertical < 0) { moveAngle += 180f; }

          if (Mathf.Abs (rbody.velocity.x) > 0.01f ||
              Mathf.Abs (rbody.velocity.z) > 0.01f) {
            moveAngle += Camera.transform.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler (
                                    0f, Mathf.LerpAngle (rbody.transform.eulerAngles.y, moveAngle,
                                        0.07f),
                                    0f);
            rbody.transform.rotation = Quaternion.identity;
            transform.rotation = rotation;
          }
        }

        // VFX
        float vignette = 0.0f;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag ("Enemy");
        float enemydistance = 150f;

        for (int i = 0; i < enemies.Length; i++) {
          float tempdist =
            (enemies[i].transform.position - transform.position).magnitude;

          if (tempdist < enemydistance)
          { enemydistance = tempdist; }
        }

        vignette = Mathf.Lerp (0.4f, .267f, enemydistance / 150f);

        if (PPC == null)
        { PPC = GameObject.FindObjectOfType<PostProcessingController>(); }

        if (PPC != null) { PPC.vignette.intensity = vignette; }

        if (useRenderSettingsFog) {
          if (Camera.transform.position.y > TerrainGenerator.waterHeight) {
            RenderSettings.fogStartDistance = 2000 * (1 - (vignette / 0.45f));
            RenderSettings.fogEndDistance = 2000 * (1 - (vignette / 0.45f));
            RenderSettings.fogColor =
              Color.Lerp (startColor, Color.red, (vignette / 0.45f));
          } else {  // Underwater
            RenderSettings.fogStartDistance =
              Mathf.Lerp (RenderSettings.fogStartDistance, 0f, 0.5f);
            RenderSettings.fogEndDistance =
              Mathf.Lerp (RenderSettings.fogEndDistance, 30f, 0.5f);
            RenderSettings.fogColor = (Color.blue + Color.white) / 2;
          }
        }
      }

      // Sound
      if (isGrounded && Time.time - lastGroundedTime >= 0.1f &&
          sounds.Player != null)
      { PlaySound (sounds.LandSound); }

      if (isJumping && Time.time - lastJumpSoundTimejump >= 0.5f &&
          sounds.Player != null) {
        PlaySound (sounds.JumpSound);
        lastJumpSoundTimejump = Time.time;
      }

      if (isGrounded && (moveVertical != 0 || moveHorizontal != 0)) {
        if ( (isSprinting &&
              Time.time - lastFootstepTime >= footstepSizeSprinting) ||
             (isCrouched &&
              Time.time - lastFootstepTime >= footstepSizeCrouched) ||
             (!isSprinting && !isCrouched &&
              Time.time - lastFootstepTime >= footstepSize)) {
          lastFootstepTime = Time.time;

          if (sounds.FootSteps.Length > 0) {
            AudioClip footstepSound =
              sounds.FootSteps[ (int) Random.Range (0, sounds.FootSteps.Length)];
            PlaySound (footstepSound);
          }
        }
      }
    }

    if (isGrounded) { lastGroundedTime = Time.time; }
  }

  void ToggleThirdPerson() {
    firstPerson = !firstPerson;

    if (firstPerson) {
      intendedCameraDistance = 0f;
      rotateWithCamera = true;
      SkinnedMeshRenderer[] renderers =
        GetComponentsInChildren<SkinnedMeshRenderer>();

      foreach (SkinnedMeshRenderer r in renderers) {
        r.shadowCastingMode =
          UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
      }
    } else {
      intendedCameraDistance = MaxCameraDistance;
      rotateWithCamera = false;
      SkinnedMeshRenderer[] renderers =
        GetComponentsInChildren<SkinnedMeshRenderer>();

      foreach (SkinnedMeshRenderer r in renderers)
      { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
    }
  }

  void Dead() {
    if (isDead) { return; }

    isDead = true;
    GameData.tries--;
    ExitVehicle();

    if (GameData.tries <= 0)
    { PlaySound (sounds.LevelFail, 1.0f); }
    else
    { PlaySound (sounds.Pain, 1.0f); }

    intendedCameraDistance += 10f;
    deathTime = Time.realtimeSinceStartup;
    GetComponent<Rigidbody>().isKinematic = true;

    if (RagdollTemplate != null) {
      Ragdoll = Instantiate (RagdollTemplate);
      Ragdoll.SetActive (true);
      Ragdoll.transform.position = transform.position;
      Ragdoll.transform.rotation = transform.rotation;

      foreach (Transform parent in GetComponentsInChildren<Transform>()) {
        foreach (
          Transform ragdoll in Ragdoll.GetComponentsInChildren<Transform>()) {
          if (ragdoll.name.Equals (parent.name)) {
            ragdoll.gameObject.transform.position = parent.position;
            ragdoll.gameObject.transform.rotation = parent.rotation;
          }
        }
      }

      Ragdoll.GetComponent<Rigidbody>().velocity = rbody.velocity;

      foreach (SkinnedMeshRenderer renderer in
               GetComponentsInChildren<SkinnedMeshRenderer>())
      { renderer.enabled = false; }

      GetComponent<CapsuleCollider>().enabled = false;
      GetComponent<Animator>().enabled = false;
      // Camera.transform.eulerAngles = new Vector3(85f, 0, 0);
    }
  }
  void UnDead() {
    transform.position = spawnLocation;
    transform.rotation = Quaternion.identity;
    Camera.transform.rotation = Quaternion.identity;
    spawned = false;
    isDead = false;
    GameData.health = 5;
    Time.timeScale = 1.0f;
    Time.fixedDeltaTime = 0.02f * Time.timeScale;
    intendedCameraDistance -= 10;
    GetComponent<Rigidbody>().isKinematic = false;
    Destroy (Ragdoll);
    GetComponent<CapsuleCollider>().enabled = true;
    GetComponent<Animator>().enabled = true;
  }

  public void EnterVehicle (VehicleController vehicle) {
    if (vehicle == null) { return; }

    GameData.Vehicle = vehicle;
    timeInVehicle = 0.0f;
    GetComponent<Collider>().enabled = false;

    if (isLocalPlayer) {
      SkinnedMeshRenderer[] renderers =
        GetComponentsInChildren<SkinnedMeshRenderer>();

      foreach (SkinnedMeshRenderer r in renderers)
      { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
    }
  }
  public void ExitVehicle() {
    if (GameData.Vehicle == null) { return; }

    timeInVehicle = 0.0f;
    GetComponent<Collider>().enabled = true;
    transform.position = GameData.Vehicle.transform.position + Vector3.up * 2f;
    transform.rotation = Quaternion.Euler (
                           0, GameData.Vehicle.transform.rotation.eulerAngles.y, 0);
    rbody.velocity = Vector3.zero;
    Camera.transform.rotation = transform.rotation;
    SkinnedMeshRenderer[] renderers =
      GetComponentsInChildren<SkinnedMeshRenderer>();

    foreach (SkinnedMeshRenderer r in renderers)
    { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }

    GameData.Vehicle = null;
  }

  void OnAnimatorIK() {
    if (!photonView.isMine && PhotonNetwork.connected) { return; }

    if (anim == null) { anim = GetComponent<Animator>(); }
    if (anim == null) { return; }

    if (rbody == null) { rbody = GetComponent<Rigidbody>(); }
    if (rbody == null) { return; }

    if (Mathf.Abs (rbody.velocity.x) > 0.02f ||
        Mathf.Abs (rbody.velocity.z) > 0.02f) {
      turn = (moveAngle - anim.bodyRotation.eulerAngles.y) / 180f;

      while (turn < -1) { turn += 2; }
      while (turn > 1) { turn -= 2; }
    } else {
      turn = 0f;
    }

    anim.SetFloat ("Forward", forward);
    anim.SetFloat ("Turn", turn);

    if (!isUnderwater) {
      anim.SetFloat ("Jump", -9 + (Time.time - lastGroundedTime) * 9f);
      anim.SetFloat ("JumpLeg", -1 + (Time.time - lastGroundedTime) * 4f);
    } else {
      anim.SetFloat ("Jump", Mathf.Abs ( (Time.time * 200f % 200f - 100) / 200f));
      anim.SetFloat ("JumpLeg",
                     Mathf.Abs ( (Time.time * 200f % 400f - 200) / 400f));
    }

    anim.SetBool ("OnGround", isCrouched || godMode ||
                  (Time.time - lastGroundedTime <= 0.1f));
    anim.SetBool ("Crouch", isCrouched);
  }

  void OnTriggerEnter (Collider other) {
    Debug.Log (other);

    if (other.gameObject.CompareTag ("Collectible") &&
        (endTime > Time.time || timer == null)) {
      Destroy (other.gameObject);
      GameData.collectedCollectibles += 10;
      PlaySound (sounds.CollectibleSound);
    } else if (other.gameObject.CompareTag ("Explosion") ||
               other.gameObject.CompareTag ("Enemy")) {
      GameData.health = 0;
      Dead();
    } else if (other.gameObject.CompareTag ("EnemyProjectile")) {
      Destroy (other.gameObject);
      GameData.health--;
      Dead();
    }
  }
  void PlaySound (AudioClip clip, float volume = -1f) {
    if (sounds.Player != null && clip != null && GameData.soundEffects) {
      AudioPlayer player = Instantiate (sounds.Player, transform.position,
                                        Quaternion.identity) as AudioPlayer;
      player.clip = clip;

      if (volume >= 0f && volume <= 1f)
      { player.volume = volume; }
    }
  }
  void onDestroy() {
    GameData.showCursor = true;
  }
}


