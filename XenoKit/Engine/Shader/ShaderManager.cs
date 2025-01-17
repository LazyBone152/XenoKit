using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.BEV;
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

        public bool IsExtShadersLoaded { get; private set; }

        private readonly SDS_File DefaultSdsFile;
        private readonly SDS_File AgeSdsFile;
        private readonly EMB_File DefaultEmb_VS;
        private readonly EMB_File DefaultEmb_PS;
        private readonly EMB_File AgeEmb_VS;
        private readonly EMB_File AgeEmb_PS;

        //Caches
        private readonly List<ShaderProgram> ShaderPrograms = new List<ShaderProgram>();
        private readonly List<ShaderProgram> ExtShaders = new List<ShaderProgram>();
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

        private Texture2D NoRimLightTexture;

        //File watch
        private FileSystemWatcher adamShaderWatcher;
        private bool defaultVsEmbDirty, defaultPsEmbDirty, ageVsEmbDirty, agePsEmbDirty;
        private bool defaultSdsDirty, ageSdsDirty;

        public ShaderManager(GameBase game)
        {
            this.game = game;
            DefaultSdsFile = (SDS_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/technique_default_sds.emz", false, true);
            AgeSdsFile = (SDS_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/technique_age_sds.emz", false, true);
            AgeEmb_VS = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_age_vs.emz", false, true);
            AgeEmb_PS = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_age_ps.emz", false, true);
            DefaultEmb_VS = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_default_vs.emz", false, true);
            DefaultEmb_PS = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_default_ps.emz", false, true);

            NoRimLightTexture = new Texture2D(game.GraphicsDevice, 128, 8);

            InitAdamShaderWatcher();

            LoadExtShaders();
        }

        private void LoadExtShaders()
        {
            if(ExtShaders.Count > 0)
            {
                Log.Add("LoadExtShaders has been called more than once.", LogType.Warning);
                return;
            }

            if(!Directory.Exists("XenoKit/ExtShaders"))
            {
                Log.Add("LoadExtShaders: \"XenoKit/ExtShaders\" was not found. Some post process effects will be disabled.", LogType.Error);
                return;
            }

#if !DEBUG
            try
#endif
            {
                ExtShaders.Add(LoadExtShader("AxisCorrection", "AxisCorrection", "AxisCorrection", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Copy", "Copy", "Copy", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Dim", "Dim", "Dim", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_CopyRegion", "CopyRegion", "CopyRegion", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Smooth", "Smooth", "Smooth", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Glare", "Glare", "Glare", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Pixel", "Pixel", "Pixel", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Merge2", "Merge2", "Merge2", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Merge5", "Merge5", "Merge5", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_Merge8", "Merge8", "Merge8", ShaderProgramSource.External));
                ExtShaders.Add(LoadExtShader("YBS_SceneMerge", "SceneMerge", "SceneMerge", ShaderProgramSource.External));
            }
#if !DEBUG
            catch (Exception ex)
            {
                Log.Add($"LoadExtShaders: failed to load post process shaders, with error: {ex.Message}", ex.ToString(), LogType.Error);
                return;
            }
#endif

            IsExtShadersLoaded = true;
        }

        private ShaderProgram LoadExtShader(string name, string vertexShader, string pixelShader, ShaderProgramSource source = ShaderProgramSource.External)
        {
            if(source == ShaderProgramSource.Xenoverse)
            {
                Log.Add($"LoadExtShader: Game shaders cannot be loaded in this way.", LogType.Error);
            }

            string vsPath = $"XenoKit/ExtShaders/{vertexShader}.xvu";
            string psPath = $"XenoKit/ExtShaders/{pixelShader}.xpu";

            if (!File.Exists(vsPath))
            {
                Log.Add($"Shader not found: \"{vsPath}\"", LogType.Error);
                return null;
            }

            if (!File.Exists(psPath))
            {
                Log.Add($"Shader not found: \"{psPath}\"", LogType.Error);
                return null;
            }

            byte[] vsShader = File.ReadAllBytes(vsPath);
            byte[] psShader = File.ReadAllBytes(psPath);

            return new ShaderProgram(name, vsShader, psShader, source, game.GraphicsDevice);
        }

        public ShaderProgram GetExtShaderProgram(string shaderProgramName)
        {
            ShaderProgram shaderProgram = ExtShaders.FirstOrDefault(x => x.Name == shaderProgramName);

            if (shaderProgram == null)
            {
                Log.Add($"GetExtShaderProgram: could not find shader \"{shaderProgramName}\"", LogType.Error);
            }

            return shaderProgram;
        }

        public ShaderProgram GetShaderProgram(string shaderProgramName)
        {
            lock (ShaderPrograms)
            {
                ShaderProgram shaderProgram = ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);

                if (shaderProgram == null)
                {
                    //Correct priority is AGE -> DEFAULT

                    //First we look in the age SDS
                    SDSShaderProgram sdsEntry = AgeSdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);
                    bool isDefaultSds = false;

                    //If the ShaderProgram isn't found there, then check the default SDS
                    if (sdsEntry == null)
                    {
                        sdsEntry = DefaultSdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == shaderProgramName);
                        isDefaultSds = true;
                    }

                    if (sdsEntry != null)
                    {
                        byte[] ps = GetPixelShader(sdsEntry.PixelShader, isDefaultSds);
                        byte[] vs = GetVertexShader(sdsEntry.VertexShader, isDefaultSds);

                        shaderProgram = new ShaderProgram(sdsEntry, vs, ps, HasSkinningEnable(sdsEntry), game.GraphicsDevice);
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

        public void DelayedUpdate()
        {
            HandleShaderReloading();
        }

        #region ShaderReload
        private void InitAdamShaderWatcher()
        {
            string path = FileManager.Instance.GetAbsolutePath("adam_shader");
            Directory.CreateDirectory(path);

            adamShaderWatcher = new FileSystemWatcher();
            adamShaderWatcher.Path = path;
            adamShaderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
            adamShaderWatcher.Filter = "*.emz";

            // Handle events
            adamShaderWatcher.Changed += (sender, e) => OnShaderFilesChanged(e);

            // Start watching
            adamShaderWatcher.EnableRaisingEvents = true;
        }

        private void OnShaderFilesChanged(FileSystemEventArgs e)
        {
            switch (e.Name)
            {
                case "shader_default_ps.emz":
                    defaultPsEmbDirty = true;
                    break;
                case "shader_default_vs.emz":
                    defaultVsEmbDirty = true;
                    break;
                case "shader_age_ps.emz":
                    agePsEmbDirty = true;
                    break;
                case "shader_age_vs.emz":
                    ageVsEmbDirty = true;
                    break;
                case "technique_default_sds.emz":
                    defaultSdsDirty = true;
                    break;
                case "technique_age_sds.emz":
                    ageSdsDirty = true;
                    break;
            }
        }

        private void HandleShaderReloading()
        {
            if (!SettingsManager.settings.XenoKit_AutoReloadShaders) return;

            List<string> vertexShadersModified = new List<string>();
            List<string> pixelShadersModified = new List<string>();
            List<string> sdsShadersModified = new List<string>();

            if (defaultSdsDirty)
            {
                SDS_File sds = (SDS_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/technique_default_sds.emz", false, true, true);
                UpdateShaderSds(sds, DefaultSdsFile, sdsShadersModified);
                defaultSdsDirty = false;
            }

            if (ageSdsDirty)
            {
                SDS_File sds = (SDS_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/technique_age_sds.emz", false, true, true);
                UpdateShaderSds(sds, AgeSdsFile, sdsShadersModified);
                ageSdsDirty = false;
            }

            if (defaultPsEmbDirty)
            {
                EMB_File emb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_default_ps.emz", false, true, true);
                UpdateShaderEmb(emb, DefaultEmb_PS, pixelShadersModified);
                defaultPsEmbDirty = false;
            }

            if (defaultVsEmbDirty)
            {
                EMB_File emb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_default_vs.emz", false, true, true);
                UpdateShaderEmb(emb, DefaultEmb_VS, vertexShadersModified);
                defaultVsEmbDirty = false;
            }

            if (agePsEmbDirty)
            {
                EMB_File emb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_age_ps.emz", false, true, true);
                UpdateShaderEmb(emb, AgeEmb_PS, pixelShadersModified);
                agePsEmbDirty = false;
            }

            if (ageVsEmbDirty)
            {
                EMB_File emb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("adam_shader/shader_age_vs.emz", false, true, true);
                UpdateShaderEmb(emb, AgeEmb_VS, vertexShadersModified);
                ageVsEmbDirty = false;
            }

            if (vertexShadersModified.Count > 0 || pixelShadersModified.Count > 0 || sdsShadersModified.Count > 0)
            {
                List<ShaderProgram> modifiedShaderPrograms = new List<ShaderProgram>();

                lock (ShaderPrograms)
                {
                    for (int i = ShaderPrograms.Count - 1; i >= 0; i--)
                    {
                        ShaderProgram shader = ShaderPrograms[i];

                        if (pixelShadersModified.Contains(shader.SdsEntry.PixelShader) || 
                            vertexShadersModified.Contains(shader.SdsEntry.VertexShader) ||
                            sdsShadersModified.Contains(shader.SdsEntry.Name))
                        {
                            //Remove it and reload, now with new vert/pixel shaders
                            ShaderPrograms.RemoveAt(i);
                            var newShader = GetShaderProgram(shader.Name);

                            if (shader == null)
                            {
                                Log.Add("Shader reload failed? shader was null!", LogType.Error);
                                ShaderPrograms.Remove(newShader);
                                ShaderPrograms.Add(shader);
                            }
                            else
                            {
                                modifiedShaderPrograms.Add(newShader);
                            }

                        }

                    }
                }

                game.CompiledObjectManager.ForceShaderUpdate(modifiedShaderPrograms);

                Log.Add("Shaders hot reloaded!");
            }
        }

        private static void UpdateShaderSds(SDS_File newSds, SDS_File sdsFile, List<string> modifiedShaderPrograms)
        {
            foreach(var newSdsEntry in newSds.ShaderPrograms)
            {
                var existingEntry = sdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == newSdsEntry.Name);

                if(existingEntry != null)
                {
                    if (!newSdsEntry.Compare(existingEntry))
                    {
                        modifiedShaderPrograms.Add(newSdsEntry.Name);

                        //Copy over new entry state
                        existingEntry.VertexShader = newSdsEntry.VertexShader;
                        existingEntry.PixelShader = newSdsEntry.PixelShader;
                        existingEntry.Parameters = newSdsEntry.Parameters;
                    }
                }
                else
                {
                    //New shader - add to in memory SDS
                    sdsFile.ShaderPrograms.Add(newSdsEntry);
                }
            }
        }

        private static void UpdateShaderEmb(EMB_File newEmb, EMB_File embFile, List<string> modifiedShaders)
        {
            foreach (var newEmbEntry in newEmb.Entry)
            {
                var existingEntry = embFile.GetEntry(newEmbEntry.Name);

                if(existingEntry != null)
                {
                    if (!newEmbEntry.Compare(existingEntry))
                    {
                        existingEntry.Data = newEmbEntry.Data;
                        modifiedShaders.Add(Path.GetFileNameWithoutExtension(newEmbEntry.Name));
                    }
                }
                else
                {
                    //New entry - add to in memory EMB
                    embFile.Entry.Add(newEmbEntry);
                }
            }
        }

        #endregion

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
                            //Texture2D texture = TextureLoader.ConvertToTexture2D(GetPathInShaderDir("Texture/ShadowMap.dds"), game.GraphicsDevice);
                            sampler = new GlobalSampler(slot, game.RenderSystem.ShadowPassRT0,
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
                            sampler = new GlobalSampler(slot, game.RenderSystem.SamplerAlphaDepth,
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
                            Texture2D texture;

                            if (SettingsManager.Instance.Settings.XenoKit_RimLightingEnabled)
                            {
                                EMB_File lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFpot.emb", false); //ToP
                                //EMB_File lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFtwf.emb", false); //Future In Ruins
                                //EMB_File lightingEmb = (EMB_File)FileManager.Instance.GetParsedFileFromGame("lighting/environment/BFten.emb", false); //World Tournament
                                texture = TextureLoader.ConvertToTexture2D(lightingEmb.Entry[0], GetTextureName(slot), game.GraphicsDevice);
                            }
                            else
                            {
                                texture = NoRimLightTexture;
                            }

                            sampler = new GlobalSampler(slot,
                                                        texture,
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
            SetGlobalSampler(10, false);
            SetGlobalSampler(14, false);
            SetGlobalSampler(15, false);
        }

        public void ClearGlobalSampler(int slot)
        {
            GlobalSamplers[slot]?.Texture?.Dispose();
            GlobalSamplers[slot] = null;
        }

        #endregion

        #region Helpers
        private byte[] GetVertexShader(string name, bool isDefaultSds)
        {
            EMB_File embFile = isDefaultSds ? DefaultEmb_VS : AgeEmb_VS;
            EmbEntry enbEntry = embFile.GetEntry(name + ".xvu");

            if (enbEntry == null)
            {
                Log.Add($"Could not find VertexShader {name}!", LogType.Error);
                return null;
            }

            return enbEntry.Data;
        }

        private byte[] GetPixelShader(string name, bool isDefaultSds)
        {
            EMB_File embFile = isDefaultSds ? DefaultEmb_PS : AgeEmb_PS;
            EmbEntry enbEntry = embFile.GetEntry(name + ".xpu");

            if (enbEntry == null)
            {
                Log.Add($"Could not find PixelShader {name}!", LogType.Error);
                return null;
            }

            return enbEntry.Data;
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
