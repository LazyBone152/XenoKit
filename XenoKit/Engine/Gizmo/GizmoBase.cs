using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Shapes;
using Plane = Microsoft.Xna.Framework.Plane;

namespace XenoKit.Engine.Gizmo
{
    public enum GizmoAxis
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

    public enum GizmoMode
    {
        Translate,
        Rotate,
        Scale
    }

    public abstract class GizmoBase : Entity
    {
        public bool IsVisible { get; protected set; }
        private bool WasAutoHidden { get; set; }
        protected virtual Matrix WorldMatrix { get; } = Matrix.Identity;

        //Mouse state
        protected bool IsLeftClickHeld
        {
            get => Input.LeftClickHeldDownContext == this;
            set => Input.LeftClickHeldDownContext = value ? this : null;
        }
        //State
        protected GizmoAxis ActiveAxis = GizmoAxis.None;
        protected GizmoMode ActiveMode = GizmoMode.Translate;

        #region Stuff
        // -- Lines (Vertices) -- //
        protected VertexPositionColor[] _translationLineVertices;
        protected const float LINE_LENGTH = 3f;
        protected const float LINE_OFFSET = 1f;

        // -- Quads -- //
        protected Quad[] _quads;
        protected readonly BasicEffect _quadEffect;

        // -- Colors -- //
        protected Color[] _axisColors;
        protected Color _highlightColor;

        //Effects
        protected BasicEffect _lineEffect;
        protected BasicEffect _meshEffect;

        // -- Screen Scale -- //
        protected Matrix _screenScaleMatrix;
        protected float _screenScale;

        // -- Position - Rotation -- //
        protected Vector3 _position = Vector3.Zero;
        protected Matrix _rotationMatrix = Matrix.Identity;

        protected Vector3 _localForward = Vector3.Forward;
        protected Vector3 _localUp = Vector3.Up;
        protected Vector3 _localRight;

        // -- Matrices -- //
        protected Matrix _objectOrientedWorld;
        protected Matrix _axisAlignedWorld;
        protected Matrix[] _modelLocalSpace;
        protected Matrix _gizmoWorld = Matrix.Identity;

        // -- Translation Variables -- //
        protected Vector3 _tDelta;
        protected Vector3 _lastIntersectionPosition;
        protected Vector3 _intersectPosition;

        public bool PrecisionModeEnabled;
        protected const float PRECISION_MODE_SCALE = 0.1f;
        #endregion

        #region BoundingBoxes

        protected const float MULTI_AXIS_THICKNESS = 0.05f;
        protected const float SINGLE_AXIS_THICKNESS = 0.2f;

        protected static BoundingBox XAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(LINE_OFFSET, 0, 0),
                                       new Vector3(LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS));
            }
        }

        protected static BoundingBox YAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, LINE_OFFSET, 0),
                                       new Vector3(SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS));
            }
        }

        protected static BoundingBox ZAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, 0, LINE_OFFSET),
                                       new Vector3(SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH));
            }
        }

        protected static BoundingBox XZAxisBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, MULTI_AXIS_THICKNESS, LINE_OFFSET));
            }
        }

        protected BoundingBox XYBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, LINE_OFFSET, MULTI_AXIS_THICKNESS));
            }
        }

        protected BoundingBox YZBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(MULTI_AXIS_THICKNESS, LINE_OFFSET, LINE_OFFSET));
            }
        }

        #endregion

        #region BoundingSpheres

        protected const float RADIUS = 1f;

        protected BoundingSphere XSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[1].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        protected BoundingSphere YSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[7].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        protected BoundingSphere ZSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[13].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        #endregion

        protected virtual ITransformOperation TransformOperation { get; set; }

        //Settings
        protected virtual bool AutoPause => false;
        protected virtual bool AllowTranslate => true;
        protected virtual bool AllowRotation => true;
        protected virtual bool AllowScale => true;

        public GizmoBase(GameBase gameBase) : base(gameBase)
        {

            _lineEffect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true, AmbientLightColor = Vector3.One, EmissiveColor = Vector3.One };
            _meshEffect = new BasicEffect(GraphicsDevice);
            _quadEffect = new BasicEffect(GraphicsDevice) { World = Matrix.Identity, DiffuseColor = _highlightColor.ToVector3(), Alpha = 0.5f };
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

            if (!AllowTranslate && ActiveMode == GizmoMode.Translate)
                ActiveMode = GizmoMode.Rotate;

            if (!AllowRotation && ActiveMode == GizmoMode.Rotate)
                ActiveMode = GizmoMode.Scale;

            if (!AllowTranslate && !AllowRotation && !AllowScale)
                throw new ArgumentException("GizmoBase: Translate, Rotate and Scale are all not allowed on derived class.");
        }

        public void Enable()
        {
            IsVisible = IsContextValid();
            WasAutoHidden = false;
        }

        public void Disable()
        {
            if (TransformOperation != null)
            {
                if (!TransformOperation.IsFinished)
                    TransformOperation.Cancel();

                TransformOperation = null;
            }

            if (Input.LeftClickHeldDownContext == this)
                Input.LeftClickHeldDownContext = null;

            IsVisible = false;
            WasAutoHidden = false;
        }

        public void SetContext()
        {
            Enable();
        }

        public virtual bool IsContextValid()
        {
            return false;
        }

        public override void Draw()
        {
            if (!IsVisible) return;

            //Meshes
            for (int i = 0; i < 3; i++) //(order: x, y, z)
            {
                GizmoGeometry activeModel;
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
                _meshEffect.View = GameBase.ActiveCameraBase.ViewMatrix;
                _meshEffect.Projection = GameBase.ActiveCameraBase.ProjectionMatrix;

                _meshEffect.DiffuseColor = _axisColors[i].ToVector3();
                _meshEffect.EmissiveColor = _axisColors[i].ToVector3();

                _meshEffect.CurrentTechnique.Passes[0].Apply();

                foreach (var pass in _meshEffect.CurrentTechnique.Passes)
                {
                    GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    pass.Apply();

                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                        activeModel.Vertices, 0, activeModel.Vertices.Length,
                        activeModel.Indices, 0, activeModel.Indices.Length / 3);
                }


            }

            //Lines
            _lineEffect.World = _gizmoWorld;
            _lineEffect.View = GameBase.ActiveCameraBase.ViewMatrix;
            _lineEffect.Projection = GameBase.ActiveCameraBase.ProjectionMatrix;

            foreach (var pass in _lineEffect.CurrentTechnique.Passes)
            {
                GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                pass.Apply();

                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _translationLineVertices, 0,
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
                                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                                GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                                _quadEffect.World = _gizmoWorld;
                                _quadEffect.View = GameBase.ActiveCameraBase.ViewMatrix;
                                _quadEffect.Projection = GameBase.ActiveCameraBase.ProjectionMatrix;

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

                                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                                    activeQuad.Vertices, 0, 4,
                                                                    activeQuad.Indexes, 0, 2);

                                GraphicsDevice.BlendState = BlendState.Opaque;
                                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                            }
                            break;
                            #endregion
                    }
                    break;
            }
        }
        
        public override void Update()
        {
            if (IsVisible && Input.IsKeyDown(Keys.Escape))
            {
                Disable();
            }

            if (!IsContextValid() && IsVisible)
            {
                Disable();
                WasAutoHidden = true;
            }

            if (!IsVisible)
            {
                //Attempt to enable control if valid input
                if (Input.IsKeyDown(Keys.R) || Input.IsKeyDown(Keys.T) || Input.IsKeyDown(Keys.S) || (WasAutoHidden && IsContextValid()))
                {
                    Enable();
                }
            }

            if (!IsVisible) return;

            UpdateElements();
            UpdateMouse();

            _lastIntersectionPosition = _intersectPosition;

            if (IsLeftClickHeld && ActiveAxis != GizmoAxis.None && TransformOperation != null && ((!SceneManager.IsPlaying && AutoPause) || !AutoPause))
            {
                switch (ActiveMode)
                {
                    case GizmoMode.Scale:
                    case GizmoMode.Translate:
                        {
                            Vector3 delta = Vector3.Zero;
                            Ray ray = EngineUtils.CalculateRay(Input.MousePosition, GameBase);

                            Matrix transform = Matrix.Invert(_rotationMatrix);
                            ray.Position = Vector3.Transform(ray.Position, transform);
                            ray.Direction = Vector3.TransformNormal(ray.Direction, transform);


                            switch (ActiveAxis)
                            {
                                case GizmoAxis.XY:
                                case GizmoAxis.X:
                                    {
                                        Plane plane = new Plane(Vector3.Forward,
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

                                TransformOperation?.UpdatePos(delta);
                            }
                            else if (ActiveMode == GizmoMode.Scale)
                            {
                                // -- Apply Scale -- //
                                TransformOperation?.UpdateScale(delta);
                            }
                        }
                        break;
                    case GizmoMode.Rotate:
                        {
                            float delta = (Input.MousePosition.X - Input.PreviousMouseState.X);
                            delta *= (float)ElapsedTime.ElapsedGameTime.TotalSeconds;

                            if (PrecisionModeEnabled)
                                delta *= PRECISION_MODE_SCALE;

                            if(TransformOperation?.RotationType == RotationType.Quaternion)
                            {
                                Quaternion rotAmount = TransformOperation.GetRotation();

                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.X:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Left, delta);
                                        break;
                                    case GizmoAxis.Y:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Up, delta);
                                        break;
                                    case GizmoAxis.Z:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Forward, delta);
                                        break;
                                }

                                //Update animation
                                TransformOperation?.UpdateRot(rotAmount);
                            }
                            else if(TransformOperation?.RotationType == RotationType.EulerAngles)
                            {
                                Vector3 rotAmount = TransformOperation.GetRotationAngles();

                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.X:
                                        rotAmount.X += 50f * delta;
                                        break;
                                    case GizmoAxis.Y:
                                        rotAmount.Y += 50f * delta;
                                        break;
                                    case GizmoAxis.Z:
                                        rotAmount.Z += 50f * delta;
                                        break;
                                }

                                if (rotAmount.X > 360)
                                    rotAmount.X -= 360;
                                if (rotAmount.Y > 360)
                                    rotAmount.Y -= 360;
                                if (rotAmount.Z > 360)
                                    rotAmount.Z -= 360;

                                if (rotAmount.X < 0f)
                                    rotAmount.X += 360;
                                if (rotAmount.Y < 0f)
                                    rotAmount.Y += 360;
                                if (rotAmount.Z < 0f)
                                    rotAmount.Z += 360;

                                //Update animation
                                TransformOperation?.UpdateRot(rotAmount);
                            }
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

        protected virtual void UpdateMouse()
        {
            if (Input.MouseState.LeftButton == ButtonState.Pressed && Input.LeftClickHeldDownContext != this && ActiveAxis != GizmoAxis.None)
            {
                if (SceneManager.IsPlaying && AutoPause)
                {
                    SceneManager.IsPlaying = false;
                }

                Input.LeftClickHeldDownContext = this;
                ResetDeltas();
                StartTransformOperation();
            }

            if ((Input.MouseState.LeftButton == ButtonState.Released && Input.LeftClickHeldDownContext == this) || ((SceneManager.IsPlaying && AutoPause) && Input.LeftClickHeldDownContext == this))
            {
                Input.LeftClickHeldDownContext = null;

                if (TransformOperation?.Modified == true)
                    TransformOperation.Confirm();
                else if (TransformOperation != null)
                    TransformOperation.Cancel();

                TransformOperation = null;
                ResetDeltas();
            }
        }

        protected abstract void StartTransformOperation();

        protected void ResetDeltas()
        {
            _tDelta = Vector3.Zero;
            _lastIntersectionPosition = Vector3.Zero;
            _intersectPosition = Vector3.Zero;
        }

        protected void UpdateElements()
        {
            _position = new Vector3(-WorldMatrix.Translation.X, WorldMatrix.Translation.Y, WorldMatrix.Translation.Z);

            Vector3 vLength = GameBase.ActiveCameraBase.CameraState.Position - _position;

            //Force elements to have a min size
            const float minDistance = 0.5f;
            if (Vector3.Distance(GameBase.ActiveCameraBase.CameraState.Position, _position) < minDistance)
            {
                //vLength = new Vector3(0, 0, minDistance);
            }

            const float scaleFactor = 25.0f;

            _screenScale = vLength.Length() / scaleFactor;
            _screenScaleMatrix = Matrix.CreateScale(new Vector3(_screenScale));

            _localForward = WorldMatrix.Forward;
            _localUp = WorldMatrix.Up;
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

        protected void UpdateGizmoMode()
        {
            if (Input.KeyboardState.IsKeyDown(Keys.R) && AllowRotation)
            {
                ActiveMode = GizmoMode.Rotate;
            }
            else if (Input.KeyboardState.IsKeyDown(Keys.T) && AllowTranslate)
            {
                ActiveMode = GizmoMode.Translate;
            }
            else if (Input.KeyboardState.IsKeyDown(Keys.S) && AllowScale)
            {
                ActiveMode = GizmoMode.Scale;
            }
        }

        /// <summary>
        /// Helper method for applying color to the gizmo lines.
        /// </summary>
        protected void ApplyColor(GizmoAxis axis, Color color)
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
        protected void SelectAxis(Vector2 mousePosition)
        {
            if (!IsVisible) return;

            float closestintersection = float.MaxValue;
            Ray ray = EngineUtils.CalculateRay(mousePosition, GameBase);

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

        public virtual bool IsEnabledOnBone(int bone)
        {
            return false;
        }
    }
}
