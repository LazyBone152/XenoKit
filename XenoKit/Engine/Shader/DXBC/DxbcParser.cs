using LB_Common.Utils;
using System;
using System.Linq;
using Xv2CoreLib;
using YAXLib;

namespace XenoKit.Engine.Shader.DXBC
{
    /// <summary>
    /// Simple DXBC parser for information such as input signature and constant buffers.
    /// </summary>
    public class DxbcParser
    {
        private const string INPUT_CHUNK = "ISGN";
        private const string RESOURCE_CHUNK = "RDEF";

        public DxbcInputSignature[] InputSignature { get; set; }
        public DxbcConstantBuffer[] CBuffers { get; set; }
        public DxbcResourceBinding[] ResourceBindings { get; set; }

        public DxbcParser(byte[] bytecode)
        {
            //Input Signature
            int inputIdx = ArraySearch.IndexOf(bytecode, INPUT_CHUNK);

            if (inputIdx != -1)
            {
                int count = BitConverter.ToInt32(bytecode, inputIdx + 8);
                int chunkOffset = inputIdx + 8;
                int elementsOffset = inputIdx + 16;

                InputSignature = new DxbcInputSignature[count];

                for (int i = 0; i < count; i++)
                {
                    InputSignature[i] = new DxbcInputSignature()
                    {
                        Name = StringEx.GetString(bytecode, BitConverter.ToInt32(bytecode, elementsOffset + 0) + chunkOffset, false, StringEx.EncodingType.ASCII),
                        SemanticIndex = BitConverter.ToInt32(bytecode, elementsOffset + 4),
                        SysValueType = BitConverter.ToInt32(bytecode, elementsOffset + 8),
                        ComponentType = BitConverter.ToInt32(bytecode, elementsOffset + 12),
                        Register = BitConverter.ToInt32(bytecode, elementsOffset + 16),
                        Mask = bytecode[elementsOffset + 20],
                        ReadWriteMask = bytecode[elementsOffset + 21]
                    };

                    elementsOffset += 24;
                }
            }

            //Resources:
            int resourceIdx = ArraySearch.IndexOf(bytecode, RESOURCE_CHUNK);

            if (resourceIdx != -1)
            { 
                //Constant Bindings:
                int count = BitConverter.ToInt32(bytecode, resourceIdx + 8);
                int chunkOffset = resourceIdx + 8;
                int cbOffset = BitConverter.ToInt32(bytecode, resourceIdx + 12) + chunkOffset;

                CBuffers = new DxbcConstantBuffer[count];

                for(int i = 0; i < count; i++)
                {
                    int numVariables = BitConverter.ToInt32(bytecode, cbOffset + 4);
                    int variablesOffset = BitConverter.ToInt32(bytecode, cbOffset + 8) + chunkOffset;

                    CBuffers[i] = new DxbcConstantBuffer()
                    {
                        SizeInBytes = BitConverter.ToInt32(bytecode, cbOffset + 12),
                        Name = StringEx.GetString(bytecode, BitConverter.ToInt32(bytecode, cbOffset + 0) + chunkOffset, false, StringEx.EncodingType.ASCII),
                        NumVariables = numVariables,
                        Type = BitConverter.ToInt32(bytecode, cbOffset + 20),
                        Flags = BitConverter.ToInt32(bytecode, cbOffset + 16),
                        Variables = new DxbcConstantBufferVariable[numVariables]
                    };

                    for(int a = 0; a < numVariables; a++)
                    {
                        string variableName = StringEx.GetString(bytecode, BitConverter.ToInt32(bytecode, variablesOffset + 0) + chunkOffset, false, StringEx.EncodingType.ASCII);

                        int typeOffset = BitConverter.ToInt32(bytecode, variablesOffset + 16) + chunkOffset;
                        int defaultValueOffset = BitConverter.ToInt32(bytecode, variablesOffset + 20);
                        
                        CBuffers[i].Variables[a] = new DxbcConstantBufferVariable()
                        {
                            Name = variableName,
                            VariableSize = BitConverter.ToInt32(bytecode, variablesOffset + 8),
                            VariableFlags = BitConverter.ToInt32(bytecode, variablesOffset + 12),
                            VariableClass = BitConverter.ToUInt16(bytecode, typeOffset + 0),
                            VariableType = BitConverter.ToUInt16(bytecode, typeOffset + 2),
                            NumMatrixRows = BitConverter.ToUInt16(bytecode, typeOffset + 4),
                            NumMatrixColumns = BitConverter.ToUInt16(bytecode, typeOffset + 6),
                            NumArrayVariables = BitConverter.ToUInt16(bytecode, typeOffset + 8),
                            NumStructMembers = BitConverter.ToUInt16(bytecode, typeOffset + 10)
                        };

                        if (defaultValueOffset != 0)
                        {
                            CBuffers[i].Variables[a].DefaultValue = bytecode.GetRange(defaultValueOffset + chunkOffset, CBuffers[i].Variables[a].VariableSize);
                        }

                        variablesOffset += 40;
                    }


                    /*
                    //Fix vs_matrix_cb. We need to remove the (_padding) variables.
                    if (CBuffers[i].Name == "vs_matrix_cb")
                    {
                        var fixedVariables = new DxbcConstantBufferVariable[numVariables - 2];
                        int idx = 0;

                        foreach (var variable in CBuffers[i].Variables)
                        {
                            if (variable.Name != "g_mWV_VS_padding" && variable.Name != "g_mW_VS_padding")
                            {
                                fixedVariables[idx] = variable;
                                idx++;
                            }
                        }

                        CBuffers[i].Variables = fixedVariables;
                    }
                    */

                    cbOffset += 24;
                }

                //Resource Bindings:
                count = BitConverter.ToInt32(bytecode, resourceIdx + 16);
                int rbOffset = BitConverter.ToInt32(bytecode, resourceIdx + 20) + chunkOffset;

                ResourceBindings = new DxbcResourceBinding[count];

                for(int i = 0; i < count; i++)
                {
                    string name = StringEx.GetString(bytecode, BitConverter.ToInt32(bytecode, rbOffset + 0) + chunkOffset, false, StringEx.EncodingType.ASCII);

                    ResourceBindings[i] = new DxbcResourceBinding()
                    {
                        Name = name,
                        ShaderInputType = (DxbcResourceBinding.ResourceBindingType)BitConverter.ToInt32(bytecode, rbOffset + 4),
                        ResourceReturnType = BitConverter.ToInt32(bytecode, rbOffset + 8),
                        ResourceViewDimension = BitConverter.ToInt32(bytecode, rbOffset + 12),
                        NumSamples = BitConverter.ToInt32(bytecode, rbOffset + 16),
                        BindPoint = BitConverter.ToInt32(bytecode, rbOffset + 20),
                        BindCount = BitConverter.ToInt32(bytecode, rbOffset + 24),
                        ShaderInputFlags = BitConverter.ToInt32(bytecode, rbOffset + 28)
                    };

                    rbOffset += 32;
                }
            }
        
            if(CBuffers != null && ResourceBindings != null)
            {
                foreach(var cbuffer in CBuffers)
                {
                    var resource = ResourceBindings.FirstOrDefault(x => x.Name == cbuffer.Name && x.ShaderInputType == DxbcResourceBinding.ResourceBindingType.CBuffer);

                    if(resource != null)
                    {
                        cbuffer.ResourceBinding = resource;
                        resource.ConstantBuffer = cbuffer;
                    }

                }

            }

            //Initialize default arrays if no data was found. This avoids tiresome null checking everywhere for something thats unlikely.
            if (InputSignature == null)
                InputSignature = new DxbcInputSignature[0];

            if (CBuffers == null)
                CBuffers = new DxbcConstantBuffer[0];

            if (ResourceBindings == null)
                ResourceBindings = new DxbcResourceBinding[0];
        }
    
        public bool HasCB(string cbName)
        {
            return CBuffers.Any(x => x.Name == cbName);
        }

        public void SaveXml(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(DxbcParser));
            serializer.SerializeToFile(this, path);
        }
    }

    public class DxbcInputSignature
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int SemanticIndex { get; set; }
        [YAXAttributeForClass]
        public int SysValueType { get; set; }
        [YAXAttributeForClass]
        public int ComponentType { get; set; }
        [YAXAttributeForClass]
        public int Register { get; set; }
        [YAXAttributeForClass]
        public byte Mask { get; set; }
        [YAXAttributeForClass]
        public byte ReadWriteMask { get; set; }
    }

    public class DxbcConstantBuffer
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Flags { get; set; }
        [YAXAttributeForClass]
        public int Type { get; set; }
        [YAXAttributeForClass]
        public int NumVariables { get; set; }
        [YAXAttributeForClass]
        public int SizeInBytes { get; set; }

        //Linked from resource binding
        [YAXDontSerialize]
        public int Slot => (ResourceBinding != null) ? ResourceBinding.BindPoint : -1;
        public DxbcResourceBinding ResourceBinding = null;
        [YAXDontSerialize]
        public bool IsConstantBuffer => true;

        public DxbcConstantBufferVariable[] Variables { get; set; }
    }

    public class DxbcConstantBufferVariable
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int VariableSize { get; set; }
        [YAXAttributeForClass]
        public int VariableFlags { get; set; }

        [YAXAttributeForClass]
        public ushort VariableClass { get; set; }
        [YAXAttributeForClass]
        public ushort VariableType { get; set; }
        [YAXAttributeForClass]
        public ushort NumMatrixRows { get; set; }
        [YAXAttributeForClass]
        public ushort NumMatrixColumns { get; set; }
        [YAXAttributeForClass]
        public ushort NumArrayVariables { get; set; }
        [YAXAttributeForClass]
        public ushort NumStructMembers { get; set; }

        [YAXDontSerializeIfNull]
        public byte[] DefaultValue { get; set; }
    }

    public class DxbcResourceBinding
    {
        public enum ResourceBindingType : int
        {
            CBuffer = 0,
            Texture = 2,
            Sampler = 3
        }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ResourceBindingType ShaderInputType { get; set; }
        [YAXAttributeForClass]
        public int ResourceReturnType { get; set; }
        [YAXAttributeForClass]
        public int ResourceViewDimension { get; set; }
        [YAXAttributeForClass]
        public int NumSamples { get; set; }
        [YAXAttributeForClass]
        public int BindPoint { get; set; }
        [YAXAttributeForClass]
        public int BindCount { get; set; }
        [YAXAttributeForClass]
        public int ShaderInputFlags { get; set; }

        public DxbcConstantBuffer ConstantBuffer = null;
        [YAXDontSerialize]
        public bool IsConstantBuffer => ShaderInputType == ResourceBindingType.CBuffer;
    }

    //Did not align with EffectParameterClass. MatrixColumnsMajor was missing in MonoGame.
    enum D3D_SHADER_VARIABLE_CLASS : ushort
    {
        D3D_SVC_SCALAR,
        D3D_SVC_VECTOR,
        D3D_SVC_MATRIX_ROWS,
        D3D_SVC_MATRIX_COLUMNS,
        D3D_SVC_OBJECT,
        D3D_SVC_STRUCT,
        D3D_SVC_INTERFACE_CLASS,
        D3D_SVC_INTERFACE_POINTER,
        D3D10_SVC_SCALAR,
        D3D10_SVC_VECTOR,
        D3D10_SVC_MATRIX_ROWS,
        D3D10_SVC_MATRIX_COLUMNS,
        D3D10_SVC_OBJECT,
        D3D10_SVC_STRUCT,
        D3D11_SVC_INTERFACE_CLASS,
        D3D11_SVC_INTERFACE_POINTER,
        D3D_SVC_FORCE_DWORD
    }

    //Aligns with EffectParameterType
    enum D3D_SHADER_VARIABLE_TYPE : ushort
    {
        D3D_SVT_VOID,
        D3D_SVT_BOOL,
        D3D_SVT_INT,
        D3D_SVT_FLOAT,
        D3D_SVT_STRING,
        D3D_SVT_TEXTURE,
        D3D_SVT_TEXTURE1D,
        D3D_SVT_TEXTURE2D,
        D3D_SVT_TEXTURE3D,
        D3D_SVT_TEXTURECUBE,
        D3D_SVT_SAMPLER,
        D3D_SVT_SAMPLER1D,
        D3D_SVT_SAMPLER2D,
        D3D_SVT_SAMPLER3D,
        D3D_SVT_SAMPLERCUBE,
        D3D_SVT_PIXELSHADER,
        D3D_SVT_VERTEXSHADER,
        D3D_SVT_PIXELFRAGMENT,
        D3D_SVT_VERTEXFRAGMENT,
        D3D_SVT_UINT,
        D3D_SVT_UINT8,
        D3D_SVT_GEOMETRYSHADER,
        D3D_SVT_RASTERIZER,
        D3D_SVT_DEPTHSTENCIL,
        D3D_SVT_BLEND,
        D3D_SVT_BUFFER,
        D3D_SVT_CBUFFER,
        D3D_SVT_TBUFFER,
        D3D_SVT_TEXTURE1DARRAY,
        D3D_SVT_TEXTURE2DARRAY,
        D3D_SVT_RENDERTARGETVIEW,
        D3D_SVT_DEPTHSTENCILVIEW,
        D3D_SVT_TEXTURE2DMS,
        D3D_SVT_TEXTURE2DMSARRAY,
        D3D_SVT_TEXTURECUBEARRAY,
        D3D_SVT_HULLSHADER,
        D3D_SVT_DOMAINSHADER,
        D3D_SVT_INTERFACE_POINTER,
        D3D_SVT_COMPUTESHADER,
        D3D_SVT_DOUBLE,
        D3D_SVT_RWTEXTURE1D,
        D3D_SVT_RWTEXTURE1DARRAY,
        D3D_SVT_RWTEXTURE2D,
        D3D_SVT_RWTEXTURE2DARRAY,
        D3D_SVT_RWTEXTURE3D,
        D3D_SVT_RWBUFFER,
        D3D_SVT_BYTEADDRESS_BUFFER,
        D3D_SVT_RWBYTEADDRESS_BUFFER,
        D3D_SVT_STRUCTURED_BUFFER,
        D3D_SVT_RWSTRUCTURED_BUFFER,
        D3D_SVT_APPEND_STRUCTURED_BUFFER,
        D3D_SVT_CONSUME_STRUCTURED_BUFFER,
        D3D_SVT_MIN8FLOAT,
        D3D_SVT_MIN10FLOAT,
        D3D_SVT_MIN16FLOAT,
        D3D_SVT_MIN12INT,
        D3D_SVT_MIN16INT,
        D3D_SVT_MIN16UINT,
        D3D_SVT_INT16,
        D3D_SVT_UINT16,
        D3D_SVT_FLOAT16,
        D3D_SVT_INT64,
        D3D_SVT_UINT64,
        D3D10_SVT_VOID,
        D3D10_SVT_BOOL,
        D3D10_SVT_INT,
        D3D10_SVT_FLOAT,
        D3D10_SVT_STRING,
        D3D10_SVT_TEXTURE,
        D3D10_SVT_TEXTURE1D,
        D3D10_SVT_TEXTURE2D,
        D3D10_SVT_TEXTURE3D,
        D3D10_SVT_TEXTURECUBE,
        D3D10_SVT_SAMPLER,
        D3D10_SVT_SAMPLER1D,
        D3D10_SVT_SAMPLER2D,
        D3D10_SVT_SAMPLER3D,
        D3D10_SVT_SAMPLERCUBE,
        D3D10_SVT_PIXELSHADER,
        D3D10_SVT_VERTEXSHADER,
        D3D10_SVT_PIXELFRAGMENT,
        D3D10_SVT_VERTEXFRAGMENT,
        D3D10_SVT_UINT,
        D3D10_SVT_UINT8,
        D3D10_SVT_GEOMETRYSHADER,
        D3D10_SVT_RASTERIZER,
        D3D10_SVT_DEPTHSTENCIL,
        D3D10_SVT_BLEND,
        D3D10_SVT_BUFFER,
        D3D10_SVT_CBUFFER,
        D3D10_SVT_TBUFFER,
        D3D10_SVT_TEXTURE1DARRAY,
        D3D10_SVT_TEXTURE2DARRAY,
        D3D10_SVT_RENDERTARGETVIEW,
        D3D10_SVT_DEPTHSTENCILVIEW,
        D3D10_SVT_TEXTURE2DMS,
        D3D10_SVT_TEXTURE2DMSARRAY,
        D3D10_SVT_TEXTURECUBEARRAY,
        D3D11_SVT_HULLSHADER,
        D3D11_SVT_DOMAINSHADER,
        D3D11_SVT_INTERFACE_POINTER,
        D3D11_SVT_COMPUTESHADER,
        D3D11_SVT_DOUBLE,
        D3D11_SVT_RWTEXTURE1D,
        D3D11_SVT_RWTEXTURE1DARRAY,
        D3D11_SVT_RWTEXTURE2D,
        D3D11_SVT_RWTEXTURE2DARRAY,
        D3D11_SVT_RWTEXTURE3D,
        D3D11_SVT_RWBUFFER,
        D3D11_SVT_BYTEADDRESS_BUFFER,
        D3D11_SVT_RWBYTEADDRESS_BUFFER,
        D3D11_SVT_STRUCTURED_BUFFER,
        D3D11_SVT_RWSTRUCTURED_BUFFER,
        D3D11_SVT_APPEND_STRUCTURED_BUFFER,
        D3D11_SVT_CONSUME_STRUCTURED_BUFFER,
        D3D_SVT_FORCE_DWORD
    }

}
