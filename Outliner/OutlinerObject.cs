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
		SetOutlineVisibilty(true);
	}


	void OnDisable()
	{
		SetOutlineVisibilty(false);
	}


	public void SetOutlineVisibilty(bool visible)
	{
		if (outliner == null || outlinedObject == null) return;

		if (visible)
		{
			outliner.AddOutlinedObject(outlinedObject);
		}
		else
		{
			outliner.RemoveOutlinedObject(outlinedObject);
		}
	}

}
