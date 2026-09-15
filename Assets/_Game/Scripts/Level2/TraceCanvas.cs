using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HayuNgaksara
{
    [RequireComponent(typeof(LineRenderer))]
    public class TraceCanvas : MonoBehaviour
    {
        public event System.Action<List<Vector3>> OnStrokeComplete;

        [SerializeField] private float minPointDistance = 0.1f;
        [SerializeField] private Color traceColor       = Color.blue;
        [SerializeField] private Color passColor        = Color.green;
        [SerializeField] private Color failColor        = Color.red;

        private LineRenderer       _lr;
        private List<Vector3>      _currentStroke = new List<Vector3>();
        private List<LineRenderer> _allStrokes    = new List<LineRenderer>();
        private bool               _isDrawing;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            ConfigureLineRenderer(_lr, traceColor);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
                BeginStroke();
            else if (mouse.leftButton.isPressed && _isDrawing)
                ContinueStroke();
            else if (mouse.leftButton.wasReleasedThisFrame && _isDrawing)
                EndStroke();
        }

        private void BeginStroke()
        {
            _isDrawing = true;
            _currentStroke.Clear();
            _lr.positionCount = 0;
            _lr.startColor = _lr.endColor = traceColor;
        }

        private void ContinueStroke()
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            worldPos.z = 0f;

            if (_currentStroke.Count == 0 ||
                Vector3.Distance(worldPos, _currentStroke[_currentStroke.Count - 1]) > minPointDistance)
            {
                _currentStroke.Add(worldPos);
                _lr.positionCount = _currentStroke.Count;
                _lr.SetPositions(_currentStroke.ToArray());
            }
        }

        private void EndStroke()
        {
            _isDrawing = false;
            if (_currentStroke.Count > 1)
                OnStrokeComplete?.Invoke(new List<Vector3>(_currentStroke));
        }

        public void SetTraceColor(Color c)
        {
            _lr.startColor = _lr.endColor = c;
        }

        public void ClearAll()
        {
            _lr.positionCount = 0;
            _currentStroke.Clear();
            foreach (var lr in _allStrokes)
                if (lr != null) Destroy(lr.gameObject);
            _allStrokes.Clear();
        }

        private void ConfigureLineRenderer(LineRenderer lr, Color color)
        {
            lr.startWidth  = 0.05f;
            lr.endWidth    = 0.05f;
            lr.startColor  = color;
            lr.endColor    = color;
            lr.useWorldSpace = true;
            lr.sortingOrder  = 5;
        }
    }
}
