using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Editor;
using Xv2CoreLib.EMB_CLASS;
using Pfim;
using System.Diagnostics;

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

            Texture2D texture = ConvertToTexture2D_Native(embEntry, name, graphicsDevice);

            if(texture == null)
            {
                Log.Add($"Native loading failed for texture {embEntry.Name}, in {name}", LogType.Debug);
                //Use fallback method if native loading failed. This method is very slow as it decodes the DDS on the CPU, makes a WriteableBitmap, and then converts that into a Texture2D
                texture = ConvertToTexture2D_fallback(embEntry, name, graphicsDevice);
            }

            return texture;
        }

        private static Texture2D ConvertToTexture2D_Pfim(EmbEntry embEntry, string name, GraphicsDevice graphicsDevice, DDS_File dds)
        {
            //Based on example at https://github.com/nickbabcock/Pfim/blob/master/src/Pfim.MonoGame/MyGame.cs

            Dds image;
            using (MemoryStream ms = new MemoryStream(embEntry.Data))
            {
                image = Pfimage.FromStream(ms) as Dds;
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
            else
            {
                newData = image.Data;
            }

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
                default:
                    Log.Add($"TextureConverter: unimplemented format {image.Format} on {embEntry.Name}", LogType.Debug);
                    return null;
            }

            return newTexture;
        }

        private static Texture2D ConvertToTexture2D_Native(EmbEntry embEntry, string name = null, GraphicsDevice graphicsDevice = null)
        {
            if (graphicsDevice == null)
                graphicsDevice = SceneManager.MainGameBase.GraphicsDevice;

#if !DEBUG
            try
#endif
            {
                //If not a DDS file, then use fallback method to load format
                if (!DDS_File.IsDds(embEntry.Data)) return null;

                DDS_File dds = new DDS_File(embEntry.Data);

                SurfaceFormat format = dds.GetSurfaceFormat();

                if (format == (SurfaceFormat)(-1))
                {
                    //Unsupported DDS format, try to use Pfim instead
                    
                    try
                    {
                        return ConvertToTexture2D_Pfim(embEntry, name, graphicsDevice, dds);
                    }
                    catch (Exception ex)
                    {
                        Log.Add("ConvertToTexture2D_Native: Pfim errored out - " + ex.Message, LogType.Debug);
                        return null;
                    }
                }

                int textureSize = dds.GetTextureSize();
                int dataSize = dds.GetFullTextureSize();

                if (dds.Header.IsCubemap())
                {
                    //Some stage EMBs have cubemaps in them... that are not even used by the shaders?
                    Log.Add($"ConvertToTexture2D_Native: input texture is actually a cubemap ({embEntry.Name}, {name})", LogType.Debug);
                }

                Texture2D texture2D = new Texture2D(graphicsDevice, dds.Header.Width, dds.Header.Height, false, format);
                texture2D.SetData(embEntry.Data, dds.DataStartOffset, textureSize);

                return texture2D;
            }
#if !DEBUG
            catch
            {
                return null;
            }
#endif
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
            
            texture.SetData(data);

            if (!string.IsNullOrWhiteSpace(name))
                texture.Name = name;

            return texture;
        }

        public static TextureCube ConvertToTextureCube(EmbEntry embEntry, string name = null, GraphicsDevice graphicsDevice = null)
        {
            if (graphicsDevice == null)
                graphicsDevice = SceneManager.MainGameBase.GraphicsDevice;

            DDS_File dds = new DDS_File(embEntry.Data);

            SurfaceFormat format = dds.GetSurfaceFormat();

            if (format == (SurfaceFormat)(-1))
            {
                //Unsupported format
                string error = $"This DDS format is unsupported for a cubemap (Flags={dds.Header.PixelFormat.Flags}, FourCC={dds.Header.PixelFormat.FourCC}";

                if (dds.Header.PixelFormat.FourCC == CompressionAlgorithm.DX10)
                    error += $", DxgiFormat={dds.HeaderDX10.DXGIFormat})";
                else
                    error += ")";

                throw new InvalidDataException(error);
            }

            int textureSize = dds.GetTextureSize();
            int dataSize = dds.GetFullTextureSize();

            if (!dds.Header.IsCubemap())
            {
                throw new InvalidDataException("Texture was not a cubemap!");
            }

            TextureCube textureCube = new TextureCube(graphicsDevice, dds.Header.Width, mipMap: false, format);

            for (int i = 0; i < 6; i++)
            {
                int faceDataIdx = dds.DataStartOffset + (i * dataSize); //idx of face data in dds file
                textureCube.SetData((CubeMapFace)i, embEntry.Data, faceDataIdx, textureSize);
            }

            return textureCube;
        }

    }
}
