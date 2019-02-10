using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

// adapted from https://github.com/michaelcurtiss/UnityOutlineFX
[RequireComponent(typeof(Camera))]
public class Outliner : MonoBehaviour 
{
	[System.Serializable]
	public class OutlinedObject
	{
		public Renderer[] renderers;

		public OutlinedObject(Renderer[] renderers)
		{
			this.renderers = renderers;
		}
	}

	public Color outlineColor = new Color(1,0,0,0.5f);
	[Range(0, 1)] public float occludedOutlineOpacity = 0.3f;
	[Range(0, 3)] public float blurSize = 1.0f;
	[Range(0, 1)] public float alphaCutoff = 0.1f;
	public CameraEvent bufferDrawEvent = CameraEvent.BeforeImageEffects;

	CommandBuffer commandBuffer;
	Material outlineMaterial;
	Camera cam;
	List<OutlinedObject> outlinedObjects = new List<OutlinedObject>();


	void OnEnable()
	{
		if (outlinedObjects.Count == 0) return;
		CreateCommandBuffer();
	}


	void OnDisable()
	{
		Cleanup();
	}


	// update if values change at runtime - in editor only
	void OnValidate()
	{
		if (outlinedObjects.Count == 0) return;
		CreateCommandBuffer();
	}


	void Cleanup()
	{
		if (commandBuffer == null) return;
		cam.RemoveCommandBuffer(bufferDrawEvent, commandBuffer);
		commandBuffer = null;
	}


	public void AddOutlinedObject(OutlinedObject obj)
	{
		outlinedObjects.Add(obj);
		CreateCommandBuffer();
	}


	public void RemoveOutlinedObject(OutlinedObject obj)
	{
		outlinedObjects.Remove(obj);
		CreateCommandBuffer();
	}


	public void ClearAllOutlinedObjects()
	{
		outlinedObjects.Clear();
		CreateCommandBuffer();
	}


	public void CreateCommandBuffer()
	{
		// nothing to outline? cleanup
		if (outlinedObjects.Count == 0)
		{
			Cleanup();
			return;
		}

		// camera
		if (cam == null)
		{
			cam = GetComponent<Camera>();
			cam.depthTextureMode = DepthTextureMode.Depth;
		}

		// material
		if (outlineMaterial == null)
		{
			outlineMaterial = new Material(Shader.Find("Hidden/Outliner"));
		}

		// command buffer
		if (commandBuffer == null)
		{
			commandBuffer = new CommandBuffer();
			commandBuffer.name = "Outliner Command Buffer";
			cam.AddCommandBuffer(bufferDrawEvent, commandBuffer);
		}
		else
		{
			commandBuffer.Clear();
		}

		// initialization
		int width = (cam.targetTexture != null) ? cam.targetTexture.width : cam.pixelWidth;
		int height = (cam.targetTexture != null) ? cam.targetTexture.height : cam.pixelHeight;
		int aTempID = Shader.PropertyToID("_aTemp");
		commandBuffer.GetTemporaryRT(aTempID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
		commandBuffer.SetRenderTarget(aTempID, BuiltinRenderTextureType.CurrentActive);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);

		// render selected objects into a mask buffer, with different colors for visible vs occluded ones 
		float id = 0f;
		foreach (var collection in outlinedObjects)
		{
            		id += 0.25f;
			commandBuffer.SetGlobalFloat("_ObjectId", id);

			foreach (var render in collection.renderers)
			{
				Material mat = outlineMaterial;
				if (render.material.mainTexture != null)
				{
					mat = new Material(outlineMaterial);
					mat.SetTexture("_MainTex", render.material.mainTexture);
					mat.SetFloat("_Cutoff", alphaCutoff);
					mat.SetFloat("_DoClip", 1);
				}
				commandBuffer.DrawRenderer(render, mat, 0, 1);
				commandBuffer.DrawRenderer(render, mat, 0, 0);
			}
		}

		// object ID edge dectection pass
		int bTempID = Shader.PropertyToID("_bTemp");
		commandBuffer.GetTemporaryRT(bTempID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
		commandBuffer.Blit(aTempID, bTempID, outlineMaterial, 3);

		// Blur - adjusting blur size to appear the same size, no matter the resolution
		float proportionalBlurSize = ((float)height / 1080f) * blurSize;
		int cTempID = Shader.PropertyToID("_cTemp");
		commandBuffer.GetTemporaryRT(cTempID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
		commandBuffer.SetGlobalVector("_BlurDirection", new Vector2(proportionalBlurSize, 0));
		commandBuffer.Blit(bTempID, cTempID, outlineMaterial, 2);
		commandBuffer.SetGlobalVector("_BlurDirection", new Vector2(0, proportionalBlurSize));
		commandBuffer.Blit(cTempID, bTempID, outlineMaterial, 2);

		// final overlay
		commandBuffer.SetGlobalColor("_OutlineColor", outlineColor);
		commandBuffer.SetGlobalFloat("_OutlineInFrontOpacity", occludedOutlineOpacity);
		commandBuffer.Blit(bTempID, BuiltinRenderTextureType.CameraTarget, outlineMaterial, 4);

		// release tempRTs
		commandBuffer.ReleaseTemporaryRT(aTempID);
		commandBuffer.ReleaseTemporaryRT(bTempID);
		commandBuffer.ReleaseTemporaryRT(cTempID);
	}
}
