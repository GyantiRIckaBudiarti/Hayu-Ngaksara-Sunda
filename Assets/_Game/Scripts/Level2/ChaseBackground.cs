using UnityEngine;

namespace HayuNgaksara
{
    public class ChaseBackground : MonoBehaviour
    {
        [SerializeField] private Transform[] layers;
        [SerializeField] private float[]     parallaxFactors;
        [SerializeField] private float       scrollSpeed = 3f;
        [SerializeField] private float       resetX      = -20f;
        [SerializeField] private float       startX      = 20f;

        public float ScrollSpeed
        {
            get => scrollSpeed;
            set => scrollSpeed = value;
        }

        private void Update()
        {
            for (int i = 0; i < layers.Length && i < parallaxFactors.Length; i++)
            {
                layers[i].position += Vector3.left * scrollSpeed * parallaxFactors[i] * Time.deltaTime;
                if (layers[i].position.x < resetX)
                {
                    var pos = layers[i].position;
                    pos.x = startX;
                    layers[i].position = pos;
                }
            }
        }
    }
}
