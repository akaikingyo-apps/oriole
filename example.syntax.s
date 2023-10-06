
/* this is a multiple 
   line comment */

// this is a single line comment

module my.syntax.module // full module/class name support
{
	facade class _CON ('Core.ConsoleFacadeClass', 'Core.dll'); // external implementation support

	implicit class String for "String" // implicit class for build in types
	{
		field _this;

		method concat(s)
		{
			this._this += s;
		}
	}
	
	class Program inherits Object // inheritance
	{
		uses oriole.core; // uses external module

		field instanceField;
		static field classField;

		static // static constructor
		{
		}

		constructor() // instance constructor
		{
		}

		constructor(x): super(x) // instance constructor with super class constructor invocation
		{
		}

		static method main(args) // startup point
		{
			program = new Program();
			program.doMethodInvocationAndFieldAccess();
			program.doLooping();
			program.doBranching();
			program.doExceptionHandling();
		}

		method doMethodInvocationAndFieldAccess()
		{
			Console::println(""); // static method invocation

			program = new Program();
			program.doMethodInvocationAndFieldAccess(); // instance method invocation

			field = program.instanceField; // instance field access
			field = Program::classField; // static field access
		}

		method doLooping()
		{
			// while loop
			i = 0;			
			while (i < 10)
			{
				i ++;
			}

			// do-while loop
			i = 0;
			do
			{
				i++;
			}
			while (i < 10);

			// for loop
			for (i = 0 ;i < 10 ; i ++)
			{
			}

			// for-each loop
			list = new List();
			list.add(i);
			foreach (item in list)
			{
				break;
			}

			// increment/decrement loop
			loop (i = 0 to 10 exclusive)
			{
				continue;
			}
			loop (i = 10 down to 0)
			{
				break;
			}
		}

		method doBranching()
		{
			// if-else
			if (true)
			{
			}
			else
			{
			}

			// switch-case
			n = 1;
			switch(n)
			{
				case 1:
				case 2:
					break;
			}
		}

		method doExceptionHandling()
		{
			try
			{
				throw new Exception("error message");
			}
			catch (e)
			{
				Console::println(e.getMessage() + " at " + e.getStackTrace());
				throw;
			}
			finally
			{
			}
		}

		synchronized method doSynchronization()
		{
			synchronized (this)
			{
			}		
		}

		method doMultithreading()
		{
			Thread thread = new Thread(new Callback(this, "doSomething(0):1"));
			thread.start();
			thread.join();
		}

		method doAssembly()
		{
			asm
			{
				nop
			}
		}

		method doReflection()
		{
			c = this.getClass();
			c.getMethods();
			c.getName();
		}
	}
}
