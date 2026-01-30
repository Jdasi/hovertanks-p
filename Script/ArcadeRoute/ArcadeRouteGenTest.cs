using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.ArcadeRoute
{
    public class ArcadeRouteGenTest : MonoBehaviour
    {
        [SerializeField] bool _generate;
        [SerializeField] int _iterations;
        [SerializeField] ArcadeLevelDefinitionVisualization _visualizationPrefab;

        [Header("Test")]
        [SerializeField] ArcadeArcDefinition _arc;

        private ArcadeLevelDefinitionVisualization[] _visualization;

        private void Update()
        {
            if (!_generate)
            {
                return;
            }

            _generate = false;

            CleanupVisualization();
            VisualizeRoute(_arc.Levels);
        }

        private void CleanupVisualization()
        {
            if (_visualization == null)
            {
                return;
            }

            foreach (var level in _visualization)
            {
                Destroy(level.gameObject);
            }

            _visualization = null;
        }

        private void VisualizeRoute(ArcadeLevelDefinition[] route)
        {
            var clones = new Stack<ArcadeLevelDefinitionVisualization>(route.Length);
            var offset = Vector3.zero;

            foreach (var item in route)
            {
                var clone = Instantiate(_visualizationPrefab, offset, Quaternion.identity);
                clone.transform.SetParent(this.transform);
                clone.Init(item);

                switch (item.ExitDir)
                {
                    case Direction.Up: offset += new Vector3(0, 0, 1); break;
                    case Direction.Right: offset += new Vector3(1, 0, 0); break;
                }

                clones.Push(clone);
            }

            _visualization = clones.ToArray();
        }
    }
}
