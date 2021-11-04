namespace UnionFind
{
	public interface IUnionFind
	{
		// return the number of disjoint components
		int Count();

		// return true if two objects are connected, or return false if not connected
		bool Connected(int p, int q);

		// return the id of disjoint component that an object belongs to
		int Find(int p);

		// connect two objects if not connected yet
		void Union(int p, int q);
	}
}
