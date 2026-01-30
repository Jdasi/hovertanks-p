
namespace HoverTanks.Entities
{
    public class HistoryBuffer<T>
    {
        private readonly T[] _states;

        public int Size => _states.Length;

        /// <summary>
        /// The last index that was written to.
        /// </summary>
        public int Head { get; private set; } = -1;

        public HistoryBuffer(int size)
        {
            _states = new T[size];
        }

        public bool TryGetState(int index, out T state)
        {
            state = default;

            if (index < 0 || index >= _states.Length)
            {
                return false;
            }

            state = _states[index];

            return true;
        }

        public void Record(T state)
        {
            Head = (Head + 1) % _states.Length;
            _states[Head] = state;
        }
    }
}
