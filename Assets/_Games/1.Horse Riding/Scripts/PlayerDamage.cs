using UnityEngine;
using System.Collections;

public class PlayerDamage : MonoBehaviour {
	public float damage;
    public float hitDamage = 5;

    public static PlayerDamage instance;

	// Use this for initialization
	void Awake () {
		instance = this;
	}

}
