using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Engine.Gizmo
{
    internal enum GizmoAxis
    {
        None,
        X,
        Z,
        Y,
        XY,
        ZY,
        ZX,
        YZ
    }

    internal enum GizmoMode
    {
        Translate,
        Rotate,
        Scale
    }

    public class AnimatorGizmo
    {

        //Bone and character info
        public bool IsEnabled { get; private set; }
        private Character character = null;
        private string boneName = string.Empty;
        private int boneIdx = -1;
        private Matrix boneWorldMatrix { get { return character.animationPlayer.GetDebugBoneMatrices()[boneIdx]; } }

        //Keyframe
        private TransformOperation transformOperation = null;

        //Mouse state
        private bool isLeftClickHeld = false;

        //State
        private GizmoAxis ActiveAxis = GizmoAxis.None;
        private GizmoMode ActiveMode = GizmoMode.Translate;

        // -- Lines (Vertices) -- //
        private VertexPositionColor[] _translationLineVertices;
        private const float LINE_LENGTH = 3f;
        private const float LINE_OFFSET = 1f;

        // -- Quads -- //
        private Shapes.Quad[] _quads;
        private readonly BasicEffect _quadEffect;

        // -- Colors -- //
        private Color[] _axisColors;
        private Color _highlightColor;

        //Effects
        private BasicEffect _lineEffect;
        private BasicEffect _meshEffect;

        // -- Screen Scale -- //
        private Matrix _screenScaleMatrix;
        private float _screenScale;

        // -- Position - Rotation -- //
        private Vector3 _position = Vector3.Zero;
        private Matrix _rotationMatrix = Matrix.Identity;

        private Vector3 _localForward = Vector3.Forward;
        private Vector3 _localUp = Vector3.Up;
        private Vector3 _localRight;

        // -- Matrices -- //
        private Matrix _objectOrientedWorld;
        private Matrix _axisAlignedWorld;
        private Matrix[] _modelLocalSpace;
        private Matrix _gizmoWorld = Matrix.Identity;

        // -- Translation Variables -- //
        private Vector3 _tDelta;
        private Vector3 _lastIntersectionPosition;
        private Vector3 _intersectPosition;

        public bool PrecisionModeEnabled;
        private const float PRECISION_MODE_SCALE = 0.1f;

        #region BoundingBoxes

        private const float MULTI_AXIS_THICKNESS = 0.05f;
        private const float SINGLE_AXIS_THICKNESS = 0.2f;

        private static BoundingBox XAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(LINE_OFFSET, 0, 0),
                                       new Vector3(LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS));
            }
        }

        private static BoundingBox YAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, LINE_OFFSET, 0),
                                       new Vector3(SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS));
            }
        }

        private static BoundingBox ZAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, 0, LINE_OFFSET),
                                       new Vector3(SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH));
            }
        }

        private static BoundingBox XZAxisBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, MULTI_AXIS_THICKNESS, LINE_OFFSET));
            }
        }

        private BoundingBox XYBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, LINE_OFFSET, MULTI_AXIS_THICKNESS));
            }
        }

        private BoundingBox YZBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(MULTI_AXIS_THICKNESS, LINE_OFFSET, LINE_OFFSET));
            }
        }

        #endregion

        #region BoundingSpheres

        private const float RADIUS = 1f;
        
        private BoundingSphere XSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[1].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        private BoundingSphere YSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[7].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        private BoundingSphere ZSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[13].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        #endregion


        public AnimatorGizmo()
        {
            _lineEffect = new BasicEffect(SceneManager.GraphicsDeviceRef) { VertexColorEnabled = true, AmbientLightColor = Vector3.One, EmissiveColor = Vector3.One };
            _meshEffect = new BasicEffect(SceneManager.GraphicsDeviceRef);
            _quadEffect = new BasicEffect(SceneManager.GraphicsDeviceRef) { World = Matrix.Identity, DiffuseColor = _highlightColor.ToVector3(), Alpha = 0.5f };
            _quadEffect.EnableDefaultLighting();

            _modelLocalSpace = new Matrix[3];
            _modelLocalSpace[0] = Matrix.CreateWorld(new Vector3(LINE_LENGTH, 0, 0), Vector3.Left, Vector3.Up);
            _modelLocalSpace[1] = Matrix.CreateWorld(new Vector3(0, LINE_LENGTH, 0), Vector3.Down, Vector3.Left);
            _modelLocalSpace[2] = Matrix.CreateWorld(new Vector3(0, 0, LINE_LENGTH), Vector3.Forward, Vector3.Up);

            // -- Colors: X,Y,Z,Highlight -- //
            _axisColors = new Color[3];
            _axisColors[0] = Color.Red;
            _axisColors[1] = Color.Green;
            _axisColors[2] = Color.Blue;
            _highlightColor = Color.Gold;

            #region Fill Axis-Line array
            const float halfLineOffset = LINE_OFFSET / 2;
            var vertexList = new List<VertexPositionColor>(18);

            // helper to apply colors
            Color xColor = _axisColors[0];
            Color yColor = _axisColors[1];
            Color zColor = _axisColors[2];

            // -- X Axis -- // index 0 - 5
            vertexList.Add(new VertexPositionColor(new Vector3(halfLineOffset, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_LENGTH, 0, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), xColor));

            // -- Y Axis -- // index 6 - 11
            vertexList.Add(new VertexPositionColor(new Vector3(0, halfLineOffset, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_LENGTH, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), yColor));

            // -- Z Axis -- // index 12 - 17
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, halfLineOffset), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_LENGTH), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), zColor));

            // -- Convert to array -- //
            _translationLineVertices = vertexList.ToArray();

            #endregion


            #region Translucent Quads
            _quads = new Shapes.Quad[3];
            _quads[0] = new Shapes.Quad(new Vector3(halfLineOffset, halfLineOffset, 0), Vector3.Backward, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //XY
            _quads[1] = new Shapes.Quad(new Vector3(halfLineOffset, 0, halfLineOffset), Vector3.Up, Vector3.Right, LINE_OFFSET,
                                 LINE_OFFSET); //XZ
            _quads[2] = new Shapes.Quad(new Vector3(0, halfLineOffset, halfLineOffset), Vector3.Right, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //ZY 
            #endregion
        }

        public void Disbable()
        {
            IsEnabled = false;
            character = null;
            boneIdx = -1;
            boneName = string.Empty;
            Input.IsLeftClickHeldDown = false;
            SceneManager.CurrentSelectedBoneName = null;
            
            if(transformOperation != null)
            {
                if (!transformOperation.isFinished)
                    transformOperation.Cancel();

                transformOperation = null;
            }
        }

        public void Enable(Character _character, string boneName)
        {
            IsEnabled = true;
            character = _character;
            boneIdx = _character.Skeleton.GetBoneIndex(boneName);
            this.boneName = boneName;
            SceneManager.CurrentSelectedBoneName = boneName;
        }

        private void StartTransformOperation()
        {
            if(character?.animationPlayer.PrimaryAnimation != null)
                transformOperation = new TransformOperation(character?.animationPlayer, boneName);
        }

        protected void ResetDeltas()
        {
            _tDelta = Vector3.Zero;
            _lastIntersectionPosition = Vector3.Zero;
            _intersectPosition = Vector3.Zero;
        }

        public void Update(GameTime gameTime)
        {
            if (SceneManager.CurrentSceneState != EditorTabs.Animation || Input.IsKeyDown(Keys.Escape)) Disbable();

            if(!IsEnabled && transformOperation != null)
            {
                Disbable();
            }

            if (!IsEnabled) return;

            UpdateElements();
            UpdateMouse();

            _lastIntersectionPosition = _intersectPosition;

            if (isLeftClickHeld && ActiveAxis != GizmoAxis.None && transformOperation != null && !SceneManager.IsPlaying)
            {
                Matrix localWorld = transformOperation.GetLocalMatrix();

                switch (ActiveMode)
                {
                    case GizmoMode.Scale:
                    case GizmoMode.Translate:
                        {
                            #region Translate & Scale

                            Vector3 delta = Vector3.Zero;
                            Ray ray = EngineUtils.CalculateRay(Input.MousePosition);

                            Matrix transform = Matrix.Invert(_rotationMatrix);
                            ray.Position = Vector3.Transform(ray.Position, transform);
                            ray.Direction = Vector3.TransformNormal(ray.Direction, transform);


                            switch (ActiveAxis)
                            {
                                case GizmoAxis.XY:
                                case GizmoAxis.X:
                                    {
                                        Microsoft.Xna.Framework.Plane plane = new Plane(Vector3.Forward,
                                                                Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Z);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                            delta = ActiveAxis == GizmoAxis.X
                                                        ? new Vector3(_tDelta.X, 0, 0)
                                                        : new Vector3(_tDelta.X, _tDelta.Y, 0);
                                        }
                                    }
                                    break;
                                case GizmoAxis.Z:
                                case GizmoAxis.YZ:
                                case GizmoAxis.Y:
                                    {
                                        Plane plane = new Plane(Vector3.Left, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).X);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                            switch (ActiveAxis)
                                            {
                                                case GizmoAxis.Y:
                                                    delta = new Vector3(0, _tDelta.Y, 0);
                                                    break;
                                                case GizmoAxis.Z:
                                                    delta = new Vector3(0, 0, _tDelta.Z);
                                                    break;
                                                default:
                                                    delta = new Vector3(0, _tDelta.Y, _tDelta.Z);
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                case GizmoAxis.ZX:
                                    {
                                        Plane plane = new Plane(Vector3.Down, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Y);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                        }
                                        delta = new Vector3(_tDelta.X, 0, _tDelta.Z);
                                    }
                                    break;
                            }

                            if (PrecisionModeEnabled)
                            delta *= PRECISION_MODE_SCALE;

                            if (ActiveMode == GizmoMode.Translate)
                            {
                                // transform (local or world)
                                delta = Vector3.Transform(delta, _rotationMatrix);

                                transformOperation?.UpdatePos(delta);
                            }
                            else if (ActiveMode == GizmoMode.Scale)
                            {
                                // -- Apply Scale -- //
                                transformOperation?.UpdateScale(delta);
                            }
                            #endregion
                        }
                        break;
                    case GizmoMode.Rotate:
                        {
                            #region Rotate

                            float delta = (Input.MousePosition.X - Input.PreviousMouseState.X);
                            delta *= (float)gameTime.ElapsedGameTime.TotalSeconds;

                            if (PrecisionModeEnabled)
                                delta *= PRECISION_MODE_SCALE;

                            Quaternion rotAmount = Quaternion.CreateFromRotationMatrix(localWorld);

                            switch (ActiveAxis)
                            {
                                case GizmoAxis.X:
                                    rotAmount *= Quaternion.CreateFromAxisAngle(localWorld.Right, delta);
                                    break;
                                case GizmoAxis.Y:
                                    rotAmount *= Quaternion.CreateFromAxisAngle(localWorld.Up, delta);
                                    break;
                                case GizmoAxis.Z:
                                    rotAmount *= Quaternion.CreateFromAxisAngle(localWorld.Forward, delta);
                                    break;
                            }

                            //Update animation
                            transformOperation?.UpdateRot(rotAmount);
                            #endregion
                        }
                        break;
                }
            }
            else
            {
                UpdateGizmoMode();

                if (Input.MouseState.LeftButton == ButtonState.Released && Input.MouseState.RightButton == ButtonState.Released)
                    SelectAxis(Input.MousePosition);
            }

            // -- Reset Colors to default -- //
            ApplyColor(GizmoAxis.X, _axisColors[0]);
            ApplyColor(GizmoAxis.Y, _axisColors[1]);
            ApplyColor(GizmoAxis.Z, _axisColors[2]);

            // -- Apply Highlight -- //
            ApplyColor(ActiveAxis, _highlightColor);
        }

        private void UpdateMouse()
        {
            if(Input.MouseState.LeftButton == ButtonState.Pressed && !isLeftClickHeld && ActiveAxis != GizmoAxis.None)
            {
                if (SceneManager.IsPlaying)
                {
                    Log.Add($"Cannot use animator while playing! Pausing at current frame...", LogType.Warning);
                    SceneManager.IsPlaying = false;
                }

                Input.IsLeftClickHeldDown = true;
                isLeftClickHeld = true;
                ResetDeltas();
                StartTransformOperation();
            }

            if((Input.MouseState.LeftButton == ButtonState.Released && isLeftClickHeld) || (SceneManager.IsPlaying && isLeftClickHeld))
            {
                Input.IsLeftClickHeldDown = false;
                isLeftClickHeld = false;

                if (transformOperation?.Modified == true)
                    transformOperation.Confirm();
                else if (transformOperation != null)
                    transformOperation.Cancel();

                ResetDeltas();
            }
        }

        private void UpdateElements()
        {
            _position = new Vector3(-boneWorldMatrix.Translation.X, boneWorldMatrix.Translation.Y, boneWorldMatrix.Translation.Z);

            Vector3 vLength = SceneManager.CameraInstance.Position - _position;

            //Force elements to have a min size
            const float minDistance = 0.5f;
            if(Vector3.Distance(SceneManager.CameraInstance.Position, _position) < minDistance)
            {
                vLength = new Vector3(0, 0, minDistance);
            }

            const float scaleFactor = 25.0f;

            _screenScale = vLength.Length() / scaleFactor;
            _screenScaleMatrix = Matrix.CreateScale(new Vector3(_screenScale));

            _localForward = boneWorldMatrix.Forward;
            _localUp = boneWorldMatrix.Up;
            // -- Vector Rotation (Local/World) -- //
            _localForward.Normalize();
            _localRight = Vector3.Cross(_localForward, _localUp);
            _localUp = Vector3.Cross(_localRight, _localForward);
            _localRight.Normalize();
            _localUp.Normalize();

            // -- Create Both World Matrices -- //
            _objectOrientedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, _localForward, _localUp);
            //_axisAlignedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, Matrix.Identity.Forward, Matrix.Identity.Up);

            //Asign world (local)
            _gizmoWorld = _objectOrientedWorld;
            _rotationMatrix.Forward = _localForward;
            _rotationMatrix.Up = _localUp;
            _rotationMatrix.Right = _localRight;
        }

        private void UpdateGizmoMode()
        {
            if (Input.KeyboardState.IsKeyDown(Keys.R))
            {
                ActiveMode = GizmoMode.Rotate;
            }
            else if (Input.KeyboardState.IsKeyDown(Keys.T))
            {
                ActiveMode = GizmoMode.Translate;
            }
            else if (Input.KeyboardState.IsKeyDown(Keys.S))
            {
                ActiveMode = GizmoMode.Scale;
            }
        }

        public void Draw()
        {
            if (!IsEnabled) return;

            //Meshes
            for (int i = 0; i < 3; i++) //(order: x, y, z)
            {
                GizmoModel activeModel;
                switch (ActiveMode)
                {
                    case GizmoMode.Translate:
                        activeModel = Geometry.Translate;
                        break;
                    case GizmoMode.Rotate:
                        activeModel = Geometry.Rotate;
                        break;
                    default:
                        activeModel = Geometry.Scale;
                        break;
                }

                _meshEffect.World = _modelLocalSpace[i] * _gizmoWorld;
                _meshEffect.View = SceneManager.ViewMatrix;
                _meshEffect.Projection = SceneManager.ProjectionMatrix;

                _meshEffect.DiffuseColor = _axisColors[i].ToVector3();
                _meshEffect.EmissiveColor = _axisColors[i].ToVector3();

                _meshEffect.CurrentTechnique.Passes[0].Apply();

                foreach (var pass in _meshEffect.CurrentTechnique.Passes)
                {
                    SceneManager.GraphicsDeviceRef.RasterizerState = RasterizerState.CullClockwise;
                    SceneManager.GraphicsDeviceRef.BlendState = BlendState.Opaque;
                    SceneManager.GraphicsDeviceRef.DepthStencilState = DepthStencilState.None;
                    pass.Apply();

                    SceneManager.GraphicsDeviceRef.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                        activeModel.Vertices, 0, activeModel.Vertices.Length,
                        activeModel.Indices, 0, activeModel.Indices.Length / 3);
                }


            }

            //Lines
            _lineEffect.World = _gizmoWorld;
            _lineEffect.View = SceneManager.ViewMatrix;
            _lineEffect.Projection = SceneManager.ProjectionMatrix;

            foreach (var pass in _lineEffect.CurrentTechnique.Passes)
            {
                SceneManager.GraphicsDeviceRef.RasterizerState = RasterizerState.CullClockwise;
                SceneManager.GraphicsDeviceRef.BlendState = BlendState.Opaque;
                SceneManager.GraphicsDeviceRef.DepthStencilState = DepthStencilState.None;
                pass.Apply();

                SceneManager.GraphicsDeviceRef.DrawUserPrimitives(PrimitiveType.LineList, _translationLineVertices, 0,
                                             _translationLineVertices.Length / 2);
            }

            //Quads
            switch (ActiveMode)
            {
                case GizmoMode.Scale:
                case GizmoMode.Translate:
                    switch (ActiveAxis)
                    {
                        #region Draw Quads
                        case GizmoAxis.ZX:
                        case GizmoAxis.YZ:
                        case GizmoAxis.XY:
                            {
                                SceneManager.GraphicsDeviceRef.BlendState = BlendState.AlphaBlend;
                                SceneManager.GraphicsDeviceRef.RasterizerState = RasterizerState.CullNone;

                                _quadEffect.World = _gizmoWorld;
                                _quadEffect.View = SceneManager.ViewMatrix;
                                _quadEffect.Projection = SceneManager.ProjectionMatrix;

                                _quadEffect.CurrentTechnique.Passes[0].Apply();

                                Shapes.Quad activeQuad = new Shapes.Quad();
                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.XY:
                                        activeQuad = _quads[0];
                                        break;
                                    case GizmoAxis.ZX:
                                        activeQuad = _quads[1];
                                        break;
                                    case GizmoAxis.YZ:
                                        activeQuad = _quads[2];
                                        break;
                                }

                                SceneManager.GraphicsDeviceRef.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                                    activeQuad.Vertices, 0, 4,
                                                                    activeQuad.Indexes, 0, 2);

                                SceneManager.GraphicsDeviceRef.BlendState = BlendState.Opaque;
                                SceneManager.GraphicsDeviceRef.RasterizerState = RasterizerState.CullCounterClockwise;
                            }
                            break;
                            #endregion
                    }
                    break;
            }
        }


        /// <summary>
        /// Helper method for applying color to the gizmo lines.
        /// </summary>
        private void ApplyColor(GizmoAxis axis, Color color)
        {
            switch (ActiveMode)
            {
                case GizmoMode.Scale:
                case GizmoMode.Translate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                        case GizmoAxis.XY:
                            ApplyLineColor(0, 4, color);
                            ApplyLineColor(6, 4, color);
                            break;
                        case GizmoAxis.YZ:
                            ApplyLineColor(6, 2, color);
                            ApplyLineColor(12, 2, color);
                            ApplyLineColor(10, 2, color);
                            ApplyLineColor(16, 2, color);
                            break;
                        case GizmoAxis.ZX:
                            ApplyLineColor(0, 2, color);
                            ApplyLineColor(4, 2, color);
                            ApplyLineColor(12, 4, color);
                            break;
                    }
                    break;
                case GizmoMode.Rotate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply color on the lines associated with translation mode (re-used in Scale)
        /// </summary>
        private void ApplyLineColor(int startindex, int count, Color color)
        {
            for (int i = startindex; i < (startindex + count); i++)
            {
                _translationLineVertices[i].Color = color;
            }
        }


        /// <summary>
        /// Per-frame check to see if mouse is hovering over any axis.
        /// </summary>
        private void SelectAxis(Vector2 mousePosition)
        {
            if (!IsEnabled) return;

            float closestintersection = float.MaxValue;
            Ray ray = EngineUtils.CalculateRay(mousePosition);

            if (ActiveMode == GizmoMode.Translate)
            {
                // transform ray into local-space of the boundingboxes.
                ray.Direction = Vector3.TransformNormal(ray.Direction, Matrix.Invert(_gizmoWorld));
                ray.Position = Vector3.Transform(ray.Position, Matrix.Invert(_gizmoWorld));
            }

            #region X,Y,Z Boxes
            float? intersection = XAxisBox.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.X;
                    closestintersection = intersection.Value;
                }
            intersection = YAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Y;
                    closestintersection = intersection.Value;
                }
            }
            intersection = ZAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Z;
                    closestintersection = intersection.Value;
                }
            }
            #endregion

            if (ActiveMode == GizmoMode.Rotate || ActiveMode == GizmoMode.Scale)
            {
                #region BoundingSpheres

                intersection = XSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.X;
                        closestintersection = intersection.Value;
                    }
                intersection = YSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.Y;
                        closestintersection = intersection.Value;
                    }
                intersection = ZSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.Z;
                        closestintersection = intersection.Value;
                    }

                #endregion
            }
            if (ActiveMode == GizmoMode.Translate || ActiveMode == GizmoMode.Scale)
            {
                // if no axis was hit (x,y,z) set value to lowest possible to select the 'farthest' intersection for the XY,XZ,YZ boxes. 
                // This is done so you may still select multi-axis if you're looking at the gizmo from behind!
                if (closestintersection >= float.MaxValue)
                    closestintersection = float.MinValue;

                #region BoundingBoxes
                intersection = XYBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.XY;
                        closestintersection = intersection.Value;
                    }
                intersection = XZAxisBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.ZX;
                        closestintersection = intersection.Value;
                    }
                intersection = YZBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.YZ;
                        closestintersection = intersection.Value;
                    }
                #endregion
            }
            if (closestintersection >= float.MaxValue || closestintersection <= float.MinValue)
                ActiveAxis = GizmoAxis.None;
        
        }


        public bool IsEnabledOnBone(int bone)
        {
            return (bone == boneIdx);
        }
    }

    public class TransformOperation
    {
        private List<IUndoRedo> undos = new List<IUndoRedo>();
        public bool isFinished { get; private set; }
        public bool Modified { get; private set; }

        public EAN_Keyframe PosKeyframe;
        public EAN_Keyframe RotKeyframe;
        public EAN_Keyframe ScaleKeyframe;

        private float[] OriginalKeyframeValues;

        private EAN_Node node;
        private int frame;

        public TransformOperation(AnimationPlayer animator, string nodeName)
        {
            //Nodes, components and keyframes will all be created on the animation as an undoable operation.

            frame = animator.PrimaryAnimation.CurrentFrame_Int;
            node = animator.PrimaryAnimation.Animation.GetNode(nodeName, true, undos);
            PosKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Position, true, undos);
            RotKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Rotation, true, undos);
            ScaleKeyframe = node.GetKeyframe(frame, EAN_AnimationComponent.ComponentType.Scale, true, undos);

            OriginalKeyframeValues = node.GetKeyframeValues(frame);

        }

        public void Confirm()
        {
            if (isFinished)
                throw new InvalidOperationException($"TransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), PosKeyframe, OriginalKeyframeValues[0], PosKeyframe.X));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), PosKeyframe, OriginalKeyframeValues[1], PosKeyframe.Y));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), PosKeyframe, OriginalKeyframeValues[2], PosKeyframe.Z));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), PosKeyframe, OriginalKeyframeValues[3], PosKeyframe.W));

            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), RotKeyframe, OriginalKeyframeValues[4], RotKeyframe.X));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), RotKeyframe, OriginalKeyframeValues[5], RotKeyframe.Y));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), RotKeyframe, OriginalKeyframeValues[6], RotKeyframe.Z));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), RotKeyframe, OriginalKeyframeValues[7], RotKeyframe.W));

            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), ScaleKeyframe, OriginalKeyframeValues[8], ScaleKeyframe.X));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), ScaleKeyframe, OriginalKeyframeValues[9], ScaleKeyframe.Y));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), ScaleKeyframe, OriginalKeyframeValues[10], ScaleKeyframe.Z));
            undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), ScaleKeyframe, OriginalKeyframeValues[11], ScaleKeyframe.W));

            UndoManager.Instance.AddCompositeUndo(undos, "Animator");
            isFinished = true;
        }

        public void Cancel()
        {
            if (isFinished)
                throw new InvalidOperationException($"TransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            CompositeUndo undo = new CompositeUndo(undos, "");
            undo.Undo();

            isFinished = true;
        }
    
        public Matrix GetLocalMatrix()
        {
            Matrix local = Matrix.Identity;

            local *= Matrix.CreateScale(new Vector3(ScaleKeyframe.X, ScaleKeyframe.Y, ScaleKeyframe.Z) * ScaleKeyframe.W);
            local *= Matrix.CreateFromQuaternion(new Quaternion(RotKeyframe.X, RotKeyframe.Y, RotKeyframe.Z, RotKeyframe.W));
            local *= Matrix.CreateTranslation(new Vector3(PosKeyframe.X, PosKeyframe.Y, PosKeyframe.Z) * PosKeyframe.W);

            return local;
        }

        public void UpdatePos(Vector3 delta)
        {
            if(delta != Vector3.Zero)
            {
                Modified = true;
                PosKeyframe.ScaleByWorld();
                PosKeyframe.X += -delta.X;
                PosKeyframe.Y += delta.Y;
                PosKeyframe.Z += delta.Z;
            }
        }

        public void UpdateRot(Quaternion newRot)
        {
            if (newRot != new Quaternion(RotKeyframe.X, RotKeyframe.Y, RotKeyframe.Z, RotKeyframe.W))
            {
                Modified = true;
                RotKeyframe.X = newRot.X;
                RotKeyframe.Y = newRot.Y;
                RotKeyframe.Z = newRot.Z;
                RotKeyframe.W = newRot.W;
            }
        }

        public void UpdateScale(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                ScaleKeyframe.ScaleByWorld();
                ScaleKeyframe.X += delta.X;
                ScaleKeyframe.Y += delta.Y;
                ScaleKeyframe.Z += delta.Z;
            }
        }
    }
}
