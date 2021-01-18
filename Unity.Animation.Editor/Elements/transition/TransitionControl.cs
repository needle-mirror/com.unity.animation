using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class TransitionControlPart : BaseGraphElementPart
    {
        public static TransitionControlPart Create(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
        {
            if (model is IEdgeModel)
            {
                return new TransitionControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        public override VisualElement Root => m_TransitionControl;

        TransitionControl m_TransitionControl;

        protected TransitionControlPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            m_TransitionControl = new TransitionControl() { name = PartName };
            m_TransitionControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_TransitionControl.RegisterCallback<MouseEnterEvent>(OnMouseEnterEdge);
            m_TransitionControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEdge);

            container.Add(m_TransitionControl);
        }

        protected override void UpdatePartFromModel()
        {
            m_TransitionControl.UpdateLayout();
            UpdateTransitionControlColors();
            m_TransitionControl.MarkDirtyRepaint();
        }

        void UpdateTransitionControlColors()
        {
            var parent = m_OwnerElement as BaseTransition;

            if (parent?.Selected ?? false)
            {
                m_TransitionControl.ResetColor();
            }
            else
            {
                var inputColor = Color.white;
//
//                else if (edgeModel?.ToPort != null)
//                    outputColor = edgeModel.ToPort.GetUI<Port>(m_OwnerElement.GraphView)?.PortColor ?? Color.white;
//
//                if (parent?.IsGhostEdge ?? false)
//                {
//                    inputColor = new Color(inputColor.r, inputColor.g, inputColor.b, 0.5f);
//                }

                m_TransitionControl.SetColor(inputColor);
            }
        }

        void OnMouseEnterEdge(MouseEnterEvent e)
        {
            if (e.target == m_TransitionControl)
            {
                m_TransitionControl.ResetColor();
            }
        }

        void OnMouseLeaveEdge(MouseLeaveEvent e)
        {
            if (e.target == m_TransitionControl)
            {
                UpdateTransitionControlColors();
            }
        }
    }

    internal class TransitionControl : VisualElement
    {
        static CustomStyleProperty<int> s_TransitionWidthProperty = new CustomStyleProperty<int>("--transition-width");
        static CustomStyleProperty<Color> s_TransitionColorProperty = new CustomStyleProperty<Color>("--transition-color");
        static readonly int k_DefaultLineWidth = 2;
        static readonly Color k_DefaultColor = new Color(146 / 255f, 146 / 255f, 146 / 255f);
        static readonly float k_ContainsPointDistance = 25f;
        static readonly float k_ContainsPointDistanceSquare = k_ContainsPointDistance * k_ContainsPointDistance;

        BaseTransition m_Transition;

        bool IsTargetStateTransition => (IsSelfTransition || IsGlobalTransition || IsOnEnterSelector);
        bool IsSelfTransition => m_Transition.TransitionModel is SelfTransitionModel;
        bool IsGlobalTransition => m_Transition.TransitionModel is GlobalTransitionModel;
        bool IsOnEnterSelector => m_Transition.TransitionModel is OnEnterStateSelectorModel;


        Mesh m_Mesh;

        Color m_Color = Color.grey;
        bool m_ColorOverridden;
        bool m_WidthOverridden;
        int m_LineWidth = 2;

        public UnityEditor.GraphToolsFoundation.Overdrive.GraphView GraphView => m_Transition?.GraphView;

        int DefaultLineWidth { get; set; } = k_DefaultLineWidth;

        Color DefaultColor { get; set; } = k_DefaultColor;

        readonly float kTargetStateTransitionHeight = 150.0f;
        readonly float kSelfTransitionHeight = 100.0f;
        BaseTransition TransitionParent => m_Transition ?? (m_Transition = GetFirstAncestorOfType<BaseTransition>());

        // The start of the edge in graph coordinates.
        Vector2 From
        {
            get
            {
                if (IsTargetStateTransition)
                {
                    var toPt = TransitionParent?.To;
                    return new Vector2(toPt.Value.x, toPt.Value.y - (IsSelfTransition ? kSelfTransitionHeight : kTargetStateTransitionHeight));
                }
                return TransitionParent?.From ?? Vector2.zero;
            }
        }

        // The end of the edge in graph coordinates.
        Vector2 To => TransitionParent?.To ?? Vector2.zero;

        public Color Color
        {
            get => m_Color;
            private set
            {
                if (m_Color != value)
                {
                    m_Color = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public int LineWidth
        {
            get => m_LineWidth;
            set
            {
                m_WidthOverridden = true;

                if (m_LineWidth == value)
                    return;

                m_LineWidth = value;
                UpdateLayout(); // The layout depends on the edges width
                MarkDirtyRepaint();
            }
        }

        // The points that will be rendered. Expressed in coordinates local to the element.
        public List<Vector2> RenderPoints { get; } = new List<Vector2>();

        public TransitionControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;

            pickingMode = PickingMode.Position;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(s_TransitionWidthProperty, out var edgeWidthValue))
                DefaultLineWidth = edgeWidthValue;

            if (e.customStyle.TryGetValue(s_TransitionColorProperty, out var edgeColorValue))
                DefaultColor = edgeColorValue;

            if (!m_WidthOverridden)
            {
                m_LineWidth = DefaultLineWidth;
                UpdateLayout(); // The layout depends on the edges width
                MarkDirtyRepaint();
            }

            if (!m_ColorOverridden)
            {
                m_Color = DefaultColor;
                MarkDirtyRepaint();
            }
        }

        public void SetColor(Color color)
        {
            m_ColorOverridden = true;
            Color = color;
        }

        public void ResetColor()
        {
            m_ColorOverridden = false;
            Color = DefaultColor;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Profiler.BeginSample("DrawEdge");
            DrawEdge(mgc);
            Profiler.EndSample();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Vector2 toPt = To;
            Vector2 fromPt = From;

            float distanceInX;
            if (fromPt == Vector2.zero)
            {
                distanceInX = localPoint.x - toPt.x;
                return (distanceInX * distanceInX < k_ContainsPointDistanceSquare);
            }

            distanceInX = toPt.x - fromPt.x;
            var distanceInY = toPt.y - fromPt.y;
            var distanceInXSq = distanceInX * distanceInX;
            var distanceInYSq = distanceInY * distanceInY;
            if (distanceInXSq < float.Epsilon && distanceInYSq < float.Epsilon)
                return localPoint == Vector2.zero;
            var numerator = distanceInY * localPoint.x - distanceInX * localPoint.y + toPt.x * fromPt.y - toPt.y * fromPt.x;
            return (numerator * numerator) / (distanceInXSq + distanceInYSq) < k_ContainsPointDistanceSquare;
        }

        public override bool Overlaps(Rect rect)
        {
            if (base.Overlaps(rect))
            {
                for (int a = 0; a < RenderPoints.Count - 1; a++)
                {
                    if (RectUtils.IntersectsSegment(rect, RenderPoints[a], RenderPoints[a + 1]))
                        return true;
                }
            }

            return false;
        }

        public void UpdateLayout()
        {
            if (parent != null)
                ComputeLayout();
        }

        readonly float kLengthArrow = 25.0f;
        readonly float kLengthOpening = 15.0f;

        protected virtual void UpdateRenderPoints()
        {
            ComputeLayout();

            RenderPoints.Clear();

            Vector2 fromPoint = parent.ChangeCoordinatesTo(this, From);
            Vector2 toPoint = parent.ChangeCoordinatesTo(this, To);

            if (!m_Transition.IsGhostTransition)
            {
                if (IsSelfTransition)
                {
                    Vector2 startLooping = new Vector2(toPoint.x + kLengthOpening, toPoint.y);
                    RenderPoints.Add(startLooping);
                    Vector2 upFromStart = new Vector2(startLooping.x, startLooping.y - kSelfTransitionHeight);
                    RenderPoints.Add(upFromStart);
                    RenderPoints.Add(upFromStart);
                    Vector2 upToEnd = new Vector2(toPoint.x - kLengthOpening, startLooping.y - kSelfTransitionHeight);
                    RenderPoints.Add(upToEnd);
                    RenderPoints.Add(upToEnd);
                    Vector2 endLooping = new Vector2(toPoint.x - kLengthOpening, toPoint.y);
                    RenderPoints.Add(endLooping);
                    RenderPoints.Add(endLooping);

                    DrawArrow(upToEnd, endLooping);
                    return;
                }
                else if (IsOnEnterSelector)
                {
                    RenderPoints.Add(fromPoint);
                    Vector2 endArrow = new Vector2(toPoint.x, toPoint.y - kTargetStateTransitionHeight / 8);
                    RenderPoints.Add(endArrow);
                    RenderPoints.Add(endArrow);
                    DrawArrow(fromPoint, endArrow);
                    Vector2 startRectangle = new Vector2(toPoint.x, toPoint.y - kTargetStateTransitionHeight / 8 - kLengthArrow - 5);
                    RenderPoints.Add(startRectangle);
                    RenderPoints.Add(startRectangle);
                    Vector2 rectangleLeft = new Vector2(toPoint.x - 20.0f, startRectangle.y);
                    RenderPoints.Add(rectangleLeft);
                    RenderPoints.Add(rectangleLeft);
                    Vector2 rectangleBottomLeft = new Vector2(toPoint.x - 20.0f, startRectangle.y + 40.0f);
                    RenderPoints.Add(rectangleBottomLeft);
                    RenderPoints.Add(rectangleBottomLeft);
                    Vector3 rectangleBottomRight = new Vector2(toPoint.x + 20.0f, rectangleBottomLeft.y);
                    RenderPoints.Add(rectangleBottomRight);
                    RenderPoints.Add(rectangleBottomRight);
                    Vector3 rectangleRight = new Vector2(rectangleBottomRight.x, startRectangle.y);
                    RenderPoints.Add(rectangleRight);
                    RenderPoints.Add(rectangleRight);
                    RenderPoints.Add(startRectangle);
                    return;
                }
            }

            RenderPoints.Add(fromPoint);
            RenderPoints.Add(toPoint);

            DrawArrow(fromPoint, toPoint);
        }

        void DrawArrow(Vector2 fromPoint, Vector2 toPoint)
        {
            //@todo check with vertical line, normalize might return infinite or 0
            Vector2 dir = toPoint - fromPoint;
            var dirNormalized = dir.normalized;
            var arrowStartOnDir = toPoint - dirNormalized * kLengthArrow;
            var perpendicularDirNormalized = new Vector2(-dirNormalized.y, dirNormalized.x);
            var arrowOpen1 = arrowStartOnDir + perpendicularDirNormalized * kLengthOpening;
            var arrowOpen2 = arrowStartOnDir - perpendicularDirNormalized * kLengthOpening;

            RenderPoints.Add(toPoint);
            RenderPoints.Add(arrowOpen1);
            RenderPoints.Add(arrowOpen1);
            RenderPoints.Add(toPoint);
            RenderPoints.Add(arrowOpen2);
            RenderPoints.Add(arrowOpen2);
            RenderPoints.Add(toPoint);
        }

        readonly float kWidthOnEachSideOfTargetStateTransition = 5.0f;
        void ComputeLayout()
        {
            // Compute VisualElement position and dimension.
            var transitionModel = TransitionParent?.TransitionModel;

            if (transitionModel == null)
            {
                style.top = 0;
                style.left = 0;
                style.width = 0;
                style.height = 0;
                return;
            }

            Rect rect = new Rect(From, Vector2.zero);
            rect.xMin = Math.Min(From.x, To.x);
            rect.xMax = Math.Max(From.x, To.x);
            rect.yMin = Math.Min(From.y, To.y);
            rect.yMax = Math.Max(From.y, To.y);

            var p = rect.position;
            var dim = rect.size;

            if (IsTargetStateTransition)
            {
                p.x -= kWidthOnEachSideOfTargetStateTransition;
                dim.x += kWidthOnEachSideOfTargetStateTransition * 2;
            }

            style.left = p.x;
            style.top = p.y;
            style.width = dim.x;
            style.height = dim.y;
        }

        void DrawEdge(MeshGenerationContext mgc)
        {
            if (LineWidth <= 0)
                return;

            UpdateRenderPoints();
            if (RenderPoints.Count == 0)
                return; // Don't draw anything

            Color color = Color;

#if UNITY_EDITOR
            color *= GraphViewStaticBridge.EditorPlayModeTint;
#endif // UNITY_EDITOR

            uint cpt = (uint)RenderPoints.Count;
            uint wantedLength = (cpt) * 2;
            uint indexCount = (wantedLength - 2) * 3;

            var md = GraphViewStaticBridge.AllocateMeshWriteData(mgc, (int)wantedLength, (int)indexCount);
            if (md.vertexCount == 0)
                return;

            float polyLineLength = 0;
            for (int i = 1; i < cpt; ++i)
                polyLineLength += (RenderPoints[i - 1] - RenderPoints[i]).sqrMagnitude;

            float halfWidth = LineWidth * 0.5f;
            float currentLength = 0;

            Vector2 unitPreviousSegment = Vector2.zero;
            for (int i = 0; i < cpt; ++i)
            {
                Vector2 dir;
                Vector2 unitNextSegment = Vector2.zero;
                Vector2 nextSegment = Vector2.zero;

                if (i < cpt - 1)
                {
                    nextSegment = (RenderPoints[i + 1] - RenderPoints[i]);
                    unitNextSegment = nextSegment.normalized;
                }


                if (i > 0 && i < cpt - 1)
                {
                    dir = unitPreviousSegment + unitNextSegment;
                    dir.Normalize();
                }
                else if (i > 0)
                {
                    dir = unitPreviousSegment;
                }
                else
                {
                    dir = unitNextSegment;
                }

                Vector2 pos = RenderPoints[i];
                Vector2 uv = new Vector2(dir.y * halfWidth, -dir.x * halfWidth); // Normal scaled by half width
                Color32 tint = Color.LerpUnclamped(color, color, currentLength / polyLineLength);

                md.SetNextVertex(new Vector3(pos.x, pos.y, 1), uv, tint);
                md.SetNextVertex(new Vector3(pos.x, pos.y, -1), uv, tint);

                if (i < cpt - 2)
                {
                    currentLength += nextSegment.sqrMagnitude;
                }
                else
                {
                    currentLength = polyLineLength;
                }

                unitPreviousSegment = unitNextSegment;
            }

            // Fill triangle indices as it is a triangle strip
            for (uint i = 0; i < wantedLength - 2; ++i)
            {
                if ((i & 0x01) == 0)
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 2));
                    md.SetNextIndex((UInt16)(i + 1));
                }
                else
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 1));
                    md.SetNextIndex((UInt16)(i + 2));
                }
            }
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (m_Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Mesh);
                m_Mesh = null;
            }
        }
    }
}
