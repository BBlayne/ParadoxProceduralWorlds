namespace UnionFind
{
	public class WeightedQuickUnion : IUnionFind
	{
		private int componentCount;
		private int[] dots;
		private int[] componentSize;

		public WeightedQuickUnion(int N)
		{
			this.componentCount = N;
			dots = new int[N];
			componentSize = new int[N];
			for (int i=0; i<N; i++)
			{
				dots[i] = i;
				componentSize[i] = 1;
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

			if (componentSize[pComponent] > componentSize[qComponent])
			{
				dots[qComponent] = pComponent;
				componentSize[pComponent] += componentSize[qComponent];
			}
			else
			{
				dots[pComponent] = qComponent;
				componentSize[qComponent] += componentSize[pComponent];
			}
			componentCount--;
		}
	}
}
