var iMaxNum = 4;
var blockW : double;
var blockH : double;
var roadW : double;
var NSTiles = new Array(iMaxNum);
var WETiles = new Array(iMaxNum);
var RoadN : Transform;
var RoadW : Transform;
var Buildings : Transform[];

function Start () {
	for(var r=0; r<iMaxNum; r++) {
		NSTiles[r] = new Array(iMaxNum);
		WETiles[r] = new Array(iMaxNum);
		for(var c=0; c<iMaxNum; c++) {
			newTile(r,c);
		}
	}
}
function newTile(r, c) {
	NSTiles[r][c] = {["direction"]: 0, ["type"]: 0};
	WETiles[r][c] = {["direction"]: 1, ["type"]: 0};
	
	Instantiate(RoadN, new Vector3(c*(blockW+roadW)-blockW/2-roadW/2, 1, r*(blockH+roadW)-blockH/2-roadW/2), Quaternion.identity);
	Instantiate(RoadW, new Vector3(c*(blockW+roadW), 1, r*(blockH+roadW)), Quaternion.identity);
}
function updateNSTiles() {

}