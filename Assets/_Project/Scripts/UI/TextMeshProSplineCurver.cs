using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using TMPro;

namespace PixelMindscape.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class TextMeshProSplineCurver : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private float curveScale = 1f;
        [SerializeField] private float offset = 0f;

        private void OnEnable()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (splineContainer == null || tmpText == null || splineContainer.Splines.Count == 0) return;

            tmpText.ForceMeshUpdate(true);
            var textInfo = tmpText.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount == 0) return;

            float splineLength = splineContainer.CalculateLength();

            for (int i = 0; i < characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                var vertices = textInfo.meshInfo[materialIndex].vertices;

                // Find center of character
                Vector3 charCenter = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;

                // Map character position to spline t parameter (0 to 1)
                float t = Mathf.Clamp01(((charCenter.x * curveScale) + offset) / Mathf.Max(0.001f, splineLength));

                // Evaluate spline at t
                if (splineContainer.Evaluate(t, out float3 pos, out float3 tan, out float3 up))
                {
                    Vector3 splinePos = new Vector3(pos.x, pos.y, pos.z);
                    Vector3 splineTan = new Vector3(tan.x, tan.y, tan.z).normalized;

                    // Calculate angle for rotating character vertices
                    float angle = Mathf.Atan2(splineTan.y, splineTan.x) * Mathf.Rad2Deg;
                    Quaternion q = Quaternion.Euler(0, 0, angle);

                    // Deform each of the 4 vertices of the character quad
                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 originalVert = vertices[vertexIndex + v];
                        // Get relative position from character center
                        Vector3 relPos = originalVert - charCenter;
                        
                        // Rotate relative position to match spline tangent, then add spline position
                        vertices[vertexIndex + v] = splinePos + (q * relPos);
                    }
                }
            }

            // Push updated vertices back to the mesh
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}
