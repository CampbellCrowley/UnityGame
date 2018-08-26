// Copyright (c) Campbell Crowley. Portions from Unity Standard Assets.
// Author: Campbell Crowley (github@campbellcrowley.com)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;

[RequireComponent(typeof (MyCameraController))]
public class PlayerController : Photon.MonoBehaviour {
  [System.Serializable]
  public class Sounds {
    public AudioPlayer Player;
    public AudioClip JumpSound;
    public AudioClip LandSound;
    public AudioClip CollectibleSound;
    public AudioClip Pain;
    public AudioClip LevelFail;
    public AudioClip[] FootSteps;
  }
  [System.Serializable]
  public class MovementSettings {
    public float ForwardSpeed = 8.0f;  // Speed when walking forward
    public float BackwardSpeed = 4.0f;  // Speed when walking backwards
    public float StrafeSpeed = 4.0f;  // Speed when walking sideways
    public float RunMultiplier = 2.0f;  // Speed when sprinting
    public float JumpForce = 30f;
    public AnimationCurve SlopeCurveModifier = new AnimationCurve(
         new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f),
         new Keyframe(90.0f, 0.0f));
    [HideInInspector] public float CurrentTargetSpeed = 8f;

    private bool isSprinting;
    [HideInInspector] public bool godMode = false;

    public void UpdateDesiredTargetSpeed(Vector2 input) {
      // if (animator == null) animator = GetComponent<Animator>();
      if (input == Vector2.zero) return;
      if (input.x > 0 || input.x < 0) {
        // strafe
        CurrentTargetSpeed = StrafeSpeed;
      }
      if (input.y < 0) {
        // backwards
        CurrentTargetSpeed = BackwardSpeed;
      }
      if (input.y > 0) {
        // forwards
        // handled last as if strafing and moving forward at the same time
        // forwards speed should take precedence
        CurrentTargetSpeed = ForwardSpeed;
      }
      if (Input.GetAxis("Sprint") > 0.1) {
        CurrentTargetSpeed *= RunMultiplier;
        isSprinting = true;
      } else {
        isSprinting = false;
      }
    }
    public bool Running {
      get { return isSprinting; }
    }
  }
  [System.Serializable]
  public class AdvancedSettings {
    // distance for checking if the controller is grounded
    public float groundCheckDistance = 0.01f;
    // stops the character
    public float stickToGroundHelperDistance = 0.5f;
    // can the user control the direction that is being moved in the air
    public bool airControl;
    public float shellOffset;
  }

  public MovementSettings movementSettings = new MovementSettings();
  public AdvancedSettings advancedSettings = new AdvancedSettings();

  [Header ("OSDs/HUD")]
  [Tooltip ("UsernameOSD")]
  public GUIText usernameOSD;
  public GUIText debug;
  [Header ("Look and Sound")]
  public bool useRenderSettingsFog = false;
  public float footstepSize = 0.5f;
  public float footstepSizeCrouched = 0.7f;
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
  CapsuleCollider collider_;
  Cinematic cinematic;
  GameObject Ragdoll;
  MyCameraController cam;
  Quaternion startCameraRotation, cameraSpawnRotation;
  Rigidbody rbody;
  SkinnedMeshRenderer[] meshRenderers;
  TextMesh nameplate;
  Transform lastFloorTransform;
  Transform Head;
  Vector3 groundContactNormal;
  Vector3 spawnLocation;
  Vector3 lastFloorTransformPosition;
  bool camDistanceSnap = false;
  bool camFirstPerson = true;
  bool cinematicsFinished = false;
  bool isCrouched = false;
  bool isGrounded = false;
  // bool isUnderwater = false;
  bool jump = false;
  bool jumping = false;
  bool wasGrounded = false;
  float moveHorizontal = 0f;
  float moveVertical = 0f;
  float spawnCameraDistance = 1500f;
  float startCameraDistance = 3f;
  float moveAngle = 0f;
  float levelStartTime = 0f;
  // float lastGroundedTime = 0f;
  float timeInVehicle = 0.0f;
  float deathTime = 0.0f;

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
  }

  void Update() {
    isLocalPlayer = photonView.isMine || !PhotonNetwork.connected;

    if (cam == null) cam = GetComponent<MyCameraController>();
    // if (cam != null && cam.cam != null) {
    //   if (!GameData.loading && !cam.cam.activeSelf) {
    //     cam.cam.SetActive(true);
    //   } else if (GameData.loading && cam.cam.activeSelf) {
    //     cam.cam.SetActive(false);
    //   }
    // }

    rbody = GetComponent<Rigidbody>();

    if (!GameData.loading && UnityEngine.Camera.main != null) {
      if (nameplate == null) nameplate = GetComponentInChildren<TextMesh>();
      if (nameplate != null) {
        // nameplate.transform.LookAt(UnityEngine.Camera.main.transform.position);
        // nameplate.transform.rotation *= Quaternion.Euler(0, 180f, 0);
        nameplate.transform.rotation =
            UnityEngine.Camera.main.transform.rotation;

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
        UnDead();
      } else {
        Time.timeScale = Mathf.Lerp(
            0.1f, 0.5f, (Time.realtimeSinceStartup - deathTime) / 8f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
      }
    }

    // Inputs and Player Controls
    float interact = 0f;

    if (isLocalPlayer && !GameData.isPaused && !GameData.isChatOpen) {
      interact = Input.GetAxis ("Interact");

      if (Input.GetButtonDown("GodMode")) {
        movementSettings.godMode = !movementSettings.godMode;
      }

      isCrouched = Input.GetAxis ("Crouch") > 0.1;

      if (Input.GetButtonDown ("Toggle Third Person")) { ToggleThirdPerson(); }
      if (Input.GetButtonDown("Jump") && !jump) { jump = true; }
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
      transform.position =
        GameData.Vehicle.gameObject.transform.position + Vector3.up * 0.25f;
      transform.rotation = GameData.Vehicle.gameObject.transform.rotation;

      if (GameData.Vehicle.fuelRemaining < 100) {
        usernameOSD.text +=
          "\n" + (GameData.Vehicle.isBoat
                  ? "Boat"
                  : "Car" + " has " +
                  Mathf.Round (GameData.Vehicle.fuelRemaining) +
                  " Fuel Remaining");
      }

      rbody.velocity = Vector3.zero;

      if (timeInVehicle < 1.0f) {
        GameData.Vehicle.UpdateInputs(
            moveVertical, moveHorizontal, 0 /* lookHorizontal */,
            0 /* lookVertical */, 0f, cam.cam.GetComponent<Camera>());
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

      rbody.velocity = Vector3.zero;
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

    if (GameData.Vehicle != null) return;

    // Camera
    if (isLocalPlayer) {
      cam.UpdateTransform(Time.deltaTime);

      if (isDead && Ragdoll != null) {
        Transform[] children = Ragdoll.GetComponentsInChildren<Transform>();
        Transform target = transform;
        target.position += Vector3.up * 2.0f;

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
  }

  void FixedUpdate() {
    if (GameData.Vehicle != null) return;
    if (collider_ == null) collider_ = GetComponent<CapsuleCollider>();
    if (rbody == null) rbody = GetComponent<Rigidbody>();
    GroundCheck();
    Vector3 input = GetInput();

    if (movementSettings.godMode) {
      Vector3 desiredMove = cam.cam.transform.forward * input.y +
                            cam.transform.right * input.x;
      desiredMove =
          Vector3.ProjectOnPlane(desiredMove, groundContactNormal).normalized;
      desiredMove.y = input.z;
      desiredMove *= 30f * movementSettings.CurrentTargetSpeed;

      rbody.velocity = desiredMove;
    } else if ((Mathf.Abs(input.x) > float.Epsilon ||
                Mathf.Abs(input.y) > float.Epsilon) &&
               (advancedSettings.airControl || isGrounded)) {
      Vector3 desiredMove =
          cam.cam.transform.forward * input.y + cam.transform.right * input.x;
      desiredMove =
          Vector3.ProjectOnPlane(desiredMove, groundContactNormal).normalized;

      desiredMove.x = desiredMove.x * movementSettings.CurrentTargetSpeed;
      desiredMove.z = desiredMove.z * movementSettings.CurrentTargetSpeed;
      desiredMove.y = desiredMove.y * movementSettings.CurrentTargetSpeed;
      if (rbody.velocity.sqrMagnitude < (movementSettings.CurrentTargetSpeed *
                                         movementSettings.CurrentTargetSpeed)) {
        rbody.AddForce(desiredMove * SlopeMultiplier() * 7f, ForceMode.Impulse);
      }
    }

    if (anim == null) GetComponent<Animator>();
    if (isGrounded) {
      // rbody.drag = 5f;

      if (anim != null) {
        anim.SetInteger("Jumping", 0);
        anim.SetBool("Crouched", false);
      }
      if (jump) {
        // rbody.drag = 0.0f;
        rbody.velocity =
            new Vector3(rbody.velocity.x, 0f, rbody.velocity.z);
        rbody.AddForce(
            new Vector3(0f, movementSettings.JumpForce * rbody.mass, 0f),
            ForceMode.Impulse);
        if (anim != null) {
          anim.SetInteger("Jumping", 1);
          anim.SetTrigger("JumpTrigger");
        }
        jumping = true;
      }

      if (!jumping &&
          Mathf.Abs(input.x) < float.Epsilon&& Mathf.Abs(input.y) <
              float.Epsilon&& rbody.velocity.magnitude < 0.5f) {
        rbody.Sleep();
      }
    } else {
      // rbody.drag = 0.0f;
      if (wasGrounded && !jumping) {
        StickToGroundHelper();
        if (anim != null) {
          anim.SetInteger("Jumping", 0);
          anim.SetBool("Crouched", isCrouched);
        }
      } else if (rbody.velocity.y < 0) {
        if (anim != null) {
          anim.SetInteger("Jumping", 2);
        }
      }
    }

    Vector3 inverseTransform =
        transform.InverseTransformDirection(rbody.velocity);
    if (debug != null) debug.text = inverseTransform + "";

    if (anim != null) {
      anim.SetBool("Moving", rbody.velocity != Vector3.zero);
      // anim.SetBool("Strafing",
      //              Mathf.Abs(inverseTransform.x) >
      //              Mathf.Abs(inverseTransform.z));
      anim.SetFloat("Velocity X",
                    inverseTransform.x / (movementSettings.RunMultiplier *
                                          movementSettings.StrafeSpeed));
      anim.SetFloat("Velocity Z",
                    inverseTransform.z / (movementSettings.RunMultiplier *
                                          movementSettings.ForwardSpeed));
    }

    jump = false;
  }

  Vector3 GetInput() {
    Vector3 input =
        new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
    input.z = Input.GetAxis("Jump");
    if (input.z <= 0) input.z = -Input.GetAxis("Crouch");
    movementSettings.UpdateDesiredTargetSpeed(input);
    return input;
  }

  float SlopeMultiplier() {
    float angle = Vector3.Angle(groundContactNormal, Vector3.up);
    return movementSettings.SlopeCurveModifier.Evaluate(angle);
  }

  void StickToGroundHelper() {
    RaycastHit hitInfo;
    if (Physics.SphereCast(
            transform.position,
            collider_.radius * (1.0f - advancedSettings.shellOffset),
            Vector3.down, out hitInfo,
            ((collider_.height / 2f) - collider_.radius) +
                advancedSettings.stickToGroundHelperDistance,
            Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
      if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f) {
        rbody.velocity = Vector3.ProjectOnPlane(rbody.velocity, hitInfo.normal);
      }
    }
  }
  void GroundCheck() {
    wasGrounded = isGrounded;
    RaycastHit hitInfo;
    if (Physics.SphereCast(
            transform.position + (Vector3.down * advancedSettings.shellOffset),
            collider_.radius * (1.0f - advancedSettings.shellOffset),
            Vector3.down, out hitInfo,
            ((collider_.height / 2f) - collider_.radius) +
                advancedSettings.groundCheckDistance,
            Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
      isGrounded = true;
      groundContactNormal = hitInfo.normal;
      // lastGroundedTime = Time.time;
    } else {
      isGrounded = false;
      groundContactNormal = Vector3.up;
    }
    if (!wasGrounded && isGrounded && jumping) {
      jumping = false;
    }
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
    anim.enabled = true;
  }

  public void EnterVehicle (VehicleController vehicle) {
    if (vehicle == null) { return; }

    GameData.Vehicle = vehicle;
    timeInVehicle = 0.0f;

    if (isLocalPlayer) {
      foreach (SkinnedMeshRenderer r in meshRenderers) {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
      }
    }
  }
  public void ExitVehicle() {
    if (GameData.Vehicle == null) return;

    timeInVehicle = 0.0f;
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

  // Animation Events
  void Hit() {}

  void FootL() {}

  void FootR() {}

  void Jump() {}

  void Land() {}

  void Rolling() {
    /*if (!isRolling && isGrounded) {
      if (Input.GetAxis("Dash") > .5 || Input.GetAxis("Dash") < -.5) {
        StartCoroutine(_DirectionalRoll(
            Input.GetAxis("Dash") * Input.GetAxis("Vertical"),
            Input.GetAxis("Dash") * Input.GetAxis("Vertical")));
      }
    }*/
  }
}
