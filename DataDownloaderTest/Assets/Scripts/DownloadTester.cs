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

    [SerializeField] private float megabytesToDownload;
    
    private WaitForSeconds _downloadDelay = new WaitForSeconds(1.0f);

    public void GetFiles()
    {
        StartCoroutine(GetFilesRoutine());
    }

    public void RestorePreviouslyDownloadedFiles()
    {
        StartCoroutine(RestorePreviouslyDownloadedFilesRoutine());
    }

    private IEnumerator RestorePreviouslyDownloadedFilesRoutine()
    {
        yield return StartCoroutine(ResumeDownload());
    }

    private IEnumerator GetFilesRoutine()
    {
        
        for(int i=0; i<_testURIs.Length; i++)
        {
            yield return StartCoroutine(StartDownload(() =>
            {
                _downloadProgress.value = 1.0f;
                _downloadProgressText.text = "1.0";
            },  _testURIs[i], i.ToString()));
            
        }
    }
    
    
    private IEnumerator StartDownload(Action onDownloadCompleted, string testURI, string fileName)
    {
        string downloadFilePath = string.Format("{0}{1}{2}", _dataPath,fileName,POSTFIX); 
        
        using (BackgroundDownload download = BackgroundDownload.Start(new Uri(testURI), downloadFilePath))
        {
            yield return StartCoroutine(UpdateCurrentDownloadProgress(download, downloadFilePath));

            if (onDownloadCompleted != null)
            {
                onDownloadCompleted();
            }

            if (download.status == BackgroundDownloadStatus.Failed)
                Debug.Log(download.error);
            else
            {
                download.Dispose();
                Debug.Log("DONE downloading file");
            }
            
        }
    }
    
    private IEnumerator DEBUG_StartDownloadSample()
    {
        using (var download = BackgroundDownload.Start(new Uri("http://ipv4.download.thinkbroadband.com/50MB.zip"), "files/file.data"))
        {
            yield return download;
            if (download.status == BackgroundDownloadStatus.Failed)
                Debug.Log(download.error);
            else
                Debug.Log("DONE downloading file");
        }
    }

    private IEnumerator UpdateCurrentDownloadProgress(BackgroundDownload download, string downloadFilePath = null)
    {

        if (string.IsNullOrEmpty(downloadFilePath) == false)
        {
            _downloadLog.text = string.Format("Started downloads = {0}\nPersistent path = {1} ",
                BackgroundDownload.backgroundDownloads.Length, downloadFilePath);
        }
        
        float downloadProgress;

        while (download.keepWaiting)
        {
            downloadProgress = download.progress;
            _downloadProgress.value = downloadProgress;
            _downloadProgressText.text = downloadProgress.ToString();

            yield return _downloadDelay;
        }
    }

    private IEnumerator ResumeDownload()
    {
        if (BackgroundDownload.backgroundDownloads.Length == 0)  
        {
            Debug.Log("DOWNLOAD TESTER: ResumeDownload() -  no previous downloads. Breaking here.");
            yield break;
        }

        BackgroundDownload download = BackgroundDownload.backgroundDownloads[0];
        
        Debug.Log("DOWNLOAD TESTER: ResumeDownload() Przywracam pobieranie: url "+download.config.url + " " + download.config.filePath);

        
        yield return StartCoroutine(UpdateCurrentDownloadProgress(download));

        //yield return download;
        
        download.Dispose();
    }

    
    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
