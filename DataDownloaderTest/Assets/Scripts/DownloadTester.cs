using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Networking;
using UnityEngine;
using UnityEngine.UI;

public class DownloadTester : MonoBehaviour
{
    private const string POSTFIX = ".data";

    [SerializeField] private string[] _testURIs;
    [SerializeField] private string _dataPath;
    [SerializeField] private Slider _downloadProgress;
    [SerializeField] private Text _downloadProgressText;
    [SerializeField] private Text _downloadLog;
    [SerializeField] private Text _sizeText;

    [SerializeField] private float megabytesToDownload;
    
    private WaitForSeconds _downloadDelay = new WaitForSeconds(0.5f);

    public void GetFiles()
    {
        StartCoroutine(GetFilesRoutine());
    }

    private IEnumerator GetFilesRoutine()
    {
        
        for(int i=0; i<_testURIs.Length; i++)
        {
            yield return StartCoroutine(StartDownload((currentFileSize) =>
            {
                megabytesToDownload += currentFileSize;
                _downloadProgress.value = 1.0f;
                _downloadProgressText.text = "1.0";
            },  _testURIs[i], i.ToString()));
            
            _sizeText.text = "Downloaded data size (mb): " + megabytesToDownload;
        }
    }
    
    private IEnumerator StartDownload(Action<float> onDownloadCompleted, string testURI, string fileName)
    {
        using (BackgroundDownload download = BackgroundDownload.Start(new Uri(testURI), _dataPath+fileName+POSTFIX))
        {
            float downloadProgress;

            while (download.keepWaiting)
            {
                downloadProgress = download.progress;
                _downloadProgress.value = downloadProgress;
                _downloadProgressText.text = downloadProgress.ToString();
                
                _downloadLog.text = string.Format("Started downloads = {0}\nFile size = {1} MB\nFilename = {2} ",
                    BackgroundDownload.backgroundDownloads.Length, download.TotalFileSizeMegabytes, fileName);
                yield return _downloadDelay;
            }

            onDownloadCompleted(download.TotalFileSizeMegabytes);

            if (download.status == BackgroundDownloadStatus.Failed)
                Debug.Log(download.error);
            else
            {
                //download.Dispose();
                Debug.Log("DONE downloading file");
            }
        }
    }


    private IEnumerator ResumeDownload()
    {
        if (BackgroundDownload.backgroundDownloads.Length == 0)  
        {
            yield break;
        }

        BackgroundDownload download = BackgroundDownload.backgroundDownloads[0];
        yield return download;
        
        download.Dispose();
    }


    private void OnEnable()
    {
        //StartCoroutine(ResumeDownload());
    }
    
    
    
    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
