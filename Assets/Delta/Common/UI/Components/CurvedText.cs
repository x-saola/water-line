using TMPro;
using UnityEngine;

namespace Delta.Common
{
    [ExecuteInEditMode]
    public class CurvedText : MonoBehaviour
    {
        [SerializeField] TMP_Text _textComponent;

        private Mesh _mesh;
        private Vector3[] _vertices;

        private void Update()
        {
            _textComponent.ForceMeshUpdate();
            _mesh = _textComponent.mesh;
            _vertices = _mesh.vertices;

            for (int i = 0; i < _vertices.Length; i++)
            {
                var vertex = _vertices[i];

                float percentCircumference = vertex.x / circumference;
                Vector3 offset = Quaternion.Euler(0, 0, -percentCircumference * 360f) * Vector3.up;
                vertex = offset * radius * scaleFactor + offset * vertex.y;
                vertex += Vector3.down * radius * scaleFactor;

                _vertices[i] = vertex;
            }

            _mesh.vertices = _vertices;
            _textComponent.canvasRenderer.SetMesh(_mesh);
        }

        public float radius = 0.5f;
        public float wrapAngle = 360.0f;
        public float scaleFactor = 100.0f;


        private float circumference
        {
            get
            {
                if (_radius != radius || _scaleFactor != scaleFactor)
                {
                    _circumference = 2.0f * Mathf.PI * radius * scaleFactor;
                    _radius = radius;
                    _scaleFactor = scaleFactor;
                }

                return _circumference;
            }
        }

        private float _radius = -1;
        private float _scaleFactor = -1;
        private float _circumference = -1;

        private void Curve()
        {
            _textComponent.ForceMeshUpdate();
            var textInfo = _textComponent.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                {
                    continue;
                }

                var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

                for (int j = 0; j < 4; j++)
                {
                    // TODO: rotate text
                }
            }
        }

        // protected override void OnPopulateMesh(VertexHelper vh)
        // {
        //     base.OnPopulateMesh(vh);

        //     List<UIVertex> stream = new List<UIVertex>();
        //     vh.GetUIVertexStream(stream);
        //     for (int i = 0; i < stream.Count; i++)
        //     {
        //         UIVertex vertex = stream[i];

        //         float percentCircumference = vertex.position.x / Circumference;
        //         Vector3 offset = Quaternion.Euler(0, 0, -percentCircumference * 360f) * Vector3.up;
        //         vertex.position = offset * radius * scaleFactor + offset * vertex.position.y;
        //         vertex.position += Vector3.down * radius * scaleFactor;

        //         stream[i] = vertex;
        //     }

        //     vh.AddUIVertexTriangleStream(stream);
        // }
    }
}
