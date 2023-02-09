using System.Collections;

namespace Benkyou.Infrastructure
{
    public class QueryPage<T> : IReadOnlyList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }
        public int Skip { get; }
        public int Take { get; }
        public int Page => Take == 0 ? 1 : Skip / Take + 1; // TODO: check if used
        public int Pages => Take == 0 ? 1 : TotalCount / Take; // TODO: check if used

        public QueryPage(IReadOnlyList<T> items, int totalCount, int skip, int take)
        {
            Items = items;
            TotalCount = totalCount;
            Skip = skip > 0 ? skip : 0;
            Take = take > 0 ? take : totalCount;
        }

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => Items.Count;

        public T this[int index] => Items[index];
    }
}
