using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace XenoKit.Engine.Textures
{
    public class DDS_File
    {
        public DdsHeader Header { get; private set; }
        public DdsHeader10 HeaderDX10 { get; private set; }

        public int DataStartOffset { get; private set; }

        public DDS_File(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    Header = DdsHeader.ReadHeader(reader);

                    if(Header.PixelFormat.Flags.HasFlag(DdsPixelFormatFlags.Fourcc) && Header.PixelFormat.FourCC == CompressionAlgorithm.DX10)
                    {
                        DataStartOffset = 148;
                        HeaderDX10 = DdsHeader10.ReadHeader(reader);
                    }
                    else
                    {
                        DataStartOffset = 128;
                    }
                }
            }
        }

        public static bool IsDds(byte[] data)
        {
            return BitConverter.ToUInt32 (data, 0) == DdsHeader.DDS_MAGIC;
        }
    
        public SurfaceFormat GetSurfaceFormat()
        {
            if (Header.PixelFormat.Flags.HasFlag(DdsPixelFormatFlags.Fourcc))
            {
                switch (Header.PixelFormat.FourCC)
                {
                    case CompressionAlgorithm.D3DFMT_DXT1:
                        return SurfaceFormat.Dxt1;
                    case CompressionAlgorithm.D3DFMT_DXT3:
                        return SurfaceFormat.Dxt3;
                    case CompressionAlgorithm.D3DFMT_DXT5:
                        return SurfaceFormat.Dxt5;
                    case CompressionAlgorithm.DX10:
                        switch (HeaderDX10.DXGIFormat)
                        {
                            case DxgiFormat.BC1_UNORM:
                            case DxgiFormat.BC1_UNORM_SRGB:
                                return SurfaceFormat.Dxt1;

                            case DxgiFormat.BC2_UNORM:
                            case DxgiFormat.BC2_UNORM_SRGB:
                                return SurfaceFormat.Dxt3;

                            case DxgiFormat.BC3_UNORM:
                            case DxgiFormat.BC3_UNORM_SRGB:
                                return SurfaceFormat.Dxt5;

                            case DxgiFormat.BC4_UNORM:
                                return SurfaceFormat.Dxt1;

                            case DxgiFormat.BC5_UNORM:
                                return SurfaceFormat.Dxt5;

                            default:
                                return (SurfaceFormat)(-1);
                        }
                }
            }

            return (SurfaceFormat)(-1); //Unsupported
        }

        /// <summary>
        /// Returns complete size of the texture, including all mipmaps.
        /// </summary>
        public int GetFullTextureSize()
        {
            int size = GetTextureSize();

            if (Header.Flags.HasFlag(DdsFlags.MipMapCount))
            {
                int height = Header.Height;
                int width = Header.Width;

                for (int mip = 0; mip < Header.MipMapCount - 1; mip++)
                {
                    //if (height == 1 || width == 1) break;
                    height /= 2;
                    width /= 2;
                    size += GetTextureSize(width, height);
                }
            }

            return size;
        }

        public int GetTextureSize()
        {
            return GetTextureSize(Header.Width, Header.Height);
        }

        public int GetTextureSize(int width, int height)
        {
            if (Header.PixelFormat.Flags.HasFlag(DdsPixelFormatFlags.Fourcc))
            {
                switch (Header.PixelFormat.FourCC)
                {
                    case CompressionAlgorithm.BC4S:
                    case CompressionAlgorithm.BC4U:
                    case CompressionAlgorithm.D3DFMT_DXT1:
                        return (int)Math.Max(1, Math.Ceiling((double)width / 4)) * (int)Math.Max(1, Math.Ceiling((double)height / 4)) * 8;
                    case CompressionAlgorithm.D3DFMT_DXT3:
                    case CompressionAlgorithm.D3DFMT_DXT5:
                    case CompressionAlgorithm.BC5S:
                    case CompressionAlgorithm.BC5U:
                        return (int)Math.Max(1, Math.Ceiling((double)width / 4)) * (int)Math.Max(1, Math.Ceiling((double)height / 4)) * 16;
                    case CompressionAlgorithm.DX10:
                        switch (HeaderDX10.DXGIFormat)
                        {
                            case DxgiFormat.BC1_TYPELESS: //DXT1
                            case DxgiFormat.BC1_UNORM:
                            case DxgiFormat.BC1_UNORM_SRGB:
                            case DxgiFormat.BC4_SNORM:
                            case DxgiFormat.BC4_TYPELESS:
                            case DxgiFormat.BC4_UNORM:
                                return (int)Math.Max(1, Math.Ceiling((double)width / 4)) * (int)Math.Max(1, Math.Ceiling((double)height / 4)) * 8;
                            case DxgiFormat.BC2_TYPELESS: //DXT3
                            case DxgiFormat.BC2_UNORM:
                            case DxgiFormat.BC2_UNORM_SRGB:
                            case DxgiFormat.BC3_TYPELESS: //DXT5
                            case DxgiFormat.BC3_UNORM:
                            case DxgiFormat.BC3_UNORM_SRGB:
                            case DxgiFormat.BC5_UNORM:
                            case DxgiFormat.BC5_TYPELESS:
                            case DxgiFormat.BC5_SNORM:
                            //case DxgiFormat.BC7_TYPELESS:
                            //case DxgiFormat.BC7_UNORM:
                            //case DxgiFormat.BC7_UNORM_SRGB:
                                return (int)Math.Max(1, Math.Ceiling((double)width / 4)) * (int)Math.Max(1, Math.Ceiling((double)height / 4)) * 16;
                            default:
                                return 0;
                        }
                    default:
                        return 0;
                }

            }

            return 0;
        }

    }

    public struct DdsHeader
    {
        public const uint DDS_MAGIC = 542327876;
        public const int SIZE = 124;

        private int Size;
        public DdsFlags Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;
        public int[] Reserved1;
        public DdsPixelFormat PixelFormat;
        public int Caps;
        public int Caps2;
        public int Caps3;
        public int Caps4;
        public int Reserved2;

        public bool IsCubemap() => (Caps2 & 0x00000200) != 0; // DDSCAPS2_CUBEMAP flag

        public static DdsHeader ReadHeader(BinaryReader reader)
        {
            // Validate DDS Magic Number
            int magic = reader.ReadInt32();
            if (magic != DDS_MAGIC)
                throw new Exception("Invalid DDS file");

            // Read the DDS header
            DdsHeader header = new DdsHeader
            {
                Size = reader.ReadInt32(),
                Flags = (DdsFlags)reader.ReadUInt32(),
                Height = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                PitchOrLinearSize = reader.ReadInt32(),
                Depth = reader.ReadInt32(),
                MipMapCount = reader.ReadInt32(),
                Reserved1 = new int[11]
            };

            if (header.Size != SIZE)
                throw new InvalidDataException($"DDS header size mismatch (expected {SIZE}, but is {header.Size})");

            for (int i = 0; i < 11; i++)
                header.Reserved1[i] = reader.ReadInt32();

            // Read Pixel Format
            header.PixelFormat = new DdsPixelFormat
            {
                Size = reader.ReadInt32(),
                Flags = (DdsPixelFormatFlags)reader.ReadUInt32(),
                FourCC = (CompressionAlgorithm)reader.ReadUInt32(),
                RGBBitCount = reader.ReadInt32(),
                RBitMask = reader.ReadInt32(),
                GBitMask = reader.ReadInt32(),
                BBitMask = reader.ReadInt32(),
                ABitMask = reader.ReadInt32()
            };

            if(header.PixelFormat.Size != 32)
                throw new InvalidDataException($"DDS PixelFormat size mismatch (expected 32, but is {header.PixelFormat.Size})");

            // Read Caps
            header.Caps = reader.ReadInt32();
            header.Caps2 = reader.ReadInt32();
            header.Caps3 = reader.ReadInt32();
            header.Caps4 = reader.ReadInt32();
            header.Reserved2 = reader.ReadInt32();

            return header;
        }
    }

    public struct DdsPixelFormat
    {
        public int Size;
        public DdsPixelFormatFlags Flags;
        public CompressionAlgorithm FourCC;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int ABitMask;
    }

    public struct DdsHeader10
    {
        public DxgiFormat DXGIFormat;
        public uint ResourceDimension;
        public uint MiscFlag;
        public uint ArraySize;
        public uint MiscFlags2;

        public static DdsHeader10 ReadHeader(BinaryReader reader)
        {
            return new DdsHeader10()
            {
                DXGIFormat = (DxgiFormat)reader.ReadUInt32(),
                ResourceDimension = reader.ReadUInt32(),
                MiscFlag = reader.ReadUInt32(),
                ArraySize = reader.ReadUInt32(),
                MiscFlags2 = reader.ReadUInt32()
            };
        }
    }

    /// <summary>
    /// Denotes the compression algorithm used in the image. Either the image
    /// is uncompressed or uses some sort of block compression. The
    /// compression used is encoded in the header of image as textual
    /// representation of itself. So a DXT1 image is encoded as "1TXD" so the
    /// enum represents these values directly
    /// </summary>
    public enum CompressionAlgorithm : uint
    {
        /// <summary>
        /// No compression was used in the image.
        /// </summary>
        None = 0,

        /// <summary>
        /// <see cref="Dxt1Dds"/>. Also known as BC1
        /// </summary>
        D3DFMT_DXT1 = 827611204,

        /// <summary>
        /// Not supported. Also known as BC2
        /// </summary>
        D3DFMT_DXT2 = 844388420,

        /// <summary>
        /// <see cref="Dxt3Dds"/>. Also known as BC2
        /// </summary>
        D3DFMT_DXT3 = 861165636,

        /// <summary>
        /// Not supported. Also known as BC3
        /// </summary>
        D3DFMT_DXT4 = 877942852,

        /// <summary>
        /// <see cref="Dxt5Dds"/>. Also known as BC3
        /// </summary>
        D3DFMT_DXT5 = 894720068,

        DX10 = 808540228,

        ATI1 = 826889281,
        BC4U = 1429488450,
        BC4S = 1395934018,

        ATI2 = 843666497,
        BC5U = 1429553986,
        BC5S = 1395999554
    }

    /// <summary>Flags to indicate which members contain valid data.</summary>
    [Flags]
    public enum DdsFlags : uint
    {
        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Caps = 0x1,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Height = 0x2,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Width = 0x4,

        /// <summary>
        /// Required when pitch is provided for an uncompressed texture.
        /// </summary>
        Pitch = 0x8,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        PixelFormat = 0x1000,

        /// <summary>
        /// Required in a mipmapped texture.
        /// </summary>
        MipMapCount = 0x20000,

        /// <summary>
        /// Required when pitch is provided for a compressed texture.
        /// </summary>
        LinearSize = 0x80000,

        /// <summary>
        /// Required in a depth texture.
        /// </summary>
        Depth = 0x800000
    }

    /// <summary>Values which indicate what type of data is in the surface.</summary>
    [Flags]
    public enum DdsPixelFormatFlags : uint
    {
        /// <summary>
        ///     Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
        /// </summary>
        AlphaPixels = 0x1,

        /// <summary>
        ///     Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel
        ///     bitcount; dwABitMask contains valid data)
        /// </summary>
        Alpha = 0x2,

        /// <summary>
        ///     Texture contains compressed RGB data; dwFourCC contains valid data.
        /// </summary>
        Fourcc = 0x4,

        /// <summary>
        ///     Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask)
        ///     contain valid data.
        /// </summary>
        Rgb = 0x40,

        /// <summary>
        ///     Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask
        ///     contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
        /// </summary>
        Yuv = 0x200,

        /// <summary>
        ///     Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance
        ///     channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel
        ///     DDS file.
        /// </summary>
        Luminance = 0x20000
    }

    public enum DxgiFormat : uint
    {
        UNKNOWN = 0,
        R32G32B32A32_TYPELESS = 1,
        R32G32B32A32_FLOAT = 2,
        R32G32B32A32_UINT = 3,
        R32G32B32A32_SINT = 4,
        R32G32B32_TYPELESS = 5,
        R32G32B32_FLOAT = 6,
        R32G32B32_UINT = 7,
        R32G32B32_SINT = 8,
        R16G16B16A16_TYPELESS = 9,
        R16G16B16A16_FLOAT = 10,
        R16G16B16A16_UNORM = 11,
        R16G16B16A16_UINT = 12,
        R16G16B16A16_SNORM = 13,
        R16G16B16A16_SINT = 14,
        R32G32_TYPELESS = 15,
        R32G32_FLOAT = 16,
        R32G32_UINT = 17,
        R32G32_SINT = 18,
        R32G8X24_TYPELESS = 19,
        D32_FLOAT_S8X24_UINT = 20,
        R32_FLOAT_X8X24_TYPELESS = 21,
        X32_TYPELESS_G8X24_UINT = 22,
        R10G10B10A2_TYPELESS = 23,
        R10G10B10A2_UNORM = 24,
        R10G10B10A2_UINT = 25,
        R11G11B10_FLOAT = 26,
        R8G8B8A8_TYPELESS = 27,
        R8G8B8A8_UNORM = 28,
        R8G8B8A8_UNORM_SRGB = 29,
        R8G8B8A8_UINT = 30,
        R8G8B8A8_SNORM = 31,
        R8G8B8A8_SINT = 32,
        R16G16_TYPELESS = 33,
        R16G16_FLOAT = 34,
        R16G16_UNORM = 35,
        R16G16_UINT = 36,
        R16G16_SNORM = 37,
        R16G16_SINT = 38,
        R32_TYPELESS = 39,
        D32_FLOAT = 40,
        R32_FLOAT = 41,
        R32_UINT = 42,
        R32_SINT = 43,
        R24G8_TYPELESS = 44,
        D24_UNORM_S8_UINT = 45,
        R24_UNORM_X8_TYPELESS = 46,
        X24_TYPELESS_G8_UINT = 47,
        R8G8_TYPELESS = 48,
        R8G8_UNORM = 49,
        R8G8_UINT = 50,
        R8G8_SNORM = 51,
        R8G8_SINT = 52,
        R16_TYPELESS = 53,
        R16_FLOAT = 54,
        D16_UNORM = 55,
        R16_UNORM = 56,
        R16_UINT = 57,
        R16_SNORM = 58,
        R16_SINT = 59,
        R8_TYPELESS = 60,
        R8_UNORM = 61,
        R8_UINT = 62,
        R8_SNORM = 63,
        R8_SINT = 64,
        A8_UNORM = 65,
        R1_UNORM = 66,
        R9G9B9E5_SHAREDEXP = 67,
        R8G8_B8G8_UNORM = 68,
        G8R8_G8B8_UNORM = 69,
        BC1_TYPELESS = 70,
        BC1_UNORM = 71,
        BC1_UNORM_SRGB = 72,
        BC2_TYPELESS = 73,
        BC2_UNORM = 74,
        BC2_UNORM_SRGB = 75,
        BC3_TYPELESS = 76,
        BC3_UNORM = 77,
        BC3_UNORM_SRGB = 78,
        BC4_TYPELESS = 79,
        BC4_UNORM = 80,
        BC4_SNORM = 81,
        BC5_TYPELESS = 82,
        BC5_UNORM = 83,
        BC5_SNORM = 84,
        B5G6R5_UNORM = 85,
        B5G5R5A1_UNORM = 86,
        B8G8R8A8_UNORM = 87,
        B8G8R8X8_UNORM = 88,
        R10G10B10_XR_BIAS_A2_UNORM = 89,
        B8G8R8A8_TYPELESS = 90,
        B8G8R8A8_UNORM_SRGB = 91,
        B8G8R8X8_TYPELESS = 92,
        B8G8R8X8_UNORM_SRGB = 93,
        BC6H_TYPELESS = 94,
        BC6H_UF16 = 95,
        BC6H_SF16 = 96,
        BC7_TYPELESS = 97,
        BC7_UNORM = 98,
        BC7_UNORM_SRGB = 99,
        AYUV = 100,
        Y410 = 101,
        Y416 = 102,
        NV12 = 103,
        P010 = 104,
        P016 = 105,
        OPAQUE_420 = 106,
        YUY2 = 107,
        Y210 = 108,
        Y216 = 109,
        NV11 = 110,
        AI44 = 111,
        IA44 = 112,
        P8 = 113,
        A8P8 = 114,
        B4G4R4A4_UNORM = 115,
        P208 = 130,
        V208 = 131,
        V408 = 132,
    }
}
