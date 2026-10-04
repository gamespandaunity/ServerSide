using UnityEngine;

namespace GreedyGame.Runtime
{
	public abstract class SingletoneBase<T> : MonoBehaviour where T : class
	{
		private static T instance = null;

		private static readonly Object syncRoot = new Object();

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					lock (syncRoot)
					{
						if (instance == null)
						{
							instance = Object.FindObjectOfType(typeof(T)) as T;
							if (instance == null)
							{
								ConstantsData_M.Log("SingletoneBase<T>: Could not found GameObject of type " + typeof(T).Name);
							}
						}
					}
				}
				return instance;
			}
		}
	}
}
