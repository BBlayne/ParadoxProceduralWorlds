namespace UnionFind
{
	public class QuickUnion : IUnionFind
	{
		private int componentCount;
		private int[] dots;

		public QuickUnion(int N)
		{
			this.componentCount = N;
			dots = new int[N];
			for (int i=0; i<N; i++)
			{
				dots[i] = i;
			}
		}
		public bool Connected(int p, int q)
		{
			return Find(p) == Find(q);
		}

		public int Count()
		{
			return componentCount;
		}

		public int Find(int p)
		{
			while (p != dots[p])
			{
				p = dots[p];
			}
			return p;
		}

		public void Union(int p, int q)
		{
			int pComponent = Find(p);
			int qComponent = Find(q);
			if (pComponent == qComponent) return;
			dots[pComponent] = qComponent;
			componentCount--;
		}
	}
}
