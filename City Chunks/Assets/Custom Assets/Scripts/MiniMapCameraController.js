private
var revertFogState = false;
var myCamera;

function Start() { myCamera = GetComponent(Camera); }

function Update() {
  var aspect = (Screen.width + 0.0) / Screen.height;
  var height = .25f;
  var width = height / aspect;
  var rect = new Rect(1 - width - 0.01, 1 - height - 0.01, width, height);
  if(myCamera.rect != rect) {
    myCamera.rect = rect;
    Debug.Log("MiniMap Camera Controller Updated!");
  }
}

function OnPreRender() {
  revertFogState = RenderSettings.fog;
  RenderSettings.fog = false;
}

function OnPostRender() { RenderSettings.fog = revertFogState; }
