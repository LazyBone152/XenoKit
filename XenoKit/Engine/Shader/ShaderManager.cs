using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
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
        #region Singleton
        private static Lazy<ShaderManager> instance = new Lazy<ShaderManager>(() => new ShaderManager());
        public static ShaderManager Instance => instance.Value;

        private ShaderManager() 
        {
            string path = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/technique_default_sds.emz.sds.xml");
            string agePath = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/technique_age_sds.emz.sds.xml");

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
        #endregion

        private readonly SDS_File DefaultSdsFile;
        private readonly SDS_File AgeSdsFile;

        //Caches
        private readonly List<ShaderProgram> ShaderPrograms = new List<ShaderProgram>();
        private readonly List<GlobalSampler> GlobalSamplers = new List<GlobalSampler>();


        public ShaderProgram GetShaderProgram(string shaderProgramName, GraphicsDevice graphicsDevice)
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

                        shaderProgram = new ShaderProgram(shaderProgramName, vs, ps, HasSkinningEnable(sdsEntry), graphicsDevice);
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

        public GlobalSampler GetGlobalSampler(int slot)
        {
            lock (GlobalSamplers)
            {
                //If sampler already exists in the cache then re-use that instance.
                if (GlobalSamplers.Any(x => x.Slot == slot))
                    return GlobalSamplers.FirstOrDefault(x => x.Slot == slot);

                //Create sampler and add it to the cache
                GlobalSampler sampler = null;

                switch (slot)
                {
                    case 6:
                    case 7:
                        //ShadowMap / SamplerProjectionMap
                        {
                            Texture2D texture = TextureLoader.ConvertToTexture2D(GetPathInShaderDir("Texture/ShadowMap.dds"), SceneManager.MainGameBase.GraphicsDevice);
                            sampler = new GlobalSampler(slot, texture,
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
                                                            FilterMode = TextureFilterMode.Comparison
                                                        });
                            break;
                        }
                    case 10:
                        //SamplerAlphaDepth
                        {
                            Texture2D texture = TextureLoader.ConvertToTexture2D(GetPathInShaderDir("Texture/ShadowMap.dds"), SceneManager.MainGameBase.GraphicsDevice);
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
                                                            FilterMode = TextureFilterMode.Comparison
                                                        });
                            break;
                        }
                    case 14:
                        //General lighting
                        {
                            EMB_File lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/cmn.emb", false);
                            sampler = new GlobalSampler(slot,
                                                        TextureLoader.ConvertToTexture2D(lightingEmb.Entry[0], GetTextureName(slot), SceneManager.MainGameBase.GraphicsDevice),
                                                        new SamplerState()
                                                        {
                                                            AddressU = TextureAddressMode.Clamp,
                                                            AddressV = TextureAddressMode.Clamp,
                                                            AddressW = TextureAddressMode.Wrap,
                                                            BorderColor = new Microsoft.Xna.Framework.Color(1,1,1,1), 
                                                            MaxAnisotropy = 1,
                                                            ComparisonFunction = CompareFunction.Never,
                                                            Filter = TextureFilter.LinearMipPoint,
                                                            MipMapLevelOfDetailBias = 0,
                                                            Name = GetSamplerName(slot)
                                                        });
                            break;
                        }
                    case 15:
                        //Stage lighting
                        {
                            EMB_File lightingEmb;

                            if (SettingsManager.Instance.Settings.XenoKit_RimLightingEnabled)
                            {
                                //lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFtwf.emb", false); //Future In Ruins
                                lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFten.emb", false); //World Tournament
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
                                                        TextureLoader.ConvertToTexture2D(lightingEmb.Entry[0], GetTextureName(slot), SceneManager.MainGameBase.GraphicsDevice),
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
                                                            Name = GetSamplerName(slot)
                                                        });
                            break;
                        }
                }

                if(sampler != null)
                    GlobalSamplers.Add(sampler);

                if(sampler == null)
                {
                    //Log.Add($"Sampler Not Implemented: {GetSamplerName(slot)}", LogType.Debug);
                }

                return sampler;
            }
        }
        
        public void SetGlobalSampler(int slot, bool isVertexShader)
        {
            GlobalSampler sampler = GetGlobalSampler(slot);

            if(sampler != null)
            {
                sampler.Sampler.GraphicsDevice = SceneManager.GraphicsDeviceRef;

                if (isVertexShader)
                {
                    SceneManager.GraphicsDeviceRef.VertexTextures[slot] = sampler.Texture;
                    SceneManager.GraphicsDeviceRef.VertexSamplerStates[slot] = sampler.Sampler;
                }
                else
                {
                    SceneManager.GraphicsDeviceRef.Textures[slot] = sampler.Texture;
                    SceneManager.GraphicsDeviceRef.SamplerStates[slot] = sampler.Sampler;
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
            GlobalSamplers.RemoveAll(x => x.Slot == slot);
        }

        //Helpers
        private byte[] GetVertexShader(string name, bool isDefaultSds)
        {
            //Attempt to load breakers shaders
            //string breakersPath = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/Breakers/{name}.xvu");

            //if (File.Exists(breakersPath))
            //    return File.ReadAllBytes(breakersPath);

            string type = isDefaultSds ? "default" : "age";
            string path = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/shader_{type}_vs/{name}.xvu");

            if (!File.Exists(path))
            {
                Log.Add($"VertexShader not found at {path}.", LogType.Error);
                return null;
            }

            return File.ReadAllBytes(path);
        }

        private byte[] GetPixelShader(string name, bool isDefaultSds)
        {
            //Attempt to load breakers shaders
            //string breakersPath = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/Breakers/{name}.xpu");

            //if(File.Exists(breakersPath))
            //    return File.ReadAllBytes(breakersPath);

            string type = isDefaultSds ? "default" : "age";
            string path = Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/shader_{type}_ps/{name}.xpu");

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
            return Utils.SanitizePath($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/Shaders/{path}");
        }
            
        public void DebugParseAllShaders()
        {
            string[] files = Directory.GetFiles("Shaders", "*.*", SearchOption.AllDirectories);

            foreach(var file in files)
            {
                if(Path.GetExtension(file) == ".xvu" || Path.GetExtension(file) == ".xpu" || Path.GetExtension(file) == ".xcu")
                {
                    var byteCode = new DXBC.DxbcParser(File.ReadAllBytes(file));
                    byteCode.SaveXml(file + ".xml");
                }
            }
        }
    }

    public class GlobalSampler
    {
        public int Slot { get; private set; }
        public Texture Texture { get; private set; }
        public SamplerState Sampler { get; private set; }

        public GlobalSampler(int slot, Texture texture, SamplerState sampler)
        {
            Slot = slot;
            Texture = texture;
            Sampler = sampler;
        }
    }
}
