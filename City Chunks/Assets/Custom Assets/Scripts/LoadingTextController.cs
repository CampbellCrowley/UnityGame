using UnityEngine;
using UnityEngine.UI;
using System.Collections;

class LoadingTextController : MonoBehaviour {
  void Update() { GetComponent<Text>().text = GameData.loadingMessage; }
}

