using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abot.Util.Threads
{
	/// <summary>
	/// Support create parallel task functionallity
	/// </summary>
	internal class QueueTask
	{
		public Queue<Task> Queue = new Queue<Task>();



		public void Add(Func<Task> action)
		{
			Task task = Task.Run(action);
			ToQueueTask(task);
		}

		public void Add(Action action)
		{
			Task task = Task.Run(action);
			ToQueueTask(task);
		}

		public void WaitTasksComplition()
		{
			Task.WaitAll(Queue.ToArray());
		}



		private void ToQueueTask(Task task)
		{
			Queue.Enqueue(task);
			task.ContinueWith(_ => Queue.Dequeue());
		}
	}
}
