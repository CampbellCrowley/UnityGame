using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.Networking;
#pragma warning disable 0168

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
  float staminaDepletionRate = 0.1f;  // Percent per second
 public
  float staminaRechargeDelay = 3.0f;  // Seconds
 public
  float staminaRechargeMultiplier = 1.5f;
  [Header("Camera")]
 public
  GameObject Camera;
  // public
  //  bool CameraDamping = false;
 public
  bool CameraObjectAvoidance = true;
 public
  bool rotateWithCamera = false;
 public
  float MaxCameraDistance = 3f;
  [Header("OSDs/HUD")]
 public
  GUIText collectedCounter;
 public
  GUIText lifeCounter;
 public
  GUIText timer;
 public
  GUIText stamina;
 public
  GUIText levelDisplay;
 public
  float staminaCountBars = 20f;
 public
  GUIText debug;
  [Header("Look and Sound")]
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
  float sendFreqency = 0.1f;
 public
  float GameTime = 10f;
 public
  GameObject RagdollTemplate;
 private
  GameObject Ragdoll;
 public
  bool isDead = false;


 private
  Rigidbody rbody;
 private
  Animator anim;
 private
  TextMesh nameplate;
 private
  Color startColor;
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
  bool godMode = false;
 private
  bool isUnderwater = false;
 private
  float endTime = 0f;
 private
  float levelStartTime = 0f;
 private
  float lastGroundedTime = 0f;
 private
  float lastSprintTime = 0f;
 private
  float staminaRemaining = 1.0f;
 private
  float lastJumpSoundTimejump = 0.0f;
 private
  float lastFootstepTime = 0.0f;
 private
  float lastSendTime = 0.0f;
 private
  float lastVignetteAmount = 0.0f;
 private
  float lastSprintInput = 0.0f;
 private
  float deathTime = 0.0f;
 private
  Transform lastFloorTransform;
 private
  Vector3 lastFloorTransformPosition;

  [SyncVar] public string username = "Username";
  [SyncVar] private Vector3 rbodyPosition, rbodyVelocity, transformPosition;
  [SyncVar] private Quaternion rbodyRotation, transformRotation;

 public
  override void OnStartLocalPlayer() {
    if (sounds.LandSound != null) sounds.LandSound.LoadAudioData();
    if (sounds.JumpSound != null) sounds.JumpSound.LoadAudioData();
    foreach (AudioClip step in sounds.FootSteps) {
      if (step != null) step.LoadAudioData();
    }
    GameData.showCursor = false;
    UnDead();

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

    GetComponent<MeshRenderer>().material.color = Color.blue;

    Debug.Log("Send Freqency: " + sendFreqency);
    CmdChangeName(GameData.username);
    CmdUpdatePlayer(rbody.position, rbody.velocity, rbody.rotation,
                           transform.position, transform.rotation);

    levelStartTime = Time.time;
    lastGroundedTime = Time.time;
    lastSprintTime = Time.time;
    lastJumpSoundTimejump = Time.time;
    lastFootstepTime = Time.time;
    lastSendTime = Time.realtimeSinceStartup;

  }

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

  void Update() {
    rbody = GetComponent<Rigidbody>();
    nameplate = GetComponentInChildren<TextMesh>();
    nameplate.transform.LookAt(Camera.transform.position);
    nameplate.transform.rotation *= Quaternion.Euler(0, 180f, 0);
    if (username != "Username") {
      nameplate.text = username;
    } else {
      nameplate.text = "Player " + netId;
    }
    if (!isLocalPlayer) {
      rbody.position = rbodyPosition;
      rbody.velocity = rbodyVelocity;
      rbody.rotation = rbodyRotation;
      transform.position = transformPosition;
      transform.rotation = transformRotation;
      return;
    }

    if (isDead) {
      if (Time.realtimeSinceStartup - deathTime >= 8f) {
        if (GameData.health > 0)
          GameData.restartLevel();
        else
          GameData.MainMenu();
      } else {
        Time.timeScale = Mathf.Lerp(
            // 0.05f, 0.1f, (Time.realtimeSinceStartup - deathTime) / 3f);
            0.05f, 1.0f, (Time.realtimeSinceStartup - deathTime) / 8f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
      }
    }

    // Inputs and Player Controls
    float moveHorizontal = Input.GetAxis("Horizontal");
    float moveVertical = Input.GetAxis("Vertical");
    float lookHorizontal = Input.GetAxis("Mouse X");
    float lookVertical = Input.GetAxis("Mouse Y");
    if(Input.GetButtonDown("GodMode")) godMode = !godMode;
    RaycastHit hitinfo;
    isGrounded =
        Physics.SphereCast(transform.position + Vector3.up * (-0.49f + 1.2f),
                           0.0f, Vector3.down, out hitinfo, 0.8f);
    isCrouched = Input.GetAxis("Crouch") > 0.5;
    bool jump = Input.GetAxis("Jump") > 0.5 && isGrounded && !isCrouched;
    float sprintInput = Input.GetAxis("Sprint");
    isSprinting =
        (sprintInput > 0.5 && !isCrouched) || (isSprinting && !isGrounded);
    bool wasUnderwater = isUnderwater;
    isUnderwater = transform.position.y < TerrainGenerator.waterHeight;

    if (wasUnderwater && !isUnderwater) lastGroundedTime = Time.time;
    if (isUnderwater) isGrounded = false;

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

    // Prevent movement in first 1.5 seconds of the level or if dead, or if
    // paused.
    if (Time.time - levelStartTime < 1.5 || isDead || GameData.isPaused) {
      moveHorizontal = 0;
      moveVertical = 0;
      lookHorizontal = 0;
      lookVertical = 0;
      rbody.velocity = Vector3.up * 0f;
      jump = false;
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
    if (collectedCounter != null) {
      collectedCounter.text =
          "Bombs Remaining: " + GameData.collectedCollectibles;
    }
    if (lifeCounter != null) {
      lifeCounter.text = GameData.health + " Health";
    }
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
    if (levelDisplay != null) {
      levelDisplay.text = "Level: " + GameData.getLevel();
    }

    // Movement
    transform.position = rbody.transform.position;
    transform.rotation = rbody.transform.rotation;
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
      movement += (Input.GetAxis("Jump") * 100f * Time.deltaTime) * Vector3.up;
      movement -= (Input.GetAxis("Crouch") * 100f * Time.deltaTime) * Vector3.up;
      movement *= 30f;
    } else {
      if (isUnderwater) {
        if (transform.position.y < TerrainGenerator.waterHeight - 1f) {
          movement += Mathf.Clamp(rbody.velocity.y + 5.0f * 2f * Time.deltaTime,
                                  -moveSpeed, moveSpeed) *
                      Vector3.up;
        } else {
          movement +=
              (rbody.velocity.y - 9.81f * 3f * Time.deltaTime) * Vector3.up;
        }
      } else {
        movement += ((jump ? (moveSpeed * jumpMultiplier) : 0.0f) +
                     (rbody.velocity.y - 9.81f * 2f * Time.deltaTime)) *
                    Vector3.up;
      }
    }

    movement =
        Quaternion.Euler(0, Camera.transform.eulerAngles.y, 0) * movement;
    rbody.velocity = Vector3.Lerp(movement, rbody.velocity, 0.5f);

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
    if (CameraObjectAvoidance) {
      RaycastHit hit;
      Physics.Linecast(transform.position + Vector3.up * 2f,
                       Camera.transform.position, out hit,
                       LayerMask.GetMask("Terrain"));
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
    Vector3 newCameraPos =
        Vector3.up * 2f +
        Vector3.ClampMagnitude(
            (Vector3.left *
                 (Mathf.Sin(Camera.transform.eulerAngles.y / 180f * Mathf.PI) -
                  Mathf.Sin(Camera.transform.eulerAngles.y / 180f * Mathf.PI) *
                      Mathf.Sin((-45f + Camera.transform.eulerAngles.x) / 90f *
                                Mathf.PI)) +
             Vector3.back *
                 (Mathf.Cos(Camera.transform.eulerAngles.y / 180f * Mathf.PI) -
                  Mathf.Cos(Camera.transform.eulerAngles.y / 180f * Mathf.PI) *
                      Mathf.Sin((-45f + Camera.transform.eulerAngles.x) / 90f *
                                Mathf.PI)) +
             Vector3.up *
                 Mathf.Sin(Camera.transform.eulerAngles.x / 180f * Mathf.PI)),
            1.0f) *
            CurrentCameraDistance;
    if(!isDead || Ragdoll == null) {
      newCameraPos += transform.position;
    } else {
      newCameraPos += Ragdoll.transform.position;
    }
    if (isDead) {
      newCameraPos =
          Vector3.Lerp(Camera.transform.position, newCameraPos, 0.05f);
    } else if (GameData.cameraDamping) {
      newCameraPos =
          Vector3.Lerp(Camera.transform.position, newCameraPos, 0.33f);
    }
    Camera.transform.position = newCameraPos;
    Camera.transform.rotation =
        Quaternion.Euler(Camera.transform.eulerAngles.x - lookVertical,
                         Camera.transform.eulerAngles.y + lookHorizontal, 0);

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
        if (target_.name.Contains("Head")) {
          target = target_;
          break;
        }
      }
      Quaternion startRot = Camera.transform.rotation;
      Camera.transform.LookAt(target.position + Vector3.up * 0.2f);
      Camera.transform.rotation = Quaternion.Lerp(startRot, Camera.transform.rotation, 0.1f);
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
    vignette = 0.45f - (enemydistance / 50f);
    vignette = Mathf.Lerp(lastVignetteAmount, vignette, 1.0f * Time.deltaTime);
    lastVignetteAmount = vignette;
    try {
      Camera.GetComponent<VignetteAndChromaticAberration>().intensity =
          vignette;
    } catch (System.NullReferenceException e) {
    }
    if (Camera.transform.position.y > 194f) {
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

    // Sound
    if (isGrounded && Time.time - lastGroundedTime >= 0.05f &&
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
        if(sounds.FootSteps.Length > 0) {
          AudioClip footstepSound =
              sounds.FootSteps[(int)Random.Range(0, sounds.FootSteps.Length)];
          PlaySound(footstepSound);
        }
      }
    }

    if (isGrounded) lastGroundedTime = Time.time;
    if (Time.realtimeSinceStartup - lastSendTime > sendFreqency) {
      lastSendTime = Time.realtimeSinceStartup;
      CmdUpdatePlayer(rbody.position, rbody.velocity, rbody.rotation,
                      transform.position, transform.rotation);
    }
  }

  void Dead() {
    if (isDead) return;
    isDead = true;
    if (GameData.health <= 0) {
      PlaySound(sounds.LevelFail, 1.0f);
    } else {
      PlaySound(sounds.Pain, 1.0f);
    }
    MaxCameraDistance *= 2f;
    deathTime = Time.realtimeSinceStartup;
    GetComponent<Rigidbody>().isKinematic = true;
    if (RagdollTemplate != null) {
      Ragdoll = Instantiate(RagdollTemplate);
      Ragdoll.SetActive(true);
      Ragdoll.transform.position = transform.position;
      Ragdoll.transform.rotation = transform.rotation;
      foreach(Transform parent in GetComponentsInChildren<Transform>()) {
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
    Time.timeScale = 1.0f;
    Time.fixedDeltaTime = 0.02f * Time.timeScale;
  }

  void OnAnimatorIK() {
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
    anim.SetBool("OnGround", godMode || (Time.time - lastGroundedTime <= 0.05f));
    anim.SetBool("Crouch", isCrouched);
  }

  void OnTriggerEnter(Collider other) {
    Debug.Log(other);
    if (other.gameObject.CompareTag("Collectible") &&
        (endTime > Time.time || timer == null)) {
      Destroy(other.gameObject);
      GameData.collectedCollectibles+=10;
      PlaySound(sounds.CollectibleSound);
    } else if (other.gameObject.CompareTag("Enemy")) {
      if (GameData.getLevel() == 3) {
        GameData.health=0;
        Dead();
      } else {
        GameData.health--;
        Dead();
      }
    } else if (other.gameObject.CompareTag("EnemyProjectile")) {
      Destroy(other.gameObject);
      if (GameData.getLevel() == 3) {
        GameData.health=0;
        Dead();
      } else {
        GameData.health--;
        Dead();
      }
    } else if (other.gameObject.CompareTag("Portal")) {
      if(GameData.levelComplete()) {
        GameData.nextLevel();
      }
    }
  }
  void PlaySound(AudioClip clip, float volume = -1f) {
    if (sounds.Player != null && clip != null && GameData.soundEffects) {
      AudioPlayer player = Instantiate(sounds.Player) as AudioPlayer;
      player.clip = clip;
      if (volume >= 0f && volume <= 1f) {
        player.volume = volume;
      }
    }
  }
  // void OnDestroy() { DestroyImmediate(Camera, true); }
  void onDestroy() { GameData.showCursor = true; }
}
