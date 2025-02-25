using NewHorizons.Utility.OWML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NewHorizons.Utility.Files;

/// <summary>
/// Modified from https://stackoverflow.com/a/69141085/9643841
/// </summary>
public class SlideReelAsyncImageLoader
{
    public List<(int index, string path)> PathsToLoad { get; private set; } = new();

    public class ImageLoadedEvent : UnityEvent<Texture2D, int, string> { }
    public ImageLoadedEvent imageLoadedEvent = new();

    public bool FinishedLoading { get; private set; }
    private int _loadedCount = 0;

    // TODO: set up an optional “StartLoading” and “StartUnloading” condition on AsyncTextureLoader,
    // and make use of that for at least for projector stuff (require player to be in the same sector as the slides
    // for them to start loading, and unload when the player leaves)
    //   also remember this for ship logs!!! lol

    private bool _started;
    private bool _clamp;

    public void Start(bool clamp)
    {
        if (_started) return;

        _clamp = clamp;

        _started = true;

        if (SingletonSlideReelAsyncImageLoader.Instance == null)
        {
            Main.Instance.gameObject.AddComponent<SingletonSlideReelAsyncImageLoader>();
        }

        NHLogger.LogVerbose("Loading new slide reel");
        imageLoadedEvent.AddListener(OnImageLoaded);
        SingletonSlideReelAsyncImageLoader.Instance.Load(this);
    }

    private void OnImageLoaded(Texture texture, int index, string originalPath)
    {
        _loadedCount++;

        if (_loadedCount >= PathsToLoad.Count)
        {
            NHLogger.LogVerbose($"Finished loading all textures for a slide reel (one was {PathsToLoad.FirstOrDefault()}");
            FinishedLoading = true;
        }
    }

    private IEnumerator DownloadTexture(string url, int index)
    {
        var key = ImageUtilities.GetKey(url);
        if (ImageUtilities.CheckCachedTexture(key, out var existingTexture))
        {
            NHLogger.LogVerbose($"Already loaded image {index}:{url} with key {key}");
            imageLoadedEvent?.Invoke((Texture2D)existingTexture, index, url);
            yield break;
        }

        using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);

        yield return uwr.SendWebRequest();

        var hasError = uwr.error != null && uwr.error != "";

        if (hasError)
        {
            NHLogger.LogError($"Failed to load {index}:{url} - {uwr.error}");
        }
        else
        {
            var texture = DownloadHandlerTexture.GetContent(uwr);
            texture.name = key;
            if (_clamp)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            if (ImageUtilities.CheckCachedTexture(key, out existingTexture))
            {
                // the image could be loaded by something else by the time we're done doing async stuff
                NHLogger.LogVerbose($"Already loaded image {index}:{url}");
                GameObject.Destroy(texture);
                texture = (Texture2D)existingTexture;
            }
            else
            {
                ImageUtilities.TrackCachedTexture(key, texture);
            }

            var time = DateTime.Now;
            imageLoadedEvent?.Invoke(texture, index, url);
            NHLogger.LogVerbose($"Slide reel event took: {(DateTime.Now - time).TotalMilliseconds}ms");
        }
    }

    private class SingletonSlideReelAsyncImageLoader : MonoBehaviour
    {
        public static SingletonSlideReelAsyncImageLoader Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene _)
        {
            StopAllCoroutines();
        }

        public void Load(SlideReelAsyncImageLoader loader)
        {
            // Delay at least one frame to let things subscribe to the event before it fires
            Delay.FireOnNextUpdate(() =>
            {
                foreach (var (index, path) in loader.PathsToLoad)
                {
                    NHLogger.LogVerbose($"Loaded slide reel {index} of {loader.PathsToLoad.Count}");

                    StartCoroutine(loader.DownloadTexture(path, index));
                }
            });
        }
    }
}