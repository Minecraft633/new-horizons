using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace NewHorizons.Utility
{
    public static class ImageUtilities
    {
        private static Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();
        private static List<Texture2D> _generatedTextures = new List<Texture2D>();

        public static bool IsTextureLoaded(IModBehaviour mod, string filename)
        {
            var path = Path.Combine(mod.ModHelper.Manifest.ModFolderPath, filename);
            return _loadedTextures.ContainsKey(path);
        }

        public static Texture2D GetTexture(IModBehaviour mod, string filename, bool useMipmaps = true, bool wrap = false)
        {
            // Copied from OWML but without the print statement lol
            var path = Path.Combine(mod.ModHelper.Manifest.ModFolderPath, filename);
            if (_loadedTextures.ContainsKey(path))
            {
                Logger.LogVerbose($"Already loaded image at path: {path}");
                return _loadedTextures[path];
            }

            Logger.LogVerbose($"Loading image at path: {path}");
            try
            {
                var data = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, useMipmaps);
                texture.name = Path.GetFileNameWithoutExtension(path);
                texture.wrapMode = wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                texture.LoadImage(data);
                _loadedTextures.Add(path, texture);

                return texture;
            }
            catch (Exception ex)
            {
                // Half the time when a texture doesn't load it doesn't need to exist so just log verbose
                Logger.LogVerbose($"Exception thrown while loading texture [{filename}]:\n{ex}");
                return null;
            }
        }

        public static void DeleteTexture(IModBehaviour mod, string filename, Texture2D texture)
        {
            var path = Path.Combine(mod.ModHelper.Manifest.ModFolderPath, filename);
            if (_loadedTextures.ContainsKey(path))
            {
                if (_loadedTextures[path] == texture)
                {
                    _loadedTextures.Remove(path);
                    UnityEngine.Object.Destroy(texture);
                }
            }

            UnityEngine.Object.Destroy(texture);
        }

        public static void ClearCache()
        {
            Logger.LogVerbose("Clearing image cache");

            foreach (var texture in _loadedTextures.Values)
            {
                if (texture == null) continue;
                UnityEngine.Object.Destroy(texture);
            }
            _loadedTextures.Clear();

            foreach (var texture in _generatedTextures)
            {
                if (texture == null) continue;
                UnityEngine.Object.Destroy(texture);
            }
            _generatedTextures.Clear();
        }

        public static Texture2D Invert(IModBehaviour mod, Texture2D texture)
        {
            var name = texture.name + "Inverted";

            var path = Path.Combine(mod.ModHelper.Manifest.ModFolderPath, "cache", "invert", $"{name}.png");

            Texture2D newTexture;

            if(File.Exists(path))
            {
                newTexture = new(1, 1, texture.format, false);
                newTexture.LoadImage(File.ReadAllBytes(path));
            }
            else
            {
                var pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var x = i % texture.width;
                    var y = (int)Mathf.Floor(i / texture.height);

                    // Needs a black border
                    if (x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1)
                    {
                        pixels[i].r = 1;
                        pixels[i].g = 1;
                        pixels[i].b = 1;
                        pixels[i].a = 1;
                    }
                    else
                    {
                        pixels[i].r = 1f - pixels[i].r;
                        pixels[i].g = 1f - pixels[i].g;
                        pixels[i].b = 1f - pixels[i].b;
                    }
                }

                newTexture = new(texture.width, texture.height);
                newTexture.SetPixels(pixels);
                newTexture.Apply();

                var filePath = new FileInfo(path);
                filePath.Directory.Create();
                File.WriteAllBytes(path, newTexture.EncodeToPNG());
            }

            newTexture.name = name;
            newTexture.wrapMode = texture.wrapMode;
            _generatedTextures.Add(newTexture);

            return newTexture;
        }

        public static Texture2D MakeReelTexture(Texture2D[] textures)
        {
            var size = 256;

            var texture = (new Texture2D(size * 4, size * 4, TextureFormat.ARGB32, false));
            texture.name = "SlideReelAtlas";

            var fillPixels = new Color[size * size * 4 * 4];
            for (int xIndex = 0; xIndex < 4; xIndex++)
            {
                for (int yIndex = 0; yIndex < 4; yIndex++)
                {
                    int index = yIndex * 4 + xIndex;
                    var srcTexture = index < textures.Length ? textures[index] : null;

                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            var colour = Color.black;

                            if (srcTexture)
                            {
                                var srcX = i * srcTexture.width / (float)size;
                                var srcY = j * srcTexture.height / (float)size;
                                if (srcX >= 0 && srcX < srcTexture.width && srcY >= 0 && srcY < srcTexture.height)
                                {
                                    colour = srcTexture.GetPixel((int)srcX, (int)srcY);
                                }
                            }

                            var x = xIndex * size + i;
                            // Want it to start from the first row from the bottom then go down then modulo around idk
                            // 5 because no pos mod idk
                            var y = ((5 - yIndex) % 4) * size + j;

                            var pixelIndex = x + y * (size * 4);

                            if (pixelIndex < fillPixels.Length && pixelIndex >= 0) fillPixels[pixelIndex] = colour;
                        }
                    }
                }
            }

            texture.SetPixels(fillPixels);
            texture.Apply();

            _generatedTextures.Add(texture);

            return texture;
        }

        public static Texture2D MakeOutline(Texture2D texture, Color color, int thickness)
        {
            var outline = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            outline.name = texture.name + "Outline";
            var outlinePixels = new Color[texture.width * texture.height];
            var pixels = texture.GetPixels();

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    var fillColor = new Color(0, 0, 0, 0);

                    if (pixels[x + y * texture.width].a == 1 && CloseToTransparent(pixels, texture.width, texture.height, x, y, thickness))
                    {
                        fillColor = color;
                    }
                    outlinePixels[x + y * texture.width] = fillColor;
                }
            }

            outline.SetPixels(outlinePixels);
            outline.Apply();

            outline.wrapMode = texture.wrapMode;

            _generatedTextures.Add(outline);

            return outline;
        }

        private static bool CloseToTransparent(Color[] pixels, int width, int height, int x, int y, int thickness)
        {
            // Check nearby
            var minX = Math.Max(0, x - thickness / 2);
            var minY = Math.Max(0, y - thickness / 2);
            var maxX = Math.Min(width, x + thickness / 2);
            var maxY = Math.Min(height, y + thickness / 2);

            for (int i = minX; i < maxX; i++)
            {
                for (int j = minY; j < maxY; j++)
                {
                    if (pixels[i + j * width].a < 1) return true;
                }
            }
            return false;
        }

        public static Texture2D TintImage(Texture2D image, Color tint)
        {
            if(image == null)
            {
                Logger.LogError($"Tried to tint null image");
                return null;
            }

            var pixels = image.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r *= tint.r;
                pixels[i].g *= tint.g;
                pixels[i].b *= tint.b;
            }

            var newImage = new Texture2D(image.width, image.height);
            newImage.name = image.name + "Tinted";
            newImage.SetPixels(pixels);
            newImage.Apply();

            newImage.wrapMode = image.wrapMode;

            _generatedTextures.Add(newImage);

            return newImage;
        }

        public static Texture2D LerpGreyscaleImage(Texture2D image, Color lightTint, Color darkTint)
        {
            var pixels = image.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r = Mathf.Lerp(darkTint.r, lightTint.r, pixels[i].r);
                pixels[i].g = Mathf.Lerp(darkTint.g, lightTint.g, pixels[i].g);
                pixels[i].b = Mathf.Lerp(darkTint.b, lightTint.b, pixels[i].b);
            }

            var newImage = new Texture2D(image.width, image.height);
            newImage.name = image.name + "LerpedGrayscale";
            newImage.SetPixels(pixels);
            newImage.Apply();

            newImage.wrapMode = image.wrapMode;

            _generatedTextures.Add(newImage);

            return newImage;
        }

        public static Texture2D ClearTexture(int width, int height, bool wrap = false)
        {
            var tex = (new Texture2D(1, 1, TextureFormat.ARGB32, false));
            tex.name = "Clear";
            var fillColor = Color.clear;
            var fillPixels = new Color[tex.width * tex.height];
            for (int i = 0; i < fillPixels.Length; i++)
            {
                fillPixels[i] = fillColor;
            }
            tex.SetPixels(fillPixels);
            tex.Apply();

            tex.wrapMode = wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            _generatedTextures.Add(tex);

            return tex;
        }

        public static Texture2D CanvasScaled(Texture2D src, int width, int height)
        {
            var tex = (new Texture2D(width, height, TextureFormat.ARGB32, false));
            tex.name = src.name + "CanvasScaled";
            var fillPixels = new Color[tex.width * tex.height];
            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.height; j++)
                {
                    var x = i + (src.width - width) / 2;
                    var y = j + (src.height - height) / 2;

                    var colour = Color.black;
                    if (x < src.width && y < src.height) colour = src.GetPixel(i, j);

                    fillPixels[i + j * tex.width] = colour;
                }
            }
            tex.SetPixels(fillPixels);
            tex.Apply();

            tex.wrapMode = src.wrapMode;

            _generatedTextures.Add(tex);

            return tex;
        }

        public static Color GetAverageColor(Texture2D src)
        {
            var pixels = src.GetPixels32();
            var r = 0f;
            var g = 0f;
            var b = 0f;
            var length = pixels.Length;
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                r += (float)color.r / length;
                g += (float)color.g / length;
                b += (float)color.b / length;
            }

            return new Color(r / 255, g / 255, b / 255);
        }
        public static Texture2D MakeSolidColorTexture(int width, int height, Color color)
        {
            var pixels = new Color[width*height];
 
            for(int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
 
            var newTexture = new Texture2D(width, height);
            newTexture.SetPixels(pixels);
            newTexture.Apply();
            return newTexture;
        }

        public static Sprite GetButtonSprite(JoystickButton button) => GetButtonSprite(ButtonPromptLibrary.SharedInstance.GetButtonTexture(button));
        public static Sprite GetButtonSprite(KeyCode key) => GetButtonSprite(ButtonPromptLibrary.SharedInstance.GetButtonTexture(key));
        private static Sprite GetButtonSprite(Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            sprite.name = texture.name;
            return sprite;
        }

        // Modified from https://stackoverflow.com/a/69141085/9643841
        public class AsyncImageLoader : MonoBehaviour
        {
            public List<(int index, string path)> PathsToLoad { get; private set; } = new ();

            public class ImageLoadedEvent : UnityEvent<Texture2D, int> { }
            public ImageLoadedEvent imageLoadedEvent = new ();

            private readonly object _lockObj = new();

            public bool FinishedLoading { get; private set; }
            private int _loadedCount = 0;

            // TODO: set up an optional “StartLoading” and “StartUnloading” condition on AsyncTextureLoader,
            // and make use of that for at least for projector stuff (require player to be in the same sector as the slides
            // for them to start loading, and unload when the player leaves)

            void Start()
            {
                imageLoadedEvent.AddListener(OnImageLoaded);
                foreach (var (index, path) in PathsToLoad)
                {
                    StartCoroutine(DownloadTexture(path, index));
                }
            }

            private void OnImageLoaded(Texture texture, int index)
            {
                lock (_lockObj)
                {
                    _loadedCount++;

                    if (_loadedCount >= PathsToLoad.Count)
                    {
                        Logger.LogVerbose($"Finished loading all textures for {gameObject.name} (one was {PathsToLoad.FirstOrDefault()}");
                        FinishedLoading = true;
                    }
                }
            }

            IEnumerator DownloadTexture(string url, int index)
            {
                lock(_loadedTextures)
                {
                    if (_loadedTextures.ContainsKey(url))
                    {
                        Logger.LogVerbose($"Already loaded image {index}:{url}");
                        var texture = _loadedTextures[url];
                        imageLoadedEvent?.Invoke(texture, index);
                        yield break;
                    }
                }

                using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);

                yield return uwr.SendWebRequest();

                var hasError = uwr.error != null && uwr.error != "";

                if (hasError) 
                {
                    Logger.LogError($"Failed to load {index}:{url} - {uwr.error}");
                }
                else
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    texture.name = Path.GetFileNameWithoutExtension(url);

                    lock(_loadedTextures)
                    {
                        if (_loadedTextures.ContainsKey(url))
                        {
                            Logger.LogVerbose($"Already loaded image {index}:{url}");
                            Destroy(texture);
                            texture = _loadedTextures[url];
                        }
                        else
                        {
                            _loadedTextures.Add(url, texture);
                        }

                        imageLoadedEvent?.Invoke(texture, index);
                    }
                }
            }
        }
    }
}
