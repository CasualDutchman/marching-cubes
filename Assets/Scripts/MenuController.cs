using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

    World world;

    public GameObject menuObject;

    public InputField seedInput;
    public Slider octavesInput;
    public Text octavesDisplay;

    void Start () {
        world = GetComponent<World>();
	}
	
	void Update () {
        octavesDisplay.text = octavesInput.value.ToString();
    }

    public void OctaveInputChanged(float input) {
        world.octaves = (int)input;
    }

    public void SeedInputChanges(string input) {
        world.seed = int.Parse(input);
    }

    public void GenerateWorld() {
        world.enabled = true;
        menuObject.SetActive(false);
    }
}
