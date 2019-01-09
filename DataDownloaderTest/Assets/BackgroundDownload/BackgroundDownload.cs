﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadEditor;
#elif UNITY_ANDROID
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadAndroid;
#elif UNITY_IOS
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadiOS;
#elif UNITY_WSA_10_0
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadUWP;
#endif

namespace Unity.Networking
{
    public enum BackgroundDownloadPolicy
    {
        Default = 0,
        UnrestrictedOnly = 1,  // Only Wi-Fi
        AllowMetered = 2,      // Allow Mobile
        AlwaysAllow = 3,       // Allow Roaming
    }

    public struct BackgroundDownloadConfig
    {
        public Uri url;
        public string filePath;
        public BackgroundDownloadPolicy policy;
        public Dictionary<string, List<string>> requestHeaders;

        public void AddRequestHeader(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Header name cannot be empty");
            if (value == null)
                throw new ArgumentNullException("Header value cannot be null");
            if (requestHeaders == null)
                requestHeaders = new Dictionary<string, List<string>>();
            List<string> values;
            if (requestHeaders.TryGetValue(name, out values))
                values.Add(value);
            else
            {
                values = new List<string>();
                values.Add(value);
                requestHeaders[name] = values;
            }
        }
    }

    public enum BackgroundDownloadStatus
    {
        Downloading = 0,
        Done = 1,
        Failed = 2,
    }

    public abstract class BackgroundDownload
        : CustomYieldInstruction
        , IDisposable
    {
        protected static Dictionary<string, BackgroundDownload> _downloads;

        public static BackgroundDownload[] backgroundDownloads
        {
            get
            {
                LoadDownloads();
                var ret = new BackgroundDownload[_downloads.Count];
                int i = 0;
                foreach (var download in _downloads)
                    ret[i++] = download.Value;
                return ret;
            }
        }

        protected BackgroundDownloadConfig _config;
        protected BackgroundDownloadStatus _status = BackgroundDownloadStatus.Downloading;
        protected string _error;

        public static BackgroundDownload Start(Uri url, String filePath)
        {
            var config = new BackgroundDownloadConfig();
            config.url = url;
            config.filePath = filePath;
            return Start(config);
        }

        public static BackgroundDownload Start(BackgroundDownloadConfig config)
        {
            
            Debug.Log("=========== BackgroundDownloadRoot : Start() ");
            Debug.Log("1.Start() Sprawdzam pobierane pliki z LoadDownloads() ");

            LoadDownloads();

            if (_downloads.ContainsKey(config.filePath))
            {
                throw new ArgumentException("FATAL ERROR : Start() : Download of this file is already present");
            }
            
            Debug.Log("2.Start() Tworze instancje pobierania zależnie od platformy");

            var download = new BackgroundDownloadimpl(config);
            
            Debug.Log("3.Start() Dodaje pobieranie ze sciezka " + config.filePath + " download status " + download.status);

            _downloads.Add(config.filePath, download);
            
            Debug.Log("4.Start() zapisuje pobieranie");

            SaveDownloads();
            
            Debug.Log("5.Start() Ok - zwracam pobieranie" );

            return download;
        }

        protected BackgroundDownload()
        {
        }

        protected BackgroundDownload(BackgroundDownloadConfig config)
        {
            _config = config;
            switch (_config.policy)
            {
                case BackgroundDownloadPolicy.UnrestrictedOnly:
                case BackgroundDownloadPolicy.AllowMetered:
                case BackgroundDownloadPolicy.AlwaysAllow:
                    break;
                case BackgroundDownloadPolicy.Default:
                default:
                    _config.policy = BackgroundDownloadPolicy.AllowMetered;
                    break;
            }
        }

        public BackgroundDownloadConfig config { get { return _config; } }

        public virtual BackgroundDownloadStatus status { get { return _status; } }

        public string error { get { return _error; } }

        public float progress { get { return GetProgress(); } }

        protected abstract float GetProgress();

        public virtual void Dispose()
        {
            Debug.Log("=========== BackgroundDownloadRoot : Dispose() usuwam pobieranie ze slownika downloads " + _config.filePath);

            _downloads.Remove(_config.filePath);
            
            Debug.Log("1. Dispose() - zapisuje pliki przed usunieciem");

            SaveDownloads();
            
            Debug.Log("1. Dispose() - Sprawdzam status pobierania " + _status);

            if (_status == BackgroundDownloadStatus.Downloading)
            {
                _status = BackgroundDownloadStatus.Failed;
                _error = "Aborted";
            }
        }

        static void LoadDownloads()
        {
            Debug.Log("=========== BackgroundDownloadRoot : LoadDownloads() ");

            if (_downloads == null)
            {
                Debug.Log("1. LoadDownloads() Aktualne pobieranie puste - wczytuje dostepne / tworze nowe");

                _downloads = BackgroundDownloadimpl.LoadDownloads();
            }
        }

        static void SaveDownloads()
        {
            Debug.Log("=========== BackgroundDownloadRoot : SaveDownloads() ");

            BackgroundDownloadimpl.SaveDownloads(_downloads);
        }
    }

}
