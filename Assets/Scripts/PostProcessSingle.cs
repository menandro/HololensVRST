using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class PostProcessSingle : MonoBehaviour
{
    public Material material;
    private RenderTexture spatialMapDepthTexture;
    private RenderTexture cgDepthTexture;
    public int SpatialMapLayer = 8;
    private Camera thisCamera;
    private Camera copyCamera;
    private GameObject copyCameraGameObject;

    public bool useSemantic = false;

    // Use this for initialization
    void Start()
    {
        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();
        spatialMapDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        cgDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        material.SetTexture("_SpatialMapTex", spatialMapDepthTexture);
        material.SetTexture("_CgDepthTex", cgDepthTexture);
        material.EnableKeyword("_VisibilityComplex");
        material.EnableKeyword("_VisibilitySimple");

        //start capture of webcam stream
        if (useSemantic)
        {
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

        //Load Semantic Image to Texture

        //DebugToServer.Log.Send(source.width.ToString() + " " + source.height.ToString());
        Graphics.Blit(source, destination, material);
    }

    // Update is called once per frame
    void Update()
    {

    }

}
