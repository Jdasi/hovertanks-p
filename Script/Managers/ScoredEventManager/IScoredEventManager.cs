
public interface IScoredEventManager
{
	bool TryGetAccoladeInfo(AccoladeType type, out AccoladeInfo info);
	int GetAccoladeThreshold(AccoladeType type);
}
