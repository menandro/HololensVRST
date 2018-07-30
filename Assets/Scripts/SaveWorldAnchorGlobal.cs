using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Sharing;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
#if !UNITY_EDITOR
using Windows.Storage;
#endif

public class SaveWorldAnchorGlobal
{
    // Exporting
    private GameObject gameObjectToGet;
    private byte[] exportedData;
    private string exportAnchorName;
    private WorldAnchor exportedAnchor;
    private string exportFilename;
    private MemoryStream ms;

    // Importing
    public WorldAnchor importedAnchor;
    private byte[] importedData = null;
    private int retryCount = 3;
    private string importAnchorName;
    private GameObject gameObjectToSet;
    private string importFilename;
    public bool isImportFileLoaded = false;
    private bool isLoadingFile = false;

    // General Request
    public bool isRequestActive = false;
    public bool isRequestFinished = false;

#if UNITY_EDITOR
    public void Save(string filename, string anchorName, WorldAnchor anchor)
    {
        Debug.Log("Saving world anchor using transfer batch won't work in Editor.");
    }
    public void Save(string filename, string anchorName, GameObject gameObject)
    {
        Debug.Log("Saving world anchor using transfer batch won't work in Editor.");
    }
#endif
#if !UNITY_EDITOR
    public void Save(string filename, string anchorName, WorldAnchor anchor)
    {
        exportFilename = filename;
        exportAnchorName = anchorName;
        exportedAnchor = anchor;
        ms = new MemoryStream();

        WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();
        transferBatch.AddWorldAnchor(exportAnchorName, exportedAnchor);
        WorldAnchorTransferBatch.ExportAsync(transferBatch, OnExportDataAvailable, OnExportComplete);
    }

    public void Save(string filename, string anchorName, GameObject gameObject)
    {
        gameObjectToGet = gameObject;
        WorldAnchor anchor = gameObjectToGet.GetComponent<WorldAnchor>();
        if (anchor == null)
        {
            anchor = gameObjectToGet.AddComponent<WorldAnchor>();
        }
        Save(filename, anchorName, anchor);
    }

    private void OnExportDataAvailable(byte[] data)
    {
        //Save data to file
        ms.Write(data, 0, data.Length);
        //DebugToServer.Log.Send("Read data: " + data.Length.ToString());
        //DebugToServer.Log.Send("Copied data: " + exportedData.Length.ToString());
    }

    private void OnExportComplete(SerializationCompletionReason completionReason)
    {
        if (completionReason != SerializationCompletionReason.Succeeded)
        {
            //Failed
        }
        else
        {
            //Success
            exportedData = ms.ToArray();
            //DebugToServer.Log.Send("Total saved data: " + exportedData.Length.ToString());
            Task.Factory.StartNew(() => SaveToFile());
        }
    }

    private async void SaveToFile()
    {
        StorageFolder storageFolder = KnownFolders.CameraRoll;
        StorageFile storageFile = await storageFolder.CreateFileAsync(exportFilename, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(storageFile, exportedData);
        //DebugToServer.Log.Send("Anchor file saved as: " + exportFilename);
    }

    //public async Task<string> LoadFromFile()
    //{
    //    isLoadingFile = true;
    //    StorageFolder storageFolder = KnownFolders.CameraRoll;
    //    StorageFile storageFile;
    //    try
    //    {
    //        storageFile = await storageFolder.GetFileAsync(importFilename);
    //    }
    //    catch (Exception ex)
    //    {
    //        DebugToServer.Log.Send(ex.ToString());
    //        return ex.ToString();
    //    }
    //    if (storageFile != null){
    //        var buffer = await FileIO.ReadBufferAsync(storageFile);
    //        importedData = buffer.ToArray();
    //        isImportFileLoaded = true;
    //    }
    //    else
    //    {
    //        isImportFileLoaded = false;
    //        DebugToServer.Log.Send("Anchor file: " + importFilename + " not found.");
    //        throw new Exception("Anchor file: " + importFilename + " not found.");
    //    }
    //    isLoadingFile = false;
    //    return null;
    //}

    public async void SetWorldAnchor(string filename, string anchorName, GameObject gameObject)
    {
        isRequestActive = true;
        isRequestFinished = false;
        // Import Data (open from file)
        importFilename = filename;
        importAnchorName = anchorName;
        gameObjectToSet = gameObject;

        // Load the data if it not yet loaded
        if (importedData == null)
        {
            StorageFolder storageFolder = KnownFolders.CameraRoll;
            DebugToServer.Log.Send("Opening file.");
            StorageFile storageFile = await storageFolder.GetFileAsync(importFilename);
            DebugToServer.Log.Send("File found. Reading file.");
            var buffer = await FileIO.ReadBufferAsync(storageFile);
            importedData = buffer.ToArray();
        }
        WorldAnchorTransferBatch.ImportAsync(importedData, OnImportComplete);

        //var res = Task.Run(async () => await LoadFromFile()).Result;

        // Read the world anchor
        //if (!isLoadingFile && isImportFileLoaded)
        //{
        //    importAnchorName = anchorName;
        //    WorldAnchorTransferBatch.ImportAsync(importedData, OnImportComplete);
        //}
        //else if (!isLoadingFile)
        //{
        //    //Assign a random world anchor
        //    DebugToServer.Log.Send("Assigning random world anchor.");
        //    gameObjectToSet.AddComponent<WorldAnchor>();
        //}

    }
    
    private void OnImportComplete(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
    {
        if (completionReason != SerializationCompletionReason.Succeeded)
        {
            //Import failed
            if (retryCount > 0)
            {
                retryCount--;
                WorldAnchorTransferBatch.ImportAsync(importedData, OnImportComplete);
            }
            return;
        }
        DebugToServer.Log.Send("Setting world anchor...");
        string[] ids = deserializedTransferBatch.GetAllIds();
        foreach (string id in ids)
        {
            DebugToServer.Log.Send(id);
        }
        importedAnchor = deserializedTransferBatch.LockObject(importAnchorName, gameObjectToSet);
        DebugToServer.Log.Send("World anchor set for " + gameObjectToSet.name);
        isRequestActive = false;
        isRequestFinished = true;
    }
#endif
#if UNITY_EDITOR
    public void SetWorldAnchor(string filename, string anchorName, GameObject gameObject)
    {
        gameObject.AddComponent<WorldAnchor>();
    }
#endif
}
