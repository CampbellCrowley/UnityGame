var waterLevel : float;
private var isUnderwater : boolean;
@SerializeField
public var normalColor : Color = new Color (0.5f, 0.5f, 0.5f, 0.5f);
@SerializeField
public var underwaterColor : Color = new Color (0.22f, 0.65f, 0.77f, 0.5f);

/* function Start () {
	normalColor = new Color (0.5f, 0.5f, 0.5f, 0.5f);
	underwaterColor = new Color (0.22f, 0.65f, 0.77f, 0.5f);
} */
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
	RenderSettings.fogDensity = 0.50f;
  RenderSettings.fogStartDistance = 0.0f;
  RenderSettings.fogEndDistance = 5.0f;
}
