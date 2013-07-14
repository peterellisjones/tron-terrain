using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour {
	
	ChunkManager cm;

	// Use this for initialization
	void Start () {
		cm = new ChunkManager();
		
	}
	
	// Update is called once per frame
	void Update () {
		cm.UpdateResolutions();
		cm.BuildAndDestroyChunks();
	}
}
