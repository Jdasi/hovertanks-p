
public interface IClassName
{
	string Name { get; }
	string ClassName { get; }
}

public class ClassInfo : IClassName
{
	public string Name { get; }
	public string ClassName { get; }

	public ClassInfo(IClassName className)
	{
		Name = className?.Name ?? "";
		ClassName = className?.ClassName ?? "";
	}

	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(Name)
			&& !string.IsNullOrWhiteSpace(ClassName);
	}
}
