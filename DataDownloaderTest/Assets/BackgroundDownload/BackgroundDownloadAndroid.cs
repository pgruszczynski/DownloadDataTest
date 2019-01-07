#if UNITY_ANDROID

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.Networking
{

    class BackgroundDownloadAndroid : BackgroundDownload
    {
        static AndroidJavaClass _playerClass;
        static AndroidJavaClass _backgroundDownloadClass;

        class Callback : AndroidJavaProxy
        {
            public Callback()
                : base("com.unity3d.backgrounddownload.CompletionReceiver$Callback")
            {}

            void downloadCompleted()
            {
                foreach (var download in _downloads.Values)
                    ((BackgroundDownloadAndroid)download).CheckFinished();
            }
        }

        static Callback _finishedCallback;

        AndroidJavaObject _download;
        long _id = 0;

        static void SetupBackendStatics()
        {
            Debug.Log("=========== ANDROID PLUGIN: SetupBackendStatics(): ");
            if (_backgroundDownloadClass == null)
            {
                Debug.Log("1. SetupBackendStatics(): tworze instacje klasy javy Background download");
                _backgroundDownloadClass = new AndroidJavaClass("com.unity3d.backgrounddownload.BackgroundDownload");
            }
            if (_finishedCallback == null)
            {
                Debug.Log("2. SetupBackendStatics(): tworze instacje receiver CompletionReceiver");

                _finishedCallback = new Callback();
                var receiver = new AndroidJavaClass("com.unity3d.backgrounddownload.CompletionReceiver");
                receiver.CallStatic("setCallback", _finishedCallback);
            }

            if (_playerClass == null)
            {
                Debug.Log("3. SetupBackendStatics(): tworze instacje unityplayera");

                _playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            }
        }

        internal BackgroundDownloadAndroid(BackgroundDownloadConfig config)
            : base(config)
        {
            
            Debug.Log("=========== ANDROID PLUGIN: BackgroundDownloadAndroid(config konstruktor): tworze obiekt BackgroundDownloadAndroid");

            SetupBackendStatics();
            string filePath = Path.Combine(Application.persistentDataPath, config.filePath);
            if (File.Exists(filePath))
            {
                Debug.Log("1. BackgroundDownloadAndroid(config konstruktor): Usuwam katalog pobran z filepath " + filePath);

                File.Delete(filePath);
            }
            else
            {
                Debug.Log("1. BackgroundDownloadAndroid(config konstruktor): Tworze katalog pobran z filepath " + filePath);

                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            
            Debug.Log("2. BackgroundDownloadAndroid(config konstruktor): Tworze polityke pobierania");

            string fileUri = "file://" + filePath;
            bool allowMetered = false;
            bool allowRoaming = false;
            switch (_config.policy)
            {
                case BackgroundDownloadPolicy.AllowMetered:
                    allowMetered = true;
                    break;
                case BackgroundDownloadPolicy.AlwaysAllow:
                    allowMetered = true;
                    allowRoaming = true;
                    break;
                default:
                    break;
            }
            Debug.Log("3. BackgroundDownloadAndroid(config konstruktor): Tworze natywny obiekt pobierania");

            _download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("create", config.url.AbsoluteUri, fileUri);

            _download.Call("setAllowMetered", allowMetered);
            _download.Call("setAllowRoaming", allowRoaming);
            if (config.requestHeaders != null)
                foreach (var header in config.requestHeaders)
                    if (header.Value != null)
                        foreach (var val in header.Value)
                            _download.Call("addRequestHeader", header.Key, val);
            var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            Debug.Log("4. BackgroundDownloadAndroid(config konstruktor): Ustawiam id pobierania");

            _id = _download.Call<long>("start", activity);
        }

        BackgroundDownloadAndroid(long id, AndroidJavaObject download)
        {
            Debug.Log("=========== ANDROID PLUGIN: BackgroundDownloadAndroid(id, download: konstruktor): tworze obiekt BackgroundDownloadAndroid");

            _id = id;
            _download = download;
            _config.url = QueryDownloadUri();
            _config.filePath = QueryDestinationPath();
            CheckFinished();
        }

        static BackgroundDownloadAndroid Recreate(long id)
        {
            Debug.Log("=========== ANDROID PLUGIN: Recreate(): Próbuje przywrocić pobieranie");

            try
            {
                SetupBackendStatics();
                var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
                var download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("recreate", activity, id);
                Debug.Log("=========== ANDROID PLUGIN: Recreate(): Czy moge przywrocić pobieranie? Object download status " + (download != null));

                if (download != null)
                {
                    return new BackgroundDownloadAndroid(id, download);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Failed to recreate background download with id {0}: {1}", id, e.Message));
            }

            return null;
        }

        Uri QueryDownloadUri()
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. QueryDownloadUri(): Pobieram uri pliku");

            return new Uri(_download.Call<string>("getDownloadUrl"));
        }

        string QueryDestinationPath()
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. QueryDestinationPath(): Pobieram destination path");

            string uri = _download.Call<string>("getDestinationUri");
            string basePath = Application.persistentDataPath;
            var pos = uri.IndexOf(basePath);
            pos += basePath.Length;
            if (uri[pos] == '/')
                ++pos;
            
            Debug.Log("1. QueryDestinationPath(): sciezka substringu "+uri.Substring(pos));

            return uri.Substring(pos);
        }

        string GetError()
        {
            return _download.Call<string>("getError");
        }

        void CheckFinished()
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. CheckFinished(): Sprawdzam czy pobrano plik" + _status);

            if (_status == BackgroundDownloadStatus.Downloading)
            {
                int status = _download.Call<int>("checkFinished");
                if (status == 1)
                {
                    _status = BackgroundDownloadStatus.Done;
                    Debug.Log("1. CheckFinished(): Status == 1 ? " + _status);
                }
                else if (status < 0)
                {
                    _status = BackgroundDownloadStatus.Failed;
                    Debug.Log("1. CheckFinished(): Status != 1 ERROR " + GetError());

                    _error = GetError();
                }
            }
        }

        void RemoveDownload()
        {
            _download.Call("remove");
        }

        public override bool keepWaiting { get { return _status == BackgroundDownloadStatus.Downloading; } }

        protected override float GetProgress()
        {
            return _download.Call<float>("getProgress");
        }

        protected override int GetFileSize()
        {
            return _download.Call<int>("getTotalFilesizeBytes");
        }

        public override void Dispose()
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. Dispose(): usuwam smieci ");

            RemoveDownload();
            base.Dispose();
        }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. LoadDownloads(): Laduje pobrane pliki ");

            var downloads = new Dictionary<string, BackgroundDownload>();
            var file = Path.Combine(Application.persistentDataPath, "unity_background_downloads.txt");
            if (File.Exists(file))
            {
                Debug.Log("1. LoadDownloads(): Plik z id pobieran istnieje ");

                foreach (var line in File.ReadAllLines(file))
                {
                    Debug.Log("2. LoadDownloads(): Czytam pobrane id z pliku" + line);

                    if (!string.IsNullOrEmpty(line))
                    {
                        Debug.Log("3. LoadDownloads(): Odtwarzam pobieranie o id" + long.Parse(line));

                        long id = long.Parse(line);
                        var dl = Recreate(id);

                        if (dl != null)
                        {
                            Debug.Log("4. LoadDownloads(): Udało się odtworzyc pobieranie");

                            downloads[dl.config.filePath] = dl;
                        }
                    }
                }
            }

            Debug.Log("5. LoadDownloads() zapisuje dane na temat id pobieran  ");

            // some loads might have failed, save the actual state
            SaveDownloads(downloads);
            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
            Debug.Log("=========== ANDROID PLUGIN: 1. SaveDownloads(): Zapis id pobieran ");

            var file = Path.Combine(Application.persistentDataPath, "unity_background_downloads.txt");
            if (downloads.Count > 0)
            {
                Debug.Log("2. SaveDownloads(): Aktywne pobierania "+downloads.Count);

                var ids = new string[downloads.Count];
                int i = 0;
                foreach (var dl in downloads)
                {
                    ids[i++] = ((BackgroundDownloadAndroid)dl.Value)._id.ToString();
                }
                File.WriteAllLines(file, ids);
            }
            else if (File.Exists(file))
            {
                Debug.Log("2. SaveDownloads(): Usuwam plik z pobieraniami ");
                File.Delete(file);
            }
        }
    }

}

#endif
