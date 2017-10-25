using UnityEngine;
using UnityEngine.UI;

class PlayerWeaponController : MonoBehaviour {
  public Weapon currentWeapon;
  public Weapon[] allWeapons = {};
  public WeaponUI weaponUI = null;

  WeaponUI currentUIInstance;
  bool isUIOpen = false;

  void Start() {
    for (int i = 0; i < allWeapons.Length; i++) {
      for (int j = 0; j < allWeapons.Length; j++) {
        if (i == j) continue;
        if (allWeapons[i].weaponID == allWeapons[j].weaponID) {
          Debug.LogError(
              "Two weapons have the same ID! This is not allowed! (" +
              allWeapons[i].ToString() + ") == (" + allWeapons[j].ToString() +
              ").");
        }
      }
    }
    (currentUIInstance = Instantiate(weaponUI))
        .Create(allWeapons, currentWeapon);
  }
  void Update() {
    if (!GameData.isPaused && !GameData.isChatOpen &&
        Input.GetAxis("ShowWeaponUI") > 0.1f) {
      if (!isUIOpen) ShowUI();
    } else if (isUIOpen) {
      HideUI();
    }
  }
  public void ShowUI() {
    currentUIInstance.Show();
    GameData.isWeaponUIOpen = true;
    isUIOpen = true;
   }
  public void HideUI() {
    ChangeWeapon(currentUIInstance.Hide());

    GameData.isWeaponUIOpen = false;
    isUIOpen = false;
  }
  public void ChangeWeapon(Weapon nextWeapon) {
    if (nextWeapon != null) {
      Debug.Log("Changing weapon to " + nextWeapon.ToString());
      currentWeapon = nextWeapon;
    }
  }

  void OnDestroy() { Destroy(currentUIInstance); }
}
