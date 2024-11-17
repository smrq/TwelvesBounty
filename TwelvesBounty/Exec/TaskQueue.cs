using System;
using System.Collections;
using System.Collections.Generic;

namespace TwelvesBounty.Exec {
	public class TaskQueue {
		private readonly Queue<IEnumerable> queue = [];
		private IEnumerator? current = null;

		public bool HasTask {
			get { return current != null || queue.Count > 0; }
		}

		public void Add(IEnumerable task) {
			queue.Enqueue(task);
		}

		public void Clear() {
			queue.Clear();
			current = null;
		}

		public void Execute() {
			if (!HasTask) return;
			current ??= queue.Dequeue().GetEnumerator();
			if (!current.MoveNext()) {
				current = null;
			}
		}

		public static IEnumerable Idle(int ms) {
			var t = DateTime.Now.AddMilliseconds(ms);
			while (DateTime.Now < t) {
				yield return null;
			}
		}
	}
}
