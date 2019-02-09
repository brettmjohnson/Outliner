using UnityEngine;

public class OutlinerObject : MonoBehaviour
{
	Outliner outliner;
	Outliner.OutlinedObject outlinedObject;


	void Awake()
	{
		outliner = FindObjectOfType<Outliner>();
		outlinedObject = new Outliner.OutlinedObject(GetComponentsInChildren<Renderer>());
	}


	void OnEnable()
	{
		if (outliner == null || outlinedObject == null) return;
		outliner.AddOutlinedObject(outlinedObject);
	}


	void OnDisable()
	{
		if (outliner == null || outlinedObject == null) return;
		outliner.RemoveOutlinedObject(outlinedObject);
	}

}
