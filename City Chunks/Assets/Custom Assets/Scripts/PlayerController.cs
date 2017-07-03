using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.Networking;
#pragma warning disable 0168

[NetworkSettings(sendInterval=0.01f)]
public
class PlayerController : NetworkBehaviour {
  [System.Serializable] public class Sounds {
   public
    AudioPlayer Player;
   public
    AudioClip JumpSound;
   public
    AudioClip LandSound;
   public
    AudioClip CollectibleSound;
   public
    AudioClip Pain;
   public
    AudioClip LevelFail;
   public
    AudioClip[] FootSteps;
  }

  [Header("Movement")]
 public
  float moveSpeed = 5f;
 public
  float jumpMultiplier = 5f;
 public
  float jumpFrequency = 0.25f;
 public
  float staminaDepletionRate = 0.1f;  // Percent per second
 public
  float staminaRechargeDelay = 3.0f;  // Seconds
 public
  float staminaRechargeMultiplier = 1.5f;
  [Header("Camera")]
 public
  GameObject Camera;
 public
  bool CameraObjectAvoidance = true;
 public
  bool rotateWithCamera = false;
 public
  float MaxCameraDistance = 3f;
 public
  float playerHeight = 1.5f;
 public
  float crouchedHeight = 1.0f;
  [Header("OSDs/HUD")]
 public
  GUIText collectedCounter;
  [Tooltip("LifeOSD")] public GUIText lifeCounter;
 public
  GUIText timer;
 public
  GUIText stamina;
 [Tooltip("LevelOSD")]
 public
  GUIText levelDisplay;
 [Tooltip("UsernameOSD")]
 public
  GUIText usernameOSD;
 public
  float staminaCountBars = 20f;
 public
  GUIText debug;
  [Header("MiniMap")]
 public
  Camera MiniMapCamera;
 public
  Vector3 miniMapRelativePosition;
  [Header("Look and Sound")]
 public
  bool useRenderSettingsFog = true;
 public
  float footstepSize = 0.5f;
 public
  float footstepSizeCrouched = 0.7f;
 public
  float footstepSizeSprinting = 0.3f;
 public
  Sounds sounds;
  [Header("Misc.")]
 public
  float sendFrequency = 0.01f;
 public
  float GameTime = 10f;
 public
  GameObject RagdollTemplate;
 private
  GameObject Ragdoll;
 public
  bool isDead = false;
 public
  bool spawned = false;
 public
  float flyDownTime = 6.0f;
 public
  float flyDownEndTime = 1.5f;

 private
  Cinematic cinematic;
 private
  Rigidbody rbody;
 private
  Animator anim;
 private
  TextMesh nameplate;
 private
  Color startColor;
 private
  Quaternion startCameraRotation, cameraSpawnRotation;
 private
  float spawnCameraDistance = 1500f;
 private
  float intendedCameraDistance;
 private
  float turn = 0f;
 private
  float forward = 0f;
 private
  float moveAngle = 0f;
 private
  float CurrentCameraDistance = 3f;
 private
  bool isGrounded = true;
 private
  bool isCrouched = false;
 private
  bool isSprinting = false;
 private
  bool isUnderwater = false;
 private
  bool godMode = false;
 private
  float endTime = 0f;
 private
  Vector3 spawnLocation;
 private
  float staminaRemaining = 1.0f;
 private
  float levelStartTime = 0f;
 private
  float lastGroundedTime = 0f;
 private
  float lastSprintTime = 0f;
 private
  float lastJumpSoundTimejump = 0.0f;
 private
  float lastJumpTime = 0.0f;
 private
  float lastFootstepTime = 0.0f;
 private
  float lastSendTime = 0.0f;
 private
  float lastVignetteAmount = 0.0f;
 private
  float lastSprintInput = 0.0f;
 private
  float timeInVehicle = 0.0f;
 private
  float deathTime = 0.0f;
 private
  Transform lastFloorTransform;
 private
  Vector3 lastFloorTransformPosition;
 private
  bool cinematicsFinished = false;

  [SyncVar] public string username = "Username";
  [SyncVar] private Vector3 rbodyPosition, rbodyVelocity, transformPosition;
  [SyncVar] private Quaternion rbodyRotation, transformRotation;
  [Command] public void CmdChangeName(string name) { username = name; }
  [Command] public void CmdUpdatePlayer(Vector3 rposition, Vector3 rvelocity,
                                        Quaternion rrotation, Vector3 tposition,
                                        Quaternion trotation) {
    rbodyPosition = rposition;
    rbodyVelocity = rvelocity;
    rbodyRotation = rrotation;
    transformPosition = tposition;
    transformRotation = trotation;
  }


 public
  override void OnStartLocalPlayer() {
    spawned = false;
    if (sounds.LandSound != null) sounds.LandSound.LoadAudioData();
    if (sounds.JumpSound != null) sounds.JumpSound.LoadAudioData();
    foreach (AudioClip step in sounds.FootSteps) {
      if (step != null) step.LoadAudioData();
    }
    GameData.showCursor = false;

    cinematic = FindObjectOfType<Cinematic>();
    anim = GetComponent<Animator>();
    rbody = GetComponent<Rigidbody>();

    startColor = RenderSettings.fogColor;

    Camera = Instantiate(Camera);
    Camera.transform.parent = null;
    Camera.GetComponent<Camera>().enabled = true;
    Camera.GetComponent<AudioListener>().enabled = true;
    Camera.name = "CameraFor" + netId;
    foreach (Camera cam in UnityEngine.Camera.allCameras) {
      cam.layerCullSpherical = true;
    }
    startCameraRotation = Camera.transform.rotation;
    intendedCameraDistance = MaxCameraDistance;

    if (MiniMapCamera != null) MiniMapCamera = Instantiate(MiniMapCamera);

    GetComponent<MeshRenderer>().material.color = Color.blue;

    if (GameData.username.ToLower() == "username" || GameData.username == "") {
      GameData.username = NameList.GetName();
    }
    Debug.Log("Send Freqency: " + sendFrequency);
    CmdChangeName(GameData.username);
    CmdUpdatePlayer(rbody.position, rbody.velocity, rbody.rotation,
                    transform.position, transform.rotation);

    GameObject temp;
    if (lifeCounter != null) {
      lifeCounter = Instantiate(lifeCounter);
    } else {
      temp = GameObject.Find("LifeOSD");
      if (temp != null) lifeCounter = temp.GetComponent<GUIText>();
    }
    if (levelDisplay != null) {
      levelDisplay = Instantiate(levelDisplay);
    } else {
      temp = GameObject.Find("LevelOSD");
      if (temp != null) levelDisplay = temp.GetComponent<GUIText>();
    }
    if (usernameOSD != null) {
      usernameOSD = Instantiate(usernameOSD);
    } else {
      temp = GameObject.Find("UsernameOSD");
      if (temp != null) usernameOSD = temp.GetComponent<GUIText>();
    }
    if (debug != null) debug = Instantiate(debug);

    levelStartTime = Time.time;
    lastGroundedTime = Time.time;
    lastSprintTime = Time.time;
    lastJumpSoundTimejump = Time.time;
    lastJumpTime = jumpFrequency;
    lastFootstepTime = Time.time;
    lastSendTime = Time.realtimeSinceStartup;

    TerrainGenerator.AddPlayer(GetComponent<InitPlayer>());
  }

  void Update() {
    if (!GameData.loading && !Camera.activeSelf) Camera.SetActive(true);
    else if (GameData.loading && Camera.activeSelf) Camera.SetActive(false);
    rbody = GetComponent<Rigidbody>();
    if (!GameData.loading) {
      nameplate = GetComponentInChildren<TextMesh>();
      nameplate.transform.LookAt(UnityEngine.Camera.main.transform.position);
      nameplate.transform.rotation *= Quaternion.Euler(0, 180f, 0);
      if (username != "Username") {
        nameplate.text = username;
      } else {
        nameplate.text = "Player " + (int.Parse(netId.ToString()) - 1);
      }
    }
    if (!isLocalPlayer) {
      if (!GameData.loading)
        nameplate.GetComponent<MeshRenderer>().enabled = true;
      if (rbodyRotation.x * rbodyRotation.x +
              rbodyRotation.y * rbodyRotation.y +
              rbodyRotation.z * rbodyRotation.z +
              rbodyRotation.w * rbodyRotation.w !=
          0) {
        rbody.position = rbodyPosition;
        rbody.velocity = rbodyVelocity;
        rbody.rotation = rbodyRotation;
        transform.position = transformPosition;
        transform.rotation = transformRotation;
        forward = rbody.velocity.magnitude / moveSpeed;
      }
      return;
    } else if (intendedCameraDistance == 0 &&
               Time.time - levelStartTime > flyDownTime + flyDownEndTime) {
      if (!GameData.loading)
        nameplate.GetComponent<MeshRenderer>().enabled = false;
    }

    if (usernameOSD != null && !GameData.loading)
      usernameOSD.text = nameplate.text;

    // Cinematics
    if (cinematic != null && !cinematic.isDone) {
      return;
    } else if (cinematic != null) {
      cinematicsFinished = true;
    }

    if (Input.GetKeyDown("k")) {
      GameData.health = 0;
      GameData.tries = 0;
      Dead();
    }
    if (isDead) {
      if (Time.realtimeSinceStartup - deathTime >= 8f) {
        if (GameData.health > 0) {
          UnDead();
          // GameData.restartLevel();
        } else {
          if (GameData.tries > 0) {
            UnDead();
            // GameData.restartLevel();
          } else {
            GameData.MainMenu();
          }
        }
      } else {
        Time.timeScale = Mathf.Lerp(
            // 0.05f, 0.1f, (Time.realtimeSinceStartup - deathTime) / 3f);
            0.1f, 0.5f, (Time.realtimeSinceStartup - deathTime) / 8f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
      }
    }

    // Inputs and Player Controls
    float moveHorizontal = Input.GetAxis("Horizontal");
    float moveVertical = Input.GetAxis("Vertical");
    float lookHorizontal = Input.GetAxis("Mouse X");
    float lookVertical = Input.GetAxis("Mouse Y");
    float interact = Input.GetAxis("Interact");
    if(Input.GetButtonDown("GodMode")) godMode = !godMode;
    RaycastHit hitinfo;
    isGrounded =
        Physics.Raycast(transform.position, Vector3.down, out hitinfo, 0.2f);
    isCrouched = Input.GetAxis("Crouch") > 0.5;
    bool jump = Input.GetAxis("Jump") > 0.5 && isGrounded && !isCrouched &&
                lastJumpTime >= jumpFrequency;
    float sprintInput = Input.GetAxis("Sprint");
    isSprinting =
        (sprintInput > 0.5 && !isCrouched) || (isSprinting && !isGrounded);
    bool wasUnderwater = isUnderwater;
    isUnderwater = transform.position.y < TerrainGenerator.waterHeight;

    if (wasUnderwater && !isUnderwater) lastGroundedTime = Time.time;
    if (isUnderwater) isGrounded = false;
    lastJumpTime += Time.deltaTime;
    if (jump) lastJumpTime = 0.0f;

    // Standing on platform
    // This is necessary to ensure the player moves with the platform they are
    // standing on. The position obtained from this is used to offset the
    // player's position.
    if (lastFloorTransform == null ||
        hitinfo.transform != null &&
            hitinfo.transform.name != lastFloorTransform.name) {
      lastFloorTransform = hitinfo.transform;
      if (lastFloorTransform != null) {
        lastFloorTransformPosition = lastFloorTransform.position;
      }
    }
    if (isGrounded) {
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

    if (!TerrainGenerator.doneLoadingSpawn && !spawned) {
      levelStartTime = Time.time;
      Camera.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
      cameraSpawnRotation = Camera.transform.rotation;
      spawnLocation = transform.position;
    } else if (cinematicsFinished && !spawned) {
      levelStartTime = Time.time;
      cameraSpawnRotation = Camera.transform.rotation;
      spawnLocation = transform.position;
      spawned = true;
    } else {
      spawned = true;
    }

    // Vehicles
    timeInVehicle += Time.deltaTime;
    if(GameData.Vehicle!= null && GameData.Vehicle.fuelRemaining < 0) {
      ExitVehicle();
    }
    if (GameData.Vehicle != null) {
      GameData.Vehicle.UpdateInputs(moveVertical, moveHorizontal,
                                    lookHorizontal, lookVertical, sprintInput,
                                    Camera.GetComponent<Camera>());
      transform.position =
          GameData.Vehicle.gameObject.transform.position + Vector3.up * 0.25f;
      transform.rotation = GameData.Vehicle.gameObject.transform.rotation;
      if (MiniMapCamera != null) {
        MiniMapCamera.transform.position =
            transform.position + miniMapRelativePosition;
      }
      if (GameData.Vehicle.fuelRemaining < 100) {
        usernameOSD.text +=
            "\n" + (GameData.Vehicle.isBoat
                        ? "Boat"
                        : "Car" + " has " +
                              Mathf.Round(GameData.Vehicle.fuelRemaining) +
                              " Fuel Remaining");
      }
      moveHorizontal = 0;
      moveVertical = 0;
      lookHorizontal = 0;
      lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = true;
      isGrounded = true;
      jump = false;
      if (timeInVehicle < 1.0f) {
        GameData.Vehicle.UpdateInputs(moveVertical, moveHorizontal,
                                      lookHorizontal, lookVertical, sprintInput,
                                      Camera.GetComponent<Camera>());
      }
      if (interact > 0.5 && timeInVehicle > 0.5f) {
        ExitVehicle();
      }
    } else {
      RaycastHit raycast;
      Physics.Raycast(Camera.transform.position,
                      Camera.transform.rotation * Vector3.forward, out raycast,
                      10f);
      if (raycast.transform != null &&
          raycast.transform.CompareTag("Vehicle")) {
        if (raycast.transform.gameObject.GetComponent<VehicleController>()
                .fuelRemaining > 0) {
          if (interact > 0.5f && timeInVehicle > 0.5f) {
            EnterVehicle(
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
    // paused.
    if (Time.time - levelStartTime < flyDownTime || isDead ||
        GameData.isPaused) {
      if (Input.GetKeyDown("enter")) {
        levelStartTime = Time.time - flyDownTime;
      }
      if(GameData.isPaused) {
        levelStartTime += Time.deltaTime;
      }
      if (cameraSpawnRotation.w == 0) {
        Camera.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
        cameraSpawnRotation = Camera.transform.rotation;
      }
      if (!isDead) {
        Camera.transform.rotation =
            Quaternion.Lerp(cameraSpawnRotation, startCameraRotation,
                            (Time.time - levelStartTime) / flyDownTime);
        MaxCameraDistance =
            Mathf.Lerp(spawnCameraDistance, intendedCameraDistance,
                       (Time.time - levelStartTime) / flyDownTime);
      }
      if(GameData.Vehicle != null) {
        timeInVehicle += Time.deltaTime;
        transform.position = GameData.Vehicle.gameObject.transform.position;
        if (interact > 0.5 && timeInVehicle > 0.5f) GameData.Vehicle = null;
      }
      moveHorizontal = 0;
      moveVertical = 0;
      lookHorizontal = 0;
      lookVertical = 0;
      rbody.velocity = Vector3.zero;
      isCrouched = false;
      jump = false;
    } else {
      if (Time.time - levelStartTime <
              flyDownTime + flyDownEndTime + Time.deltaTime &&
          Time.time - levelStartTime > flyDownTime + flyDownEndTime) {
        moveVertical = 1.0f;
        if (intendedCameraDistance == 0) {
          SkinnedMeshRenderer[] renderers =
              GetComponentsInChildren<SkinnedMeshRenderer>();
          foreach (SkinnedMeshRenderer r in renderers) {
            r.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
          }
        }
      }
      MaxCameraDistance = intendedCameraDistance;

    }

    // Collider
    CapsuleCollider collider = GetComponent<CapsuleCollider>();
    if (isCrouched && collider != null) {
      collider.height = crouchedHeight;
    } else if(collider != null) {
      collider.height = playerHeight;
    }

    // Stamina
    if (isSprinting) lastSprintTime = Time.time;
    if (Time.time - lastSprintTime >= staminaRechargeDelay) {
      staminaRemaining +=
          staminaDepletionRate * Time.deltaTime * staminaRechargeMultiplier;
      if (staminaRemaining > 1.0) staminaRemaining = 1.0f;
    }
    if (staminaRemaining <= 0) {
      isSprinting = false;
      sprintInput = lastSprintInput -= Time.deltaTime;
    } else if (isGrounded && isSprinting &&
               (moveHorizontal != 0 || moveVertical != 0)) {
      staminaRemaining -= staminaDepletionRate * Time.deltaTime;
    }
    lastSprintInput = sprintInput;

    // Start countdown once player moves.
    if (!(moveHorizontal == 0 && moveVertical == 0 && !jump) && endTime == 0f) {
      endTime = Time.time + GameTime;
    }

    if (GameData.health <= 0) {
      Dead();
    }

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
          for (int i = 0; i < (int)(staminaRemaining * staminaCountBars); i++) {
            stamina.text += "|";
          }
          for (int i = (int)(staminaCountBars * staminaRemaining);
               i < staminaCountBars; i++) {
            stamina.text += "!";
          }
        }
      } else if (stamina != null) {
        stamina.text = "";
      }
      if (GameData.Vehicle == null) {
      }
      if (timer != null) {
        float timeRemaining = Mathf.Round((endTime - Time.time) * 10f) / 10f;
        if (endTime == 0f) timeRemaining = GameTime;
        string timeRemaining_ = "";
        if (timeRemaining > 0) {
          timeRemaining_ += timeRemaining;
        } else {
          timeRemaining_ += "0.0";
        }
        if (timeRemaining % 1 == 0 && timeRemaining > 0) timeRemaining_ += ".0";
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
      if (collectedCounter != null) {
        collectedCounter.text = "";
      }
      if (lifeCounter != null) {
        lifeCounter.text = "";
      }
      if (stamina != null) {
        stamina.text = "";
      }
      if (timer != null) {
        endTime += Time.deltaTime;
        timer.text = "";
      }
      if(usernameOSD !=null) {
        usernameOSD.text = "";
      }
    }
    if (levelDisplay != null) {
      levelDisplay.text = "Level: " + GameData.getLevel();
    }

    if (GameData.Vehicle == null) {
      // Movement
      Vector3 movement =
          moveHorizontal * Vector3.right + moveVertical * Vector3.forward;
      movement = Vector3.ClampMagnitude(movement, 1.0f);
      if (isCrouched) {
        forward = movement.magnitude;
        movement *= moveSpeed * 0.5f;
      } else {
        forward = movement.magnitude / Mathf.Lerp(2.5f, 1.0f, sprintInput);
        movement *= moveSpeed * Mathf.Lerp(1.0f, 2.5f, sprintInput);
      }
      if (godMode) {
        movement += Input.GetAxis("Jump") * Vector3.up;
        movement -= Input.GetAxis("Crouch") * Vector3.up;
        movement *= 30f;
      } else {
        if (isUnderwater) {
          if (transform.position.y < TerrainGenerator.waterHeight - 1f) {
            movement +=
                Mathf.Clamp(
                    rbody.velocity.y +
                        2.0f * Time.deltaTime * (TerrainGenerator.waterHeight -
                                                 transform.position.y),
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
              ((jump ? (moveSpeed * jumpMultiplier) : 0.0f) +
               ((/*isGrounded ||*/ jump)
                    ? rbody.velocity.y
                    : (rbody.velocity.y - 9.81f * 4f * Time.deltaTime))),
              rbody.velocity.z);
          movement += rbody.velocity.y * Vector3.up;
        }
      }

      movement =
          Quaternion.Euler(0, Camera.transform.eulerAngles.y, 0) * movement;
      Vector3 acceleration = Vector3.zero;
      rbody.velocity = movement;
      // rbody.velocity =
      //     Vector3.SmoothDamp(rbody.velocity, movement, ref acceleration,
      //     0.01f);

      // Rotation
      if (rotateWithCamera) {
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x, transform.eulerAngles.y + lookHorizontal,
            transform.eulerAngles.z);
      } else {
        moveAngle = Mathf.Atan(moveHorizontal / moveVertical) * 180f / Mathf.PI;
        if (!(moveAngle >= 0 || moveAngle < 0)) {
          moveAngle = 0f;
        }
        if (moveVertical < 0) moveAngle += 180f;
        if (Mathf.Abs(rbody.velocity.x) > 0.01f ||
            Mathf.Abs(rbody.velocity.z) > 0.01f) {
          moveAngle += Camera.transform.eulerAngles.y;
          Quaternion rotation = Quaternion.Euler(
              0f,
              Mathf.LerpAngle(rbody.transform.eulerAngles.y, moveAngle, 0.07f),
              0f);

          rbody.transform.rotation = Quaternion.identity;
          transform.rotation = rotation;
        }
      }

      // Camera
      if (MiniMapCamera != null) {
        MiniMapCamera.transform.position =
            transform.position + miniMapRelativePosition;
      }
      Camera.transform.rotation =
          Quaternion.Euler(Camera.transform.eulerAngles.x - lookVertical,
                           Camera.transform.eulerAngles.y + lookHorizontal, 0);
      if (CameraObjectAvoidance && spawned) {
        RaycastHit hit;
        Physics.Linecast(
            transform.position +
                Vector3.up * (isCrouched ? crouchedHeight : playerHeight),
            Camera.transform.position, out hit, LayerMask.GetMask("Ground"));
        if (hit.transform != Camera.transform && hit.transform != transform &&
            hit.transform != null) {
          CurrentCameraDistance = hit.distance;
        } else {
          CurrentCameraDistance += 1.0f * Time.deltaTime;
          if (CurrentCameraDistance > MaxCameraDistance) {
            CurrentCameraDistance = MaxCameraDistance;
          }
        }
      }
      if (!spawned || isDead) CurrentCameraDistance = MaxCameraDistance;
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
                   Mathf.Sin(Camera.transform.eulerAngles.x / 180f * Mathf.PI)),
              1.0f) *
              CurrentCameraDistance;
      if (!isDead || Ragdoll == null) {
        newCameraPos += transform.position;
      } else {
        newCameraPos += Ragdoll.transform.position;
      }
      if (isDead) {
        Vector3 velocity = Vector3.zero;
        newCameraPos = Vector3.SmoothDamp(Camera.transform.position,
                                          newCameraPos, ref velocity, 0.05f);
      } else if (GameData.cameraDamping && spawned &&
                 (MaxCameraDistance != 0 ||
                  Time.time - levelStartTime < flyDownTime + flyDownEndTime)) {
        Vector3 velocity = Vector3.zero;
        newCameraPos = Vector3.SmoothDamp(Camera.transform.position,
                                          newCameraPos, ref velocity, 0.05f);
      }
      if (newCameraPos.y <= TerrainGenerator.waterHeight + 0.4f) {
        newCameraPos += Vector3.down * 0.8f;
      }

      Camera.transform.position = newCameraPos;

      if (Camera.transform.eulerAngles.x > 75.0f &&
          Camera.transform.eulerAngles.x < 90.0f) {
        Camera.transform.rotation =
            Quaternion.Euler(75.0f, Camera.transform.eulerAngles.y, 0f);
      } else if (Camera.transform.eulerAngles.x < 360f - 75.0f &&
                 Camera.transform.eulerAngles.x > 90.0f) {
        Camera.transform.rotation =
            Quaternion.Euler(-75.0f, Camera.transform.eulerAngles.y, 0f);
      }
      if (isDead && Ragdoll != null) {
        Transform[] children = Ragdoll.GetComponentsInChildren<Transform>();
        Transform target = transform;
        target.position += Vector3.up * 1.2f;
        foreach (Transform target_ in children) {
          if (target_.name.Contains("Head_M")) {
            target = target_;
            break;
          }
        }
        Quaternion startRot = Camera.transform.rotation;
        Camera.transform.LookAt(target.position + Vector3.up * 0.2f);
        Camera.transform.rotation =
            Quaternion.Lerp(startRot, Camera.transform.rotation, 0.1f);
      }

      // VFX
      float vignette = 0.0f;
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      float enemydistance = 100f;
      for (int i = 0; i < enemies.Length; i++) {
        float tempdist =
            (enemies[i].transform.position - transform.position).magnitude;
        if (tempdist < enemydistance) {
          enemydistance = tempdist;
        }
      }
      vignette = 0.45f - (enemydistance / 150f);
      vignette =
          Mathf.Lerp(lastVignetteAmount, vignette, 2.0f * Time.deltaTime);
      lastVignetteAmount = vignette;

      // TODO: Standard Assets VignetteAndChromaticAberration is deprecated.
      // Should try to interface with post processing instead.
      /*try {
        Camera.GetComponent<VignetteAndChromaticAberration>().intensity =
            vignette;
      } catch (System.NullReferenceException e) {
      }*/

      if(useRenderSettingsFog) {
        if (Camera.transform.position.y > TerrainGenerator.waterHeight) {
          RenderSettings.fogStartDistance = 300 * (1 - (vignette / 0.45f));
          RenderSettings.fogEndDistance = 500 * (1 - (vignette / 0.45f));
          RenderSettings.fogColor =
              Color.Lerp(startColor, Color.red, (vignette / 0.45f));
        } else {  // Underwater
          RenderSettings.fogStartDistance =
              Mathf.Lerp(RenderSettings.fogStartDistance, 0f, 0.5f);
          RenderSettings.fogEndDistance =
              Mathf.Lerp(RenderSettings.fogEndDistance, 30f, 0.5f);
          RenderSettings.fogColor = (Color.blue + Color.white) / 2;
        }
      }

      // Sound
      if (isGrounded && Time.time - lastGroundedTime >= 0.1f &&
          sounds.Player != null) {
        PlaySound(sounds.LandSound);
      }
      if (jump && Time.time - lastJumpSoundTimejump >= 0.5f &&
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
          if (sounds.FootSteps.Length > 0) {
            AudioClip footstepSound =
                sounds.FootSteps[(int)Random.Range(0, sounds.FootSteps.Length)];
            PlaySound(footstepSound);
          }
        }
      }
    }

    if (isGrounded) lastGroundedTime = Time.time;
    if (Time.realtimeSinceStartup - lastSendTime > sendFrequency) {
      lastSendTime = Time.realtimeSinceStartup;
      CmdUpdatePlayer(rbody.position, rbody.velocity, rbody.rotation,
                      transform.position, transform.rotation);
    }
  }

  void Dead() {
    if (isDead) return;
    isDead = true;
    GameData.tries--;
    ExitVehicle();
    if (GameData.tries <= 0) {
      PlaySound(sounds.LevelFail, 1.0f);
    } else {
      PlaySound(sounds.Pain, 1.0f);
    }
    MaxCameraDistance += 10f;
    deathTime = Time.realtimeSinceStartup;
    GetComponent<Rigidbody>().isKinematic = true;
    if (RagdollTemplate != null) {
      Ragdoll = Instantiate(RagdollTemplate);
      Ragdoll.SetActive(true);
      Ragdoll.transform.position = transform.position;
      Ragdoll.transform.rotation = transform.rotation;
      foreach (Transform parent in GetComponentsInChildren<Transform>()) {
        foreach (
            Transform ragdoll in Ragdoll.GetComponentsInChildren<Transform>()) {
          if (ragdoll.name.Equals(parent.name)) {
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
    MaxCameraDistance -= 10;
    GetComponent<Rigidbody>().isKinematic = false;
    Destroy(Ragdoll);
    GetComponent<CapsuleCollider>().enabled = true;
    GetComponent<Animator>().enabled = true;
  }

  public void EnterVehicle(VehicleController vehicle) {
     if (vehicle == null) return;
     GameData.Vehicle = vehicle;
     timeInVehicle = 0.0f;
     GetComponent<Collider>().enabled = false;
     SkinnedMeshRenderer[] renderers =
         GetComponentsInChildren<SkinnedMeshRenderer>();
     foreach (SkinnedMeshRenderer r in renderers) {
       r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
     }
   }
 public
  void ExitVehicle() {
    if (GameData.Vehicle == null) return;
    timeInVehicle = 0.0f;

    GetComponent<Collider>().enabled = true;

    transform.position = GameData.Vehicle.transform.position + Vector3.up * 2f;
    transform.rotation = Quaternion.Euler(
        0, GameData.Vehicle.transform.rotation.eulerAngles.y, 0);
    rbody.velocity = Vector3.zero;
    Camera.transform.rotation = transform.rotation;

    SkinnedMeshRenderer[] renderers =
        GetComponentsInChildren<SkinnedMeshRenderer>();
    foreach (SkinnedMeshRenderer r in renderers) {
      r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }

    GameData.Vehicle = null;
  }

  void OnAnimatorIK() {
    if (!isLocalPlayer) return;
    if (Mathf.Abs(rbody.velocity.x) > 0.02f ||
        Mathf.Abs(rbody.velocity.z) > 0.02f) {
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
    anim.SetBool("OnGround", godMode || (Time.time - lastGroundedTime <= 0.1f));
    anim.SetBool("Crouch", isCrouched);
  }

  void OnTriggerEnter(Collider other) {
    Debug.Log(other);
    if (other.gameObject.CompareTag("Collectible") &&
        (endTime > Time.time || timer == null)) {
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
    } else if (other.gameObject.CompareTag("Portal")) {
      if (GameData.levelComplete()) {
        GameData.nextLevel();
      }
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
    TerrainGenerator.RemovePlayer(GetComponent<InitPlayer>());
  }
}
