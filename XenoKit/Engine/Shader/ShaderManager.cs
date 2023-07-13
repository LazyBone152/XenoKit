using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SDS;

namespace XenoKit.Engine.Shader
{
    public class ShaderManager
    {
        private GameBase game;
        private bool IsRenderSystemActive => game.RenderSystem != null;

        public ShaderManager(GameBase game)
        {
            this.game = game;
            string path = Utils.SanitizePath($"{SettingsManager.Instance.GetAppFolder()}/Shaders/technique_default_sds.emz.sds.xml");
            string agePath = Utils.SanitizePath($"{SettingsManager.Instance.GetAppFolder()}/Shaders/technique_age_sds.emz.sds.xml");

            if (!File.Exists(path))
            {
                Log.Add($"SDS file not found at {path}.", LogType.Error);
            }
            else
            {
                DefaultSdsFile = SDS_File.LoadFromXml(path);
            }

            if (!File.Exists(agePath))
            {
                Log.Add($"SDS file not found at {agePath}.", LogType.Error);
            }
            else
            {
                AgeSdsFile = SDS_File.LoadFromXml(agePath);
            }

            //DebugParseAllShaders();
        }

        private readonly SDS_File DefaultSdsFile;
        private readonly SDS_File AgeSdsFile;

        //Caches
        private readonly List<ShaderProgram> ShaderPrograms = new List<ShaderProgram>();
        private readonly GlobalSampler[] GlobalSamplers = new GlobalSampler[16];

        //Global sampler indices (0 - 4 are object specific samplers, 13+ are global but static so irrelevant)
        private const int SamplerCubeMap = 5;
        private const int SamplerProjectionMap = 6;
        private const int SamplerShadowMap = 7;
        private const int SamplerReflect = 8;
        private const int SamplerRefract = 9;
        private const int SamplerAlphaDepth = 10;
        private const int SamplerCurrentScene = 11;
        private const int SamplerSmallScene = 12;

        public ShaderProgram GetShaderProgram(string shaderProgramName)
        {
            lock (ShaderPrograms)
            {
                ShaderProgram shaderProgram = ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);

                if (shaderProgram == null)
                {
                    //First we look in the default SDS
                    SDSShaderProgram sdsEntry = DefaultSdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);
                    bool isDefaultSds = true;

                    //If the ShaderProgram isn't found there, then check the age SDS
                    if (sdsEntry == null)
                    {
                        sdsEntry = AgeSdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);
                        isDefaultSds = false;
                    }

                    if (sdsEntry != null)
                    {
                        byte[] ps = GetPixelShader(sdsEntry.PixelShader, isDefaultSds);
                        byte[] vs = GetVertexShader(sdsEntry.VertexShader, isDefaultSds);

                        shaderProgram = new ShaderProgram(shaderProgramName, vs, ps, HasSkinningEnable(sdsEntry), game.GraphicsDevice);
                        ShaderPrograms.Add(shaderProgram);
                    }
                    else
                    {
                        Log.Add($"ShaderProgram {shaderProgramName} not found.", LogType.Error);
                        return null;
                    }
                }

                return shaderProgram;
            }
        }

        public void Update()
        {
            //Set global sampler textures from RenderTargets here
        }

        #region GlobalSamplers
        public GlobalSampler GetGlobalSampler(int slot)
        {
            lock (GlobalSamplers)
            {
                //If sampler already exists in the cache then re-use that instance.
                if (GlobalSamplers[slot] != null)
                    return GlobalSamplers[slot];

                //Create sampler and add it to the cache
                GlobalSampler sampler = null;

                switch (slot)
                {
                    case 6:
                    case 7:
                        //ShadowMap / SamplerProjectionMap
                        {
                            sampler = new GlobalSampler(slot, game.RenderSystem.SamplerAlphaDepth,
                                                        new SamplerState()
                                                        {
                                                            AddressU = TextureAddressMode.Border,
                                                            AddressV = TextureAddressMode.Border,
                                                            AddressW = TextureAddressMode.Wrap,
                                                            BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                                                            MaxAnisotropy = 1,
                                                            ComparisonFunction = CompareFunction.LessEqual,
                                                            Filter = TextureFilter.Linear,
                                                            MipMapLevelOfDetailBias = 0,
                                                            Name = GetSamplerName(slot),
                                                            FilterMode = TextureFilterMode.Comparison,
                                                            GraphicsDevice = game.GraphicsDevice
                                                        });
                            break;
                        }
                    case 10:
                        //SamplerAlphaDepth
                        {
                            Texture2D texture = TextureLoader.ConvertToTexture2D(GetPathInShaderDir("Texture/ShadowMap.dds"), game.GraphicsDevice);
                            sampler = new GlobalSampler(slot, texture,
                                                        new SamplerState()
                                                        {
                                                            AddressU = TextureAddressMode.Wrap,
                                                            AddressV = TextureAddressMode.Wrap,
                                                            AddressW = TextureAddressMode.Wrap,
                                                            BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                                                            MaxAnisotropy = 1,
                                                            ComparisonFunction = CompareFunction.LessEqual,
                                                            Filter = TextureFilter.Point,
                                                            MipMapLevelOfDetailBias = 0,
                                                            Name = GetSamplerName(slot),
                                                            FilterMode = TextureFilterMode.Comparison,
                                                            GraphicsDevice = game.GraphicsDevice
                                                        });
                            break;
                        }
                    case 14:
                        //General lighting
                        {
                            EMB_File lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/cmn.emb", false);
                            sampler = new GlobalSampler(slot,
                                                        TextureLoader.ConvertToTexture2D(lightingEmb.Entry[0], GetTextureName(slot), game.GraphicsDevice),
                                                        new SamplerState()
                                                        {
                                                            AddressU = TextureAddressMode.Clamp,
                                                            AddressV = TextureAddressMode.Clamp,
                                                            AddressW = TextureAddressMode.Wrap,
                                                            BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                                                            MaxAnisotropy = 1,
                                                            ComparisonFunction = CompareFunction.Never,
                                                            Filter = TextureFilter.LinearMipPoint,
                                                            MipMapLevelOfDetailBias = 0,
                                                            Name = GetSamplerName(slot),
                                                            GraphicsDevice = game.GraphicsDevice
                                                        });
                            break;
                        }
                    case 15:
                        //Stage lighting
                        {
                            EMB_File lightingEmb;

                            if (SettingsManager.Instance.Settings.XenoKit_RimLightingEnabled)
                            {
                                lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFpot.emb", false); //ToP
                                //lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFtwf.emb", false); //Future In Ruins
                                //lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFten.emb", false); //World Tournament
                            }
                            else
                            {
                                byte[] dds = File.ReadAllBytes(GetPathInShaderDir("texture/NoRimLight.dds"));
                                lightingEmb = new EMB_File()
                                {
                                    Entry = new Xv2CoreLib.Resource.AsyncObservableCollection<EmbEntry>()
                                    {
                                        new EmbEntry()
                                        {
                                            Data = dds,
                                            Name = "DATA1.dds"
                                        }
                                    }
                                };
                            }

                            sampler = new GlobalSampler(slot,
                                                        TextureLoader.ConvertToTexture2D(lightingEmb.Entry[0], GetTextureName(slot), game.GraphicsDevice),
                                                        new SamplerState()
                                                        {
                                                            AddressU = TextureAddressMode.Clamp,
                                                            AddressV = TextureAddressMode.Clamp,
                                                            AddressW = TextureAddressMode.Wrap,
                                                            BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                                                            MaxAnisotropy = 1,
                                                            ComparisonFunction = CompareFunction.Never,
                                                            Filter = TextureFilter.PointMipLinear,
                                                            MipMapLevelOfDetailBias = 0,
                                                            Name = GetSamplerName(slot),
                                                            GraphicsDevice = game.GraphicsDevice
                                                        });
                            break;
                        }
                }

                GlobalSamplers[slot] = sampler;

                if (sampler == null)
                {
                    //Log.Add($"Sampler Not Implemented: {GetSamplerName(slot)}", LogType.Debug);
                }

                return sampler;
            }
        }

        public void SetGlobalSampler(int slot, bool isVertexShader)
        {
            GlobalSampler sampler = GetGlobalSampler(slot);

            if (sampler != null)
            {
                if (isVertexShader)
                {
                    game.GraphicsDevice.VertexTextures[slot] = sampler.RT != null ? sampler.RT.RenderTarget : sampler.Texture;
                    game.GraphicsDevice.VertexSamplerStates[slot] = sampler.Sampler;
                }
                else
                {
                    game.GraphicsDevice.Textures[slot] = sampler.RT != null ? sampler.RT.RenderTarget : sampler.Texture;
                    game.GraphicsDevice.SamplerStates[slot] = sampler.Sampler;
                }
            }
        }

        public void SetAllGlobalSamplers()
        {
            SetGlobalSampler(7, true);
            SetGlobalSampler(6, true);
            SetGlobalSampler(7, false);
            SetGlobalSampler(6, false);
            SetGlobalSampler(14, false);
            SetGlobalSampler(14, false);
            SetGlobalSampler(15, false);
            SetGlobalSampler(15, false);
        }

        public void ClearGlobalSampler(int slot)
        {
            GlobalSamplers[slot] = null;
        }

        #endregion

        #region Helpers
        private byte[] GetVertexShader(string name, bool isDefaultSds)
        {
            string type = isDefaultSds ? "default" : "age";
            string path = Utils.SanitizePath($"{SettingsManager.Instance.GetAppFolder()}/Shaders/shader_{type}_vs/{name}.xvu");

            if (!File.Exists(path))
            {
                Log.Add($"VertexShader not found at {path}.", LogType.Error);
                return null;
            }

            return File.ReadAllBytes(path);
        }

        private byte[] GetPixelShader(string name, bool isDefaultSds)
        {
            string type = isDefaultSds ? "default" : "age";
            string path = Utils.SanitizePath($"{SettingsManager.Instance.GetAppFolder()}/Shaders/shader_{type}_ps/{name}.xpu");

            if (!File.Exists(path))
            {
                Log.Add($"PixelShader not found at {path}.", LogType.Error);
                return null;
            }

            return File.ReadAllBytes(path);
        }

        public string GetSamplerName(int slot)
        {

            switch (slot)
            {
                case 0:
                    return "State_ImageSampler0";
                case 1:
                    return "State_ImageSampler1";
                case 2:
                    return "State_ImageSampler2";
                case 3:
                    return "State_ImageSampler3";
                case 4:
                    return "State_SamplerToon";
                case 5:
                    return "State_SamplerCubeMap";
                case 6:
                    return "State_SamplerProjectionMap";
                case 7:
                    return "SamplerShadowMap";
                case 8:
                    return "State_SamplerReflect";
                case 9:
                    return "State_SamplerRefract";
                case 10:
                    return "State_SamplerAlphaDepth";
                case 11:
                    return "State_SamplerCurrentScene";
                case 12:
                    return "State_SamplerSmallScene";
                case 13:
                    return "State_ImageSamplerTemp13";
                case 14:
                    return "State_ImageSamplerTemp14";
                case 15:
                    return "State_ImageSamplerTemp15";
            }
            return slot.ToString();
        }

        public string GetTextureName(int slot)
        {
            switch (slot)
            {
                case 0:
                    return "Texture_ImageSampler0";
                case 1:
                    return "Texture_ImageSampler1";
                case 2:
                    return "Texture_ImageSampler2";
                case 3:
                    return "Texture_ImageSampler3";
                case 4:
                    return "Texture_SamplerToon";
                case 5:
                    return "Texture_SamplerCubeMap";
                case 6:
                    return "Texture_SamplerProjectionMap";
                case 7:
                    return "Texture_SamplerShadowMap";
                case 8:
                    return "Texture_SamplerReflect";
                case 9:
                    return "Texture_SamplerRefract";
                case 10:
                    return "Texture_SamplerAlphaDepth";
                case 11:
                    return "Texture_SamplerCurrentScene";
                case 12:
                    return "Texture_SamplerSmallScene";
                case 13:
                    return "Texture_ImageSamplerTemp13";
                case 14:
                    return "Texture_ImageSamplerTemp14";
                case 15:
                    return "Texture_ImageSamplerTemp15";
            }
            return slot.ToString();
        }

        private bool HasSkinningEnable(SDSShaderProgram entry)
        {
            return entry.Parameters.Any(x => x.Name == "SkinningEnable");
        }

        private string GetPathInShaderDir(string path)
        {
            return Utils.SanitizePath($"{SettingsManager.Instance.GetAppFolder()}/Shaders/{path}");
        }

        public void DebugParseAllShaders()
        {
            string[] files = Directory.GetFiles("Shaders", "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (Path.GetExtension(file) == ".xvu" || Path.GetExtension(file) == ".xpu" || Path.GetExtension(file) == ".xcu")
                {
                    var byteCode = new DXBC.DxbcParser(File.ReadAllBytes(file));
                    byteCode.SaveXml(file + ".xml");
                }
            }
        }

        #endregion
    }

    public class GlobalSampler
    {
        public int Slot { get; private set; }
        public SamplerState Sampler { get; private set; }

        //Texture:
        public Texture Texture { get; private set; }
        public RenderTargetWrapper RT { get; private set; }

        public GlobalSampler(int slot, Texture texture, SamplerState sampler)
        {
            Slot = slot;
            Texture = texture;
            Sampler = sampler;
        }

        public GlobalSampler(int slot, RenderTargetWrapper texture, SamplerState sampler)
        {
            Slot = slot;
            RT = texture;
            Sampler = sampler;
        }
    }
}
