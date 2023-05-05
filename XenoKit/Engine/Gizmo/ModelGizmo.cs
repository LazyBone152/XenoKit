using System.Collections.Generic;
using Xv2CoreLib.EMD;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Model;
using XenoKit.Editor;

namespace XenoKit.Engine.Gizmo
{
    public class ModelGizmo : GizmoBase
    {
        //Has an unsolved bug so it is currently disabled
        //Only a single TransformOperation works correctly. Any further ones will not update properly. The end result is good, but it doesn't show anything while dragging the gizmo.

        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if (value is ModelTransformOperation modelTransformOp)
                {
                    transformOperation = modelTransformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }
        private ModelTransformOperation transformOperation = null;

        private EMD_File SourceFile = null;
        private IList<EMD_Submesh> SelectedSourceSubmeshes = null;
        private IList<Xv2Submesh> SelectedCompiledSubmeshes = null;

        //Settings
        protected override bool AllowRotation => false;
        protected override bool AllowScale => false;

        public ModelGizmo(GameBase gameBase) : base(gameBase)
        {
            
        }

        public void SetContext(IList<EMD_Submesh> selectedSourceSubmeshes, IList<Xv2Submesh> selectedCompiledSubmeshes, EMD_File sourceFile)
        {
            SelectedSourceSubmeshes = selectedSourceSubmeshes;
            SelectedCompiledSubmeshes = selectedCompiledSubmeshes;
            SourceFile = sourceFile;

            base.SetContext();
        }

        public void RemoveContext()
        {
            SetContext(null, null, null);
        }

        public override bool IsContextValid()
        {
            return SelectedSourceSubmeshes != null && SelectedCompiledSubmeshes != null && SourceFile != null;
        }

        protected override void StartTransformOperation()
        {
            if(SelectedCompiledSubmeshes != null && SelectedSourceSubmeshes != null)
            {
                transformOperation = new ModelTransformOperation(SelectedSourceSubmeshes, SelectedCompiledSubmeshes, SourceFile);
            }
        }
    }
}
