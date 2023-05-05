using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Xv2CoreLib.EMB_CLASS;
using Pfim;
using System.Windows.Media.Imaging;

namespace XenoKit.Engine.Textures
{
    public static class TextureLoader
    {
        public static Texture2D ConvertToTexture2D(string path, GraphicsDevice graphicsDevice)
        {
            byte[] bytes = File.ReadAllBytes(path);
            EmbEntry embEntry = new EmbEntry();
            embEntry.Data = bytes;

            return ConvertToTexture2D(embEntry, Path.GetFileName(path), graphicsDevice);
        }

        public static Texture2D ConvertToTexture2D(EmbEntry embEntry, string name = null, GraphicsDevice graphicsDevice = null)
        {
            if (graphicsDevice == null)
                graphicsDevice = SceneManager.MainGameBase.GraphicsDevice;

            //Taken from example at https://github.com/nickbabcock/Pfim/blob/master/src/Pfim.MonoGame/MyGame.cs
            try
            {

                IImage image;
                using (MemoryStream ms = new MemoryStream(embEntry.Data))
                {
                    image = Pfim.Pfim.FromStream(ms);
                }

                byte[] newData;

                // Since mono game can't handle data with line padding in a stride
                // we create an stripped down array if any padding is detected
                var tightStride = image.Width * image.BitsPerPixel / 8;
                if (image.Stride != tightStride)
                {
                    newData = new byte[image.Height * tightStride];
                    for (int i = 0; i < image.Height; i++)
                    {
                        Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
                    }
                }
                //else if(image.MipMaps.Length > 1)
                //{
                //    newData = new byte[image.MipMaps[0].DataLen];
                //    Buffer.BlockCopy(image.Data, image.MipMaps[0].DataOffset, newData, 0, newData.Length);
                //}
                else
                {
                    newData = image.Data;
                }

                // I believe mono game core is limited in its texture support
                // so we're assuming 32bit data format is needed. One can always
                // upscale 24bit / 16bit / 15bit data (not shown in sample).
                var newTexture = new Texture2D(graphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);

                if (!string.IsNullOrWhiteSpace(name))
                    newTexture.Name = name;

                switch (image.Format)
                {
                    case ImageFormat.Rgba32:
                        // Flip red and blue color channels.
                        for (int i = 0; i < newData.Length; i += 4)
                        {
                            var temp = newData[i + 2];
                            newData[i + 2] = newData[i];
                            newData[i] = temp;
                        }

                        newTexture.SetData(newData);
                        break;
                }

                return newTexture;
            }
            catch
            {
                return ConvertToTexture2D_fallback(embEntry, name);
            }
        }

        private static Texture2D ConvertToTexture2D_fallback(EmbEntry embEntry, string name = null, GraphicsDevice graphicsDevice = null)
        {
            if (graphicsDevice == null)
                graphicsDevice = SceneManager.MainGameBase.GraphicsDevice;

            Texture2D texture = new Texture2D(graphicsDevice, embEntry.Texture.PixelWidth, embEntry.Texture.PixelHeight);
            byte[] data = embEntry.Texture.ToByteArray();

            //Swap color positions
            for(int i = 0; i < data.Length; i += 4)
            {
                byte a = data[i];
                byte r = data[i + 1];
                byte g = data[i + 2];
                byte b = data[i + 3];
                data[i] = g;
                data[i + 1] = r;
                data[i + 2] = a;
                data[i + 3] = b;
            }

            //XNA blacks out any pixels with an alpha of zero.
            for (int i = 0; i < data.Length; i += 4)
            {
                if (data[i + 3] == 0)
                {
                    data[i + 0] = 0;
                    data[i + 1] = 0;
                    data[i + 2] = 0;
                }
            }
            
            texture.SetData(data);

            if (!string.IsNullOrWhiteSpace(name))
                texture.Name = name;

            return texture;
        }
         
    }
}
