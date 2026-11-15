using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Blumgames.UI
{
    /// <summary>
    /// Nine-sliced geometry driven by the existing Image's fill settings.
    /// Fill Amount reveals the full-size sliced image without resizing its borders or UVs.
    /// Place before other mesh effects such as Shadow and Outline.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Image))]
    [AddComponentMenu("")]
    [MovedFrom(true, "GangsPixel.Gameplay", "Assembly-CSharp", "SlicedFill")]
    public sealed class SlicedFill : BaseMeshEffect
    {
        readonly Vector2[] positions = new Vector2[4];
        readonly Vector2[] uvs = new Vector2[4];
        readonly List<UIVertex> mask = new List<UIVertex>(24);
        readonly UIVertex[] polygonA = new UIVertex[8];
        readonly UIVertex[] polygonB = new UIVertex[8];

        protected override void OnEnable()
        {
#if UNITY_EDITOR
            hideFlags |= HideFlags.HideInInspector;
#endif
            base.OnEnable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            hideFlags |= HideFlags.HideInInspector;
            base.OnValidate();
        }
#endif

        public override void ModifyMesh(VertexHelper mesh)
        {
            var image = graphic as UnityEngine.UI.Image;
            if (!IsActive() || image == null || image.type != UnityEngine.UI.Image.Type.Filled) return;
            Sprite sprite = image.overrideSprite;
            if (sprite == null || sprite.border.sqrMagnitude == 0) return;
            if (image.fillAmount < .001f) { mesh.Clear(); return; }

            Rect fullRect = image.GetPixelAdjustedRect();
            if (fullRect.width <= 0 || fullRect.height <= 0) { mesh.Clear(); return; }
            bool horizontal = image.fillMethod == UnityEngine.UI.Image.FillMethod.Horizontal;
            bool vertical = image.fillMethod == UnityEngine.UI.Image.FillMethod.Vertical;
            bool linear = (horizontal || vertical) && image.fillAmount < 1;
            bool radial = !horizontal && !vertical && image.fillAmount < 1;
            // Slice the complete image first; only the reveal edge moves with Fill Amount.
            int axisIndex = horizontal ? 0 : 1;
            bool reverse = image.fillOrigin == 1;
            float edge = Mathf.Lerp(fullRect.min[axisIndex], fullRect.max[axisIndex],
                reverse ? 1 - image.fillAmount : image.fillAmount);
            Vector2 clipStart = horizontal ? new Vector2(edge, 0) : new Vector2(0, edge);
            Vector2 clipEnd = clipStart + (horizontal ? Vector2.up : Vector2.right);
            float clipWinding = (horizontal ? 1 : -1) * (reverse ? -1 : 1);
            Vector4 outer = DataUtility.GetOuterUV(sprite);
            if (radial)
            {
                // Use Unity's own radial sectors (all origins and clockwise settings).
                mask.Clear();
                mesh.GetUIVertexStream(mask);
                for (int i = 0; i < mask.Count; i++)
                {
                    UIVertex v = mask[i];
                    v.position = new Vector3(
                        Mathf.Lerp(fullRect.xMin, fullRect.xMax, Mathf.InverseLerp(outer.x, outer.z, v.uv0.x)),
                        Mathf.Lerp(fullRect.yMin, fullRect.yMax, Mathf.InverseLerp(outer.y, outer.w, v.uv0.y)));
                    mask[i] = v;
                }
            }
            float ppu = Mathf.Max(.0001f, image.pixelsPerUnit * image.pixelsPerUnitMultiplier);
            Vector4 border = sprite.border / ppu;
            Vector4 padding = DataUtility.GetPadding(sprite) / ppu;
            Rect originalRect = image.rectTransform.rect;
            for (int axis = 0; axis < 2; axis++)
            {
                float pixelRatio = originalRect.size[axis] == 0 ? 1 : fullRect.size[axis] / originalRect.size[axis];
                border[axis] *= pixelRatio;
                border[axis + 2] *= pixelRatio;
                float sum = border[axis] + border[axis + 2];
                if (sum > fullRect.size[axis])
                {
                    float compression = fullRect.size[axis] / sum;
                    border[axis] *= compression;
                    border[axis + 2] *= compression;
                    padding[axis] *= compression;
                    padding[axis + 2] *= compression;
                }
            }

            Vector4 inner = DataUtility.GetInnerUV(sprite);
            positions[0] = fullRect.min + new Vector2(padding.x, padding.y);
            positions[1] = fullRect.min + new Vector2(border.x, border.y);
            positions[2] = fullRect.max - new Vector2(border.z, border.w);
            positions[3] = fullRect.max - new Vector2(padding.z, padding.w);
            uvs[0] = new Vector2(outer.x, outer.y);
            uvs[1] = new Vector2(inner.x, inner.y);
            uvs[2] = new Vector2(inner.z, inner.w);
            uvs[3] = new Vector2(outer.z, outer.w);
            mesh.Clear();
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                if (!image.fillCenter && x == 1 && y == 1) continue;
                if (positions[x + 1].x <= positions[x].x || positions[y + 1].y <= positions[y].y) continue;
                if (!radial)
                {
                    SetQuad(x, y, image.color);
                    if (linear)
                    {
                        int count = Clip(polygonA, 4, polygonB, clipStart, clipEnd, clipWinding);
                        AddPolygon(mesh, polygonB, count);
                    }
                    else AddPolygon(mesh, polygonA, 4);
                    continue;
                }
                for (int i = 0; i + 2 < mask.Count; i += 3)
                {
                    SetQuad(x, y, image.color);
                    ClipQuad(mesh, mask[i].position, mask[i + 1].position, mask[i + 2].position);
                }
            }
        }

        void SetQuad(int x, int y, Color32 color)
        {
            SetVertex(0, x, y, color);
            SetVertex(1, x, y + 1, color);
            SetVertex(2, x + 1, y + 1, color);
            SetVertex(3, x + 1, y, color);
        }

        void SetVertex(int index, int x, int y, Color32 color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(positions[x].x, positions[y].y);
            vertex.uv0 = new Vector2(uvs[x].x, uvs[y].y);
            vertex.color = color;
            polygonA[index] = vertex;
        }

        void ClipQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) < .000001f) return;
            float winding = Mathf.Sign(area);
            int count = Clip(polygonA, 4, polygonB, a, b, winding);
            count = Clip(polygonB, count, polygonA, b, c, winding);
            count = Clip(polygonA, count, polygonB, c, a, winding);
            AddPolygon(mesh, polygonB, count);
        }

        static int Clip(UIVertex[] input, int count, UIVertex[] output, Vector2 a, Vector2 b, float winding)
        {
            if (count == 0) return 0;
            int written = 0;
            UIVertex previous = input[count - 1];
            float previousDistance = Cross(b - a, (Vector2)previous.position - a) * winding;
            for (int i = 0; i < count; i++)
            {
                UIVertex current = input[i];
                float distance = Cross(b - a, (Vector2)current.position - a) * winding;
                if ((distance >= 0) != (previousDistance >= 0))
                {
                    float t = previousDistance / (previousDistance - distance);
                    UIVertex intersection = previous;
                    intersection.position = Vector3.LerpUnclamped(previous.position, current.position, t);
                    intersection.uv0 = Vector4.LerpUnclamped(previous.uv0, current.uv0, t);
                    output[written++] = intersection;
                }
                if (distance >= 0) output[written++] = current;
                previous = current;
                previousDistance = distance;
            }
            return written;
        }

        static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }

        static void AddPolygon(VertexHelper mesh, UIVertex[] vertices, int count)
        {
            if (count < 3) return;
            int start = mesh.currentVertCount;
            for (int i = 0; i < count; i++) mesh.AddVert(vertices[i]);
            for (int i = 1; i < count - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }
}
