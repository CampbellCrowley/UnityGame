/* var waterLevel : float;
private var isUnderwater : boolean;
@SerializeField
public var normalColor : Color = new Color (0.5f, 0.5f, 0.5f, 0.5f);
@SerializeField
public var underwaterColor : Color = new Color (0.22f, 0.65f, 0.77f, 0.5f);

function Start () {
  SetNormal();
}

function Update () {
	if((transform.position.y < waterLevel) != isUnderwater) {
		isUnderwater=transform.position.y<waterLevel;
		if(isUnderwater) SetUnderwater();
		if(!isUnderwater) SetNormal();
	}
}

function SetNormal() {
	RenderSettings.fogColor = normalColor;
	RenderSettings.fogDensity = 0.002f;
  RenderSettings.fogStartDistance = 100.0f;
  RenderSettings.fogEndDistance = 900.0f;
}

function SetUnderwater () {
	RenderSettings.fogColor = underwaterColor;
	// RenderSettings.fogDensity = 0.50f;
  // RenderSettings.fogStartDistance = 0.0f;
  // RenderSettings.fogEndDistance = 10.0f;
} */

var underwaterLevel = 500;

private
var defaultFog;
private
var defaultFogColor;
private
var defaultFogDensity;
private
var defaultSkybox;
var noSkybox : Material;

function Start() {
  // Set the background color
  GetComponent.<Camera>().backgroundColor = Color(0, 0.4, 0.7, 1);
  defaultFog = RenderSettings.fog;
  defaultFogColor = RenderSettings.fogColor;
  defaultFogDensity = RenderSettings.fogDensity;
  defaultSkybox = RenderSettings.skybox;
}

function Update() {
  if (transform.position.y < underwaterLevel) {
    RenderSettings.fog = true;
    RenderSettings.fogColor = Color(0, 0.4, 0.7, 0.6);
    RenderSettings.fogDensity = 0.04;
    RenderSettings.skybox = noSkybox;
  }

  else {
    RenderSettings.fog = defaultFog;
    RenderSettings.fogColor = defaultFogColor;
    RenderSettings.fogDensity = defaultFogDensity;
    RenderSettings.skybox = defaultSkybox;
  }
}
