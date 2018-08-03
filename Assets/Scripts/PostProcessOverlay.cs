using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessOverlay : MonoBehaviour
{
    public Material material;
    private RenderTexture spatialMapDepthTexture;
    private RenderTexture cgDepthTexture;
    private Texture2D webcamTextureClone;
    public int SpatialMapLayer = 9;
    private Camera thisCamera;
    private Camera copyCamera;
    private GameObject copyCameraGameObject;

    int screenWidth;
    int screenHeight;

    int webcamWidth;
    int webcamHeight;

    public bool useSemantic = false;

    // Use this for initialization
    void Start()
    {
        // Open webcamera
        webcamWidth = LocatableCamera.width;
        webcamHeight = LocatableCamera.height;

        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();

        screenWidth = Screen.width;
        screenHeight = Screen.height;

        spatialMapDepthTexture = new RenderTexture(screenWidth, screenHeight, 16, RenderTextureFormat.Depth);
        cgDepthTexture = new RenderTexture(screenWidth, screenHeight, 16, RenderTextureFormat.Depth);
        webcamTextureClone = new Texture2D(webcamWidth, webcamHeight, TextureFormat.ARGB32, false);
        
        material.SetTexture("_SpatialMapTex", spatialMapDepthTexture);
        material.SetTexture("_CgDepthTex", cgDepthTexture);

        material.EnableKeyword("_VisibilityComplex");
        material.EnableKeyword("_VisibilitySimple");

        //start capture of webcam stream
        if (useSemantic)
        {
            material.SetTexture("_WebcamTex", webcamTextureClone);
            material.EnableKeyword("_WebcamTex");
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        ////get spatial render texture
        if (copyCamera == null)
        {
            Debug.Log("Copy Camera does not exist");
            return;
        }

        copyCamera.CopyFrom(thisCamera);
        copyCamera.targetTexture = spatialMapDepthTexture;
        RenderTexture.active = spatialMapDepthTexture;
        copyCamera.cullingMask = (1 << SpatialMapLayer);
        copyCamera.Render();

        //get cg render texture
        copyCamera.targetTexture = cgDepthTexture;
        RenderTexture.active = cgDepthTexture;
        copyCamera.cullingMask = (1 << 0);
        copyCamera.cullingMask = copyCamera.cullingMask & ~(1 << SpatialMapLayer);
        copyCamera.Render();

        // Copy webcam image to texture
        //int setWidth = LocatableCamera.width - LocatableCamera.width / 16 - (int)((float)LocatableCamera.width / 4.5);
        //int setHeight = LocatableCamera.height - (int)((float)LocatableCamera.height / 4.3) - LocatableCamera.height / 8;
        //Graphics.CopyTexture(LocatableCamera.webcamTexture, 0, 0, 
        //    LocatableCamera.width / 16, (int)((float)LocatableCamera.height / 4.3),
        //    setWidth, setHeight,
        //    webcamTextureClone, 0, 0, 0, 0);

        //Graphics.CopyTexture(LocatableCamera.webcamTexture, 0, 0,
        //0, 0, 
        //LocatableCamera.width, LocatableCamera.height,
        //webcamTextureClone, 0, 0, 0, 0);
        Graphics.CopyTexture(LocatableCamera.webcamTexture, webcamTextureClone);
        //Load Semantic Image to Texture

        //DebugToServer.Log.Send(source.width.ToString() + " " + source.height.ToString());
        Graphics.Blit(source, destination, material);
    }

    // Update is called once per frame
    void Update()
    {
        //Graphics.CopyTexture(LocatableCamera.webcamTexture, webcamTextureClone);
        //DebugToServer.Log.Send(LocatableCamera.width.ToString() + " " + LocatableCamera.height.ToString());
        //if (LocatableCamera.webcamTexture.didUpdateThisFrame)
        //{
        //    Graphics.CopyTexture(LocatableCamera.webcamTexture, 0, 0, 0, 0, screenWidth, screenHeight,
        //        webcamTextureClone, 0, 0, 0, 0);
        //    webcamTextureClone.Apply();
        //}
        //webcamTextureClone.SetPixels(LocatableCamera.webcamTexture.GetPixels(0, 0, screenWidth, screenHeight));
        //webcamTextureClone.Apply();
    }

}
