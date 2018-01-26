using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abot.Utils.Threads
{
	/// <summary>
	/// Support create parallel task functionallity
	/// </summary>
	public class QueueTask : IQueueTask
	{
		#region Properties

		/// <summary>
		/// Queue of tasks
		/// </summary>
		public Queue<Task> Queue { get; } = new Queue<Task>();

		#endregion

		#region Public Methods

		/// <summary>
		/// Add task to queue
		/// </summary>
		/// <param name="task"></param>
		public void Add(Task task)
		{
			ToQueueTask(task);
		}

		/// <summary>
		/// Add action for creating task, which will add to queue and
		/// when finish it work will automatically dequeue
		/// </summary>
		/// <param name="func"></param>
		public void Add(Func<Task> func)
		{
			Task task = Task.Run(func);
			ToQueueTask(task);
		}

		/// <summary>
		/// Add action for creating task, which will add to queue and
		/// when finish it work will automatically dequeue
		/// </summary>
		/// <param name="action"></param>
		public void Add(Action action)
		{
			Task task = Task.Run(action);
			ToQueueTask(task);
		}

		/// <summary>
		/// Wait all tasks from queue
		/// </summary>
		public void WaitTasksComplition()
		{
			Task.WaitAll(Queue.ToArray());
		}

		#endregion

		#region Private Methods

		private void ToQueueTask(Task task)
		{
			Queue.Enqueue(task);
			task.ContinueWith(_ => Queue.Dequeue());
		}

		#endregion
	}
}
