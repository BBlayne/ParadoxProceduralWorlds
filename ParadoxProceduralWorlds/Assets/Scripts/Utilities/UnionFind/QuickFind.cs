namespace UnionFind
{
	public class QuickFind : IUnionFind
	{
		private int componentCount;
		private int[] dots;

		public QuickFind(int N)
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
			return dots[p];
		}

		public void Union(int p, int q)
		{
			int pComponent = Find(p);
			int qComponent = Find(q);
			if (pComponent == qComponent) return;
			for (int i=0; i<dots.Length; i++)
			{
				if (dots[i] == qComponent) dots[i] = pComponent;
			}
			componentCount--;
		}
	}
}
