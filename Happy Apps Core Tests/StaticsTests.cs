using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core_Tests
{
	[TestClass]
	public class StaticsTests
	{
		private int _attempts;

		[TestInitialize]
		public void TestInitialise()
		{
			_attempts = 0;
		}
		[TestMethod]
		public void RunWithRetriesPassOnFirst()
		{
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
			}, ()=> Console.WriteLine("Attempt Failed"), 5, ex=>true);
			Assert.AreEqual(1,_attempts);
		}

		[TestMethod]
		public void RunWithRetriesPassOnLast()
		{
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
				if(_attempts<5) throw new InvalidOperationException("Failed attempt for test");
			}, () => Console.WriteLine("Attempt Failed"), 5, ex => true);
			Assert.AreEqual(5, _attempts);
		}

		[TestMethod,ExpectedException(typeof(InvalidOperationException))]
		public void RunWithRetriesFailOnLast()
		{
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
				throw new InvalidOperationException("Failed attempt for test");
			}, () => Console.WriteLine("Attempt Failed"), 5, ex => true);
			Assert.Fail("Should not reach here");
		}

		[TestMethod, ExpectedException(typeof(InvalidOperationException))]
		public void RunWithRetriesOnFilterFail()
		{
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
				if(_attempts < 2) throw new IOException("IO Exception, should retry");
				else throw new InvalidOperationException("Failed attempt for test");
			}, () => Console.WriteLine("Attempt Failed"), 5, ex => ex is IOException);
			Assert.Fail("Should not reach here");
		}

		[TestMethod]
		public void RunWithRetriesOnFilterPass()
		{
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
				if (_attempts >= 2) return;
				Console.WriteLine("Can't access file, throwing");
				throw new IOException("IO Exception, should retry");
			}, () => Console.WriteLine("Attempt Failed"), 5, ex => ex is IOException);
			Assert.AreEqual(2, _attempts);
		}

		[TestMethod]
		public void RunWithRetriesOnFilterRecover()
		{
			var canAccessFile = false;
			RunWithRetries(() =>
			{
				Console.WriteLine($"Attempt {_attempts+1}");
				_attempts++;
				if (!canAccessFile)
				{
					Console.WriteLine("Can't access file, throwing");
					throw new IOException("IO Exception, should retry");
				}
				Console.WriteLine("Accessed file.");
			}, () => canAccessFile = _attempts >= 2, 5, ex => ex is IOException);
			Assert.AreEqual(3, _attempts);
		}
	}
}
