using System;
using System.Collections.Generic;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class ModelTransformOperation : TransformOperation
    {
        private EMD_File SourceFile;
        private IList<EMD_Submesh> SourceSubmeshes;
        private IList<Model.Xv2Submesh> CompiledSubmeshes;

        public ModelTransformOperation(IList<EMD_Submesh> sourceSubmeshes, IList<Model.Xv2Submesh> compiledSubmeshes, EMD_File sourceFile)
        {
            SourceSubmeshes = sourceSubmeshes;
            CompiledSubmeshes = compiledSubmeshes;
            SourceFile = sourceFile;
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"ModelTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = 0; i < SourceSubmeshes.Count; i++)
            {
                foreach (var vertex in SourceSubmeshes[i].Vertexes)
                {
                    float newX = vertex.PositionX + CompiledSubmeshes[i].Transform.Translation.X;
                    float newY = vertex.PositionY + CompiledSubmeshes[i].Transform.Translation.Y;
                    float newZ = vertex.PositionZ + CompiledSubmeshes[i].Transform.Translation.Z;

                    undos.Add(new UndoablePropertyGeneric(nameof(EMD_Vertex.PositionX), vertex, vertex.PositionX, newX));
                    undos.Add(new UndoablePropertyGeneric(nameof(EMD_Vertex.PositionY), vertex, vertex.PositionY, newY));
                    undos.Add(new UndoablePropertyGeneric(nameof(EMD_Vertex.PositionZ), vertex, vertex.PositionZ, newZ));

                    vertex.PositionX = newX;
                    vertex.PositionY = newY;
                    vertex.PositionZ = newZ;
                }

                CompiledSubmeshes[i].Transform = Matrix.Identity;
            }

            SourceFile.TriggerModelChanged();

            undos.Add(new UndoActionDelegate(SourceFile, nameof(SourceFile.TriggerModelChanged), true));
            UndoManager.Instance.AddCompositeUndo(undos, "Model Transform");

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"ModelTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            foreach (var model in CompiledSubmeshes)
                model.Transform = Matrix.Identity;

            IsFinished = true;
        }

        public override void UpdatePos(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;

                foreach (var submesh in CompiledSubmeshes)
                {
                    submesh.Transform *= Matrix.CreateTranslation(new Vector3(-delta.X, delta.Y, delta.Z));
                }
            }
        }
    }
}
