using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abot.Util.Threads
{
	/// <summary>
	/// Support create parallel task functionallity
	/// </summary>
	public interface IQueueTask
	{
		/// <summary>
		/// Queue of tasks
		/// </summary>
		Queue<Task> Queue { get; }

		/// <summary>
		/// Add task to queue
		/// </summary>
		/// <param name="task"></param>
		void Add(Task task);

		/// <summary>
		/// Add action for creating task, which will add to queue and
		/// when finish it work will automatically dequeue
		/// </summary>
		/// <param name="func"></param>
		void Add(Func<Task> func);

		/// <summary>
		/// Add action for creating task, which will add to queue and
		/// when finish it work will automatically dequeue
		/// </summary>
		/// <param name="action"></param>
		void Add(Action action);

		/// <summary>
		/// Wait all tasks from queue
		/// </summary>
		void WaitTasksComplition();
	}
}