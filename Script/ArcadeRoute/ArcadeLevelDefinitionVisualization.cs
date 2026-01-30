using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.ArcadeRoute
{
    public class ArcadeLevelDefinitionVisualization : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Image _imgTop;
        [SerializeField] Image _imgBottom;
        [SerializeField] Image _imgLeft;
        [SerializeField] Image _imgRight;
        [SerializeField] Text _lblText;

        private Color _colorEntrance = Color.yellow;
        private Color _colorExit = Color.red;

        public void Init(ArcadeLevelDefinition level)
        {
            _imgTop.enabled = level.ExitDir == Direction.Up;
            _imgTop.color = _colorExit;

            _imgBottom.enabled = level.EntryDir == Direction.Up;
            _imgBottom.color = _colorEntrance;

            _imgLeft.enabled = level.EntryDir == Direction.Right;
            _imgLeft.color = _colorEntrance;

            _imgRight.enabled = level.ExitDir == Direction.Right;
            _imgRight.color = _colorExit;

            _lblText.text = level.SceneName;
            gameObject.name = $"LevelVis: {level.SceneName}";
        }
    }
}
