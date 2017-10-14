// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;

[RequireComponent(typeof (MyCameraController))]
public class OldPlayerController : Photon.MonoBehaviour {
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
  public float playerHeight = 1.5f;
  public float crouchedHeight = 1.0f;
  [Header ("OSDs/HUD")]
  [Tooltip ("UsernameOSD")]
  public GUIText usernameOSD;
  public GUIText debug;
  [Header ("MiniMap")]
  public Camera MiniMapCamera;
  public Vector3 miniMapRelativePosition = new Vector3(0, 2001, 0);
  public bool miniMapRelativeY = false;
  [Header ("Look and Sound")]
  public bool useRenderSettingsFog = false;
  public float footstepSize = 0.5f;
  public float footstepSizeCrouched = 0.7f;
  public float footstepSizeSprinting = 0.3f;
  public Sounds sounds;
  [Header ("Misc.")]
  public GameObject RagdollTemplate;
  public float flyDownTime = 6.0f;
  public float flyDownEndTime = 1.5f;
  public bool waitForSpawnLoading = true;

  [HideInInspector] public bool isDead = false;
  [HideInInspector] public bool spawned = false;
  [HideInInspector] public bool isLocalPlayer = false;

  Animator anim;
  Cinematic cinematic;
  CapsuleCollider collider_;
  GameObject Ragdoll;
  MyCameraController cam;
  Quaternion startCameraRotation, cameraSpawnRotation;
  Rigidbody rbody;
  SkinnedMeshRenderer[] meshRenderers;
  TextMesh nameplate;
  Transform lastFloorTransform;
  Transform Head;
  Vector3 spawnLocation;
  Vector3 lastFloorTransformPosition;
  bool isGrounded = true;
  bool isCrouched = false;
  bool isSprinting = false;
  bool isUnderwater = false;
  bool isJumping = false;
  bool godMode = false;
  bool cinematicsFinished = false;
  bool camFirstPerson = true;
  bool camDistanceSnap = false;
  float moveHorizontal = 0f;
  float moveVertical = 0f;
  float spawnCameraDistance = 1500f;
  float startCameraDistance = 3f;
  float turn = 0f;
  float forward = 0f;
  float moveAngle = 0f;
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

    if (photonView.isMine || !PhotonNetwork.connected) {
      LevelController.LocalPlayerInstance = gameObject;
      PhotonNetwork.playerName = GameData.username;
      cam = GetComponent<MyCameraController>();
      cam.Initialize();
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
    collider_ = GetComponent<CapsuleCollider>();
    anim = GetComponent<Animator>();
    rbody = GetComponent<Rigidbody>();

    if (GetComponent<CapsuleCollider>() != null) {
      colliderStartPosition = collider_.center.y;
    }

    meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

    startCameraRotation = cam.cam.transform.rotation;
    Transform[] children = GetComponentsInChildren<Transform>();
    Head = transform;
    foreach (Transform target_ in children) {
      if (target_.name.Contains("Head_M")) {
        Head = target_;
        break;
      }
    }
    if (cam == null) cam = GetComponent<MyCameraController>();
    cam.UpdateTarget(Head);
    startCameraDistance = cam.MaxCameraDistance;
    camFirstPerson = cam.firstPerson;
    camDistanceSnap = cam.distanceSnap;

    if (MiniMapCamera != null) { MiniMapCamera = Instantiate (MiniMapCamera); }

    if (usernameOSD != null) {
      usernameOSD = Instantiate(usernameOSD);
    } else {
      GameObject temp = GameObject.Find ("UsernameOSD");

      if (temp != null) { usernameOSD = temp.GetComponent<GUIText>(); }
    }

    if (debug != null) { debug = Instantiate (debug); }

    nameplate = GetComponentInChildren<TextMesh>();
    nameplate.GetComponent<MeshRenderer>().enabled = false;

    levelStartTime = Time.time;
    lastGroundedTime = Time.time;
    lastJumpTime = jumpFrequency;
    lastSprintTime = Time.time;
    lastJumpSoundTimejump = Time.time;
    lastFootstepTime = Time.time;
  }

  void Update() {
    isLocalPlayer = photonView.isMine || !PhotonNetwork.connected;

    if (cam == null) cam = GetComponent<MyCameraController>();
    if (cam != null && cam.cam != null) {
      if (!GameData.loading && !cam.cam.activeSelf) {
        cam.cam.SetActive(true);
      } else if (GameData.loading && cam.cam.activeSelf) {
        cam.cam.SetActive(false);
      }
    }

    rbody = GetComponent<Rigidbody>();

    if (!GameData.loading && UnityEngine.Camera.main != null) {
      if (nameplate == null) nameplate = GetComponentInChildren<TextMesh>();
      if (nameplate != null) {
        nameplate.transform.LookAt(UnityEngine.Camera.main.transform.position);
        nameplate.transform.rotation *= Quaternion.Euler(0, 180f, 0);

        if (photonView.owner != null) {
          nameplate.text = photonView.owner.NickName;
        }
      }
    }

    if (usernameOSD != null && nameplate != null && !GameData.loading) {
      usernameOSD.text = nameplate.text;
    }

    // Cinematics
    if (cinematic != null && !cinematic.isDone) {
      return;
    } else if (cinematic != null) {
      cinematicsFinished = true;
    }

    // Death
    if (isDead) {
      if (Time.realtimeSinceStartup - deathTime >= 8f) {
        // if (GameData.health > 0) {
        UnDead();
        // } else {
        //   if (GameData.tries > 0) {
        //     UnDead();
        //   } else {
        //     GameData.MainMenu();
        //   }
        // }
      } else {
        Time.timeScale = Mathf.Lerp(
            0.1f, 0.5f, (Time.realtimeSinceStartup - deathTime) / 8f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
      }
    }

    // Inputs and Player Controls
    float interact = 0f;
    float sprintInput = 0f;
    bool wasUnderwater = false;
    RaycastHit hitinfo = new RaycastHit();
    isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.05f,
                                 Vector3.down, out hitinfo, 0.2f);

    if (isLocalPlayer && !GameData.isPaused && !GameData.isChatOpen) {
      moveHorizontal = Input.GetAxis ("Horizontal");
      moveVertical = Input.GetAxis ("Vertical");
      interact = Input.GetAxis ("Interact");

      if (Input.GetButtonDown ("GodMode")) { godMode = !godMode; }

      isCrouched = Input.GetAxis ("Crouch") > 0.1;
      isJumping = Input.GetAxis ("Jump") > 0.1 && isGrounded && !isCrouched &&
                  lastJumpTime >= jumpFrequency;
      sprintInput = Input.GetAxis ("Sprint");
      isSprinting =
        (sprintInput > 0.1 && !isCrouched) || (isSprinting && !isGrounded);
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

      if (lastFloorTransform != null) {
        lastFloorTransformPosition = lastFloorTransform.position;
      }
    }

    if (isGrounded && hitinfo.transform != null) {
      transform.position +=
          hitinfo.transform.position - lastFloorTransformPosition;
      lastFloorTransform = hitinfo.transform;
      lastFloorTransformPosition = lastFloorTransform.position;
    }


    if (!TerrainGenerator.doneLoadingSpawn && !spawned && waitForSpawnLoading) {
      levelStartTime = Time.time;
      cam.userInput = false;
      if (cam.Initialized())
        cam.cam.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
      cameraSpawnRotation = cam.cam.transform.rotation;
      spawnLocation = transform.position;
    } else if (cinematicsFinished && !spawned) {
      levelStartTime = Time.time;
      cam.userInput = false;
      cameraSpawnRotation = cam.cam.transform.rotation;
      spawnLocation = transform.position;
      spawned = true;
    } else {
      spawned = true;
      cam.userInput = true;
    }

    // Vehicles
    timeInVehicle += Time.deltaTime;

    if (GameData.Vehicle != null) {
      // TODO: Remove this and let VehicleController get its own inputs.
      GameData.Vehicle.UpdateInputs(moveVertical, moveHorizontal,
                                    0 /* lookVertical */,
                                    0 /* lookHorizontal */, sprintInput,
                                    cam.cam.GetComponent<Camera>());
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
      // lookHorizontal = 0;
      // lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = true;
      isGrounded = true;
      isJumping = false;

      if (timeInVehicle < 1.0f) {
        GameData.Vehicle.UpdateInputs(moveVertical, moveHorizontal,
                                      0 /* lookHorizontal */,
                                      0 /* lookVertical */,
                                      sprintInput,
                                      cam.cam.GetComponent<Camera>());
      }

      if (interact > 0.5 && timeInVehicle > 0.5f) {
        ExitVehicle();
      }
    } else {
      RaycastHit raycast;
      Physics.Raycast(cam.cam.transform.position,
                      cam.cam.transform.rotation * Vector3.forward,
                      out raycast, 10f);

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
      if (Input.GetKeyDown("enter") && !GameData.isPaused &&
          !GameData.isChatOpen && !isDead) {
        levelStartTime = Time.time - flyDownTime;
        cam.cam.transform.rotation = startCameraRotation;
      }

      if (GameData.isPaused) {
        levelStartTime += Time.deltaTime;
      }

      if (cameraSpawnRotation.w == 0) {
        cam.userInput = false;
        cam.cam.transform.rotation = Quaternion.Euler (70f, 30f, 0f);
        cameraSpawnRotation = cam.cam.transform.rotation;
      }

      if (!GameData.isPaused && Time.time - levelStartTime < flyDownTime) {
        cam.firstPerson = false;
        cam.userInput = false;
        cam.distanceSnap = true;
        cam.cam.transform.rotation =
          Quaternion.Lerp (cameraSpawnRotation, startCameraRotation,
                           (Time.time - levelStartTime) / flyDownTime);
        cam.MaxCameraDistance =
            Mathf.Lerp(spawnCameraDistance, startCameraDistance,
                       (Time.time - levelStartTime) / flyDownTime);
      }

      moveHorizontal = 0;
      moveVertical = 0;
      // lookHorizontal = 0;
      // lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = false;
      isJumping = false;
    } else {
      if (Time.time - levelStartTime <
          flyDownTime + flyDownEndTime + Time.deltaTime &&
          Time.time - levelStartTime > flyDownTime + flyDownEndTime) {
        moveVertical = 1.0f;
        cam.firstPerson = camFirstPerson;
        cam.MaxCameraDistance = startCameraDistance;
        cam.distanceSnap = camDistanceSnap;

        if (cam.firstPerson && isLocalPlayer) {
          foreach (SkinnedMeshRenderer r in meshRenderers) {
            r.shadowCastingMode =
              UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
          }
        } else if (!isLocalPlayer && meshRenderers != null) {
          foreach (SkinnedMeshRenderer r in meshRenderers) {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
          }
        }
      }
    }

    // Collider
    if (isCrouched && collider_ != null) {
      collider_.height = crouchedHeight;
      if (colliderStartPosition != 0) {
        Vector3 temp = collider_.center;
        temp.y = colliderStartPosition - (playerHeight - crouchedHeight) / 2f;
        collider_.center = temp;
      }
    } else if (collider_ != null) {
      collider_.height = playerHeight;
      if (colliderStartPosition != 0) {
        Vector3 temp = collider_.center;
        temp.y = colliderStartPosition;
        collider_.center = temp;
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
               (moveHorizontal != 0 || moveVertical != 0)) {
      staminaRemaining -= staminaDepletionRate * Time.deltaTime;
    }

    lastSprintInput = sprintInput;

    if (GameData.Vehicle != null) return;

    // Movement
    Vector3 movement =
        moveHorizontal * Vector3.right + moveVertical * Vector3.forward;
    movement = Vector3.ClampMagnitude(movement, 1.0f);

    if (isCrouched) {
      forward = movement.magnitude;
      movement *= moveSpeed * 0.6f;
    } else {
      forward = movement.magnitude / Mathf.Lerp(2.0f, 1.0f, sprintInput);
      movement *= moveSpeed * Mathf.Lerp(1.0f, 2.5f, sprintInput);
    }

    if (godMode) {
      movement += Input.GetAxis("Jump") * Vector3.up;
      movement -= Input.GetAxis("Crouch") * Vector3.up;
      movement *= 30f;
    } else {
      if (isUnderwater) {
        if (transform.position.y < TerrainGenerator.waterHeight - 0.8f) {
          movement +=
              Mathf.Clamp(
                  rbody.velocity.y +
                      2.0f * Time.deltaTime *
                          (TerrainGenerator.waterHeight - transform.position.y),
                  -moveSpeed *
                      ((TerrainGenerator.waterHeight - transform.position.y) /
                       2.5f),
                  moveSpeed *
                      ((TerrainGenerator.waterHeight - transform.position.y) /
                       2.5f)) *
              Vector3.up;
        } else {
          movement +=
              (rbody.velocity.y - 9.81f * 3f * Time.deltaTime) * Vector3.up;
        }
      } else {
        rbody.velocity = new Vector3(
            rbody.velocity.x,
            ((isJumping ? (moveSpeed * jumpMultiplier)
                        : (rbody.velocity.y - 9.81f * 4f * Time.deltaTime))),
            rbody.velocity.z);
        movement += rbody.velocity.y * Vector3.up;
      }
    }

    movement =
        Quaternion.Euler(0, cam.cam.transform.eulerAngles.y, 0) * movement;

    if (movement.magnitude <= 0.015) {
      movement = Vector3.zero;
    }

    rbody.velocity = movement;

    // Camera
    if (isLocalPlayer) {
      if (MiniMapCamera != null) {
        Vector3 mapPos = transform.position + miniMapRelativePosition;
        if (!miniMapRelativeY) mapPos.y = miniMapRelativePosition.y;
        MiniMapCamera.transform.position = mapPos;
      }

      cam.UpdateTransform(Time.deltaTime);

      if (isDead && Ragdoll != null) {
        Transform[] children = Ragdoll.GetComponentsInChildren<Transform>();
        Transform target = transform;
        target.position += Vector3.up *
                           (isCrouched ? crouchedHeight : playerHeight);

        foreach (Transform target_ in children) {
          if (target_.name.Contains("Head_M")) {
            target = target_;
            break;
          }
        }

        cam.UpdateTarget(target);
      }

      // Rotation
      if (cam.rotateWithCamera) {
        rbody.transform.rotation = Quaternion.identity;
        transform.rotation =
            Quaternion.Euler(0, cam.cam.transform.eulerAngles.y, 0);
      } else {
        moveAngle = Mathf.Atan(moveHorizontal / moveVertical) * 180f / Mathf.PI;

        if (!(moveAngle >= 0 || moveAngle < 0)) {
          moveAngle = 0f;
        }

        if (moveVertical < 0) {
          moveAngle += 180f;
        }

        if (Mathf.Abs(rbody.velocity.x) > 0.01f ||
            Mathf.Abs(rbody.velocity.z) > 0.01f) {
          moveAngle += cam.cam.transform.eulerAngles.y;
          Quaternion rotation = Quaternion.Euler(
              0f,
              Mathf.LerpAngle(rbody.transform.eulerAngles.y, moveAngle, 0.10f),
              0f);
          rbody.transform.rotation = Quaternion.identity;
          transform.rotation = rotation;
        }
      }
    }

    // Sound
    if (isGrounded && Time.time - lastGroundedTime >= 0.10f &&
        sounds.Player != null) {
      PlaySound(sounds.LandSound);
    }

    if (isJumping && Time.time - lastJumpSoundTimejump >= 0.4f &&
        sounds.Player != null) {
      PlaySound(sounds.JumpSound);
      lastJumpSoundTimejump = Time.time;
    }

    if (isGrounded && (moveVertical != 0 || moveHorizontal != 0)) {
      if ((isSprinting &&
           Time.time - lastFootstepTime >= footstepSizeSprinting) ||
          (isCrouched &&
           Time.time - lastFootstepTime >= footstepSizeCrouched) ||
          (!isSprinting && !isCrouched &&
           Time.time - lastFootstepTime >= footstepSize)) {
        lastFootstepTime = Time.time;

        float volume = 0.5f;
        if (isCrouched) {
          volume = 0.2f;
        } else if (isSprinting) {
          volume = 0.8f;
        }

        if (sounds.FootSteps.Length > 0) {
          PlaySound(sounds.FootSteps[Random.Range(0, sounds.FootSteps.Length)],
                    volume);
        }
      }
    }

    if (isGrounded) lastGroundedTime = Time.time;
  }

  void ToggleThirdPerson() {
    cam.ToggleThirdPerson();

    if (cam.firstPerson) {
      foreach (SkinnedMeshRenderer r in meshRenderers) {
        r.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
      }
    } else {
      foreach (SkinnedMeshRenderer r in meshRenderers) {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
      }
    }
  }

  void Dead() {
    if (isDead) { return; }

    isDead = true;
    GameData.tries--;
    ExitVehicle();

    if (GameData.tries <= 0) {
      PlaySound(sounds.LevelFail, 1.0f);
    } else {
      PlaySound(sounds.Pain, 1.0f);
    }

    cam.MaxCameraDistance += 10f;
    deathTime = Time.realtimeSinceStartup;
    rbody.isKinematic = true;

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
                   GetComponentsInChildren<SkinnedMeshRenderer>()) {
        renderer.enabled = false;
      }

      collider_.enabled = false;
      anim.enabled = false;
      // cam.cam.transform.eulerAngles = new Vector3(85f, 0, 0);
    }
  }
  void UnDead() {
    transform.position = spawnLocation;
    transform.rotation = Quaternion.identity;
    cam.cam.transform.eulerAngles =
        new Vector3(0, cam.cam.transform.eulerAngles.y, 0);
    spawned = false;
    isDead = false;
    GameData.health = 5;
    Time.timeScale = 1.0f;
    Time.fixedDeltaTime = 0.02f * Time.timeScale;
    cam.MaxCameraDistance -= 10;
    cam.UpdateTarget(Head);
    rbody.isKinematic = false;
    Destroy (Ragdoll);
    collider_.enabled = true;
    anim.enabled = true;
  }

  public void EnterVehicle (VehicleController vehicle) {
    if (vehicle == null) { return; }

    GameData.Vehicle = vehicle;
    timeInVehicle = 0.0f;
    collider_.enabled = false;

    if (isLocalPlayer) {
      foreach (SkinnedMeshRenderer r in meshRenderers) {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
      }
    }
  }
  public void ExitVehicle() {
    if (GameData.Vehicle == null) return;

    timeInVehicle = 0.0f;
    GetComponent<Collider>().enabled = true;
    transform.position = GameData.Vehicle.transform.position + Vector3.up * 2f;
    transform.rotation = Quaternion.Euler(
        0, GameData.Vehicle.transform.rotation.eulerAngles.y, 0);
    rbody.velocity = Vector3.zero;
    cam.cam.transform.rotation = transform.rotation;

    foreach (SkinnedMeshRenderer r in meshRenderers) {
      r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }

    GameData.Vehicle = null;
  }

  void OnAnimatorIK() {
    if (!photonView.isMine && PhotonNetwork.connected) { return; }

    if (anim == null) anim = GetComponent<Animator>();
    if (anim == null) return;

    if (rbody == null) rbody = GetComponent<Rigidbody>();
    if (rbody == null) return;

    if (Mathf.Abs(rbody.velocity.x) > 0.015f ||
        Mathf.Abs(rbody.velocity.z) > 0.015f) {
      turn = (moveAngle - anim.bodyRotation.eulerAngles.y) / 180f;

      while (turn < -1) turn += 2;
      while (turn > 1) turn -= 2;
    } else {
      turn = 0f;
    }

    anim.SetFloat("Forward", forward);
    anim.SetFloat("Turn", turn);

    if (!isUnderwater) {
      anim.SetFloat("Jump", -9 + (Time.time - lastGroundedTime) * 9f);
      anim.SetFloat("JumpLeg", -1 + (Time.time - lastGroundedTime) * 4f);
    } else {
      anim.SetFloat("Jump", Mathf.Abs((Time.time * 200f % 200f - 100) / 200f));
      anim.SetFloat("JumpLeg",
                    Mathf.Abs((Time.time * 200f % 400f - 200) / 400f));
    }

    anim.SetBool("OnGround", isCrouched || godMode ||
                                 (Time.time - lastGroundedTime <= 0.07f));
    anim.SetBool("Crouch", isCrouched);
  }

  void OnTriggerEnter (Collider other) {
    if (other.gameObject.CompareTag("Collectible")) {
      Destroy(other.gameObject);
      GameData.collectedCollectibles += 10;
      PlaySound(sounds.CollectibleSound);
    } else if (other.gameObject.CompareTag("Explosion") ||
               other.gameObject.CompareTag("Enemy")) {
      GameData.health = 0;
      Dead();
    } else if (other.gameObject.CompareTag("EnemyProjectile")) {
      Destroy(other.gameObject);
      GameData.health--;
      Dead();
    }
  }

  void PlaySound(AudioClip clip, float volume = -1f) {
    if (sounds.Player != null && clip != null && GameData.soundEffects) {
      AudioPlayer player = Instantiate(sounds.Player, transform.position,
                                       Quaternion.identity) as AudioPlayer;
      player.clip = clip;

      if (volume >= 0f && volume <= 1f) {
        player.volume = volume;
      }
    }
  }
  void onDestroy() {
    GameData.showCursor = true;
  }
}
