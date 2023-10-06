
module oriole.core
{
	facade class _CON ("Oriole.Library.Core.ConsoleFacadeClass", "Core.dll");

	class Console
	{
		static method print(s)
		{
			_CON::puts(s);
		}

		static method println(s)
		{
			_CON::puts(s + "\r\n");
		}

		static method keyHit()
		{
			return _CON::kbhit() != 0;
		}

		static method readKey()
		{
			while (_CON::kbhit() == 0)
			{
				Thread::nice();
			}
			return _CON::getche();
		}

		static method readLine()
		{
			s = "";
			while (true)
			{
				while (_CON::_kbhit() == 0)
				{
					Thread::nice();
				}
				ch = _CON::getche();
				if ( ch == '\r')
				{
					_CON::puts("\n");
					return s;
				}
				s += (char)ch;
			}
		}
	}

	enum KeyCode
	{
		BACKSPACE = 8,
		TAB = 9,
		ENTER = 13,
		SHIFT = 16,
		CTRL = 17,
		ALT = 18,
		PAUSE = 19,
		ESC = 27
	}

	class Object
	{
		method getHashCode()
		{
			return hashof(this);
		}

		method toString()
		{
			c = this.getClass();
			return c.getName();
		}

		method compareTo(a)
		{
			return 0;
		}

		method getClass()
		{
			return new Class(this);
		}
	}
	
	class System
	{
		static method halt()
		{
			asm
			{
				halt
			}
		}
	}

	class Module
	{
		static method use(moduleName)
		{
			asm
			{
				loadvar moduleName
				loadmodule
			}
		}
	}

	class Field
	{
		field name;

		method set(instance, value)
		{
			asm
			{
				loadvar instance
				loadvar this
				loadfield name
				loadvar value
				setfield				
			}
		}

		method get(instance)
		{
			asm
			{
				loadvar instance
				loadvar this
				loadfield name
				getfield
				retv
			}
		}
	}

	class Method
	{
		field signature;

		method invoke(instance, parameters)
		{
			asm
			{
				loadvar instance
			}

			loop (i = 0 to sizeof(parameters) exclusive)
			{
				argument = parameters[i];
				asm
				{
					loadvar argument
				}
			}

			asm
			{
				loadvar this
				loadfield signature
				invokemethod
				retv
			}
		}
	}
	
	class Class
	{
		field _instance;

		constructor(instance)
		{
			this._instance = instance;
		}

		static method get(name)
		{
			c = new Class();
			asm
			{
				loadvar c
				loadvar name
				createinstance
				storefield _instance
			}
			return c;
		}

		method new()
		{
			asm
			{
				loadvar this
				loadfield _instance
				classname
				createinstance
				loadstring "__constructor(0)"
				invokeconstructor
				retv
			}
		}

		method new(parameters)
		{
			signature = "__constructor(" + sizeof(parameters) + ")";
			asm
			{
				loadvar this
				loadfield _instance
				classname
				createinstance
			}
			loop (i = 0 to sizeof(parameters) exclusive)
			{
				parameter = parameters[i];
				asm
				{
					loadvar parameter
				}
			}
			asm
			{
				loadvar signature
				invokeconstructor
				retv
			}
		}

		method getName()
		{			
			asm
			{
				loadvar this
				loadfield _instance
				classname
				retv
			}
		}
		
		method getStaticFields()
		{
			fields = null;			

			asm
			{
				loadvar this
				loadfield _instance
				staticfields
				storevar fields
			}

			list = new [sizeof(fields)];

			loop (i = 0 to sizeof(fields) exclusive)
			{
				field = new Field();
				field.name = fields[i];
				list[i] = field;
			}

			return list;
		}

		method getFields()
		{
			fields = null;			

			asm
			{
				loadvar this
				loadfield _instance
				fields
				storevar fields
			}

			list = new [sizeof(fields)];

			loop (i = 0 to sizeof(fields) exclusive)
			{
				field = new Field();
				field.name = fields[i];
				list[i] = field;
			}

			return list;
		}

		method getStaticMethods()
		{
			methods = null;			

			asm
			{
				loadvar this
				loadfield _instance
				staticmethods
				storevar methods
			}

			list = new [sizeof(methods)];

			loop (i = 0 to sizeof(methods) exclusive)
			{
				method = new Method();
				method.signature = methods[i];
				list[i] = method;
			}

			return list;
		}

		method getMethods()
		{
			methods = null;			

			asm
			{
				loadvar this
				loadfield _instance
				methods
				storevar methods
			}

			list = new [sizeof(methods)];

			loop (i = 0 to sizeof(methods) exclusive)
			{
				method = new Method();
				method.signature = methods[i];
				list[i] = method;
			}

			return list;
		}
	}

	class Date
	{
		field _ticks;
		field _day;
		field _month;
		field _year;
		field _hour;
		field _minute;
		field _second;

		constructor()
		{
			asm
			{
				loadvar this				
				time
				storefield _ticks
			}
			
			monthDays = new [12];
			monthDays[0] = 31;
			monthDays[1] = 28;
			monthDays[2] = 31;
			monthDays[3] = 30;
			monthDays[4] = 31;
			monthDays[5] = 30;
			monthDays[6] = 31;
			monthDays[7] = 31;
			monthDays[8] = 30;
			monthDays[9] = 31;
			monthDays[10] = 30;
			monthDays[11] = 31;
			
			years = (int)(1 + this._ticks / 864000000000 / 365.242);
			days = (int)(this._ticks / 864000000000 - ((years - 1) * 365.242));

			if (years % 4 == 0 && (years % 100 != 0 || years % 400 == 0))
			{
				monthDays[1] ++;
			}

			loop (month = 0 to 12 exclusive)
			{
				if (days <= monthDays[month])
				{
					break; 
				}
				days -= monthDays[month];
			}

			this._year = years;
			this._day = days;
			this._month = month + 1;

			if (this._month > 12)
			{
				this._month = 1;
				this._year = this._year + 1;
			}

			this._second = (this._ticks / 10000000) % 60;
			this._minute = ((this._ticks / 10000000) % (60 * 60)) / 60;
			this._hour = ((this._ticks / 10000000) % (24 * 60 * 60)) / (60 * 60);
		}

		method getTicks()
		{
			return this._ticks;
		}

		method getSecond()
		{
			return this._second;
		}

		method getMinute()
		{
			return this._minute;
		}

		method getHour()
		{
			return this._hour;
		}

		method getDay()
		{
			return this._day;
		}

		method getMonth()
		{
			return this._month;
		}

		method getYear()
		{
			return this._year;
		}

		method toString()
		{
			return this.getDay() + "/" + this.getMonth() + "/" + this.getYear() + " " + this.getHour() + ":" + this.getMinute() + ":" + this.getSecond();
		}
	}

	class Exception
	{
		field _stackTrace;
		field _message;

		constructor()
		{
			this._message = "Exception";
		}

		constructor(message)
		{
			this._message = message;
		}

		method getMessage()
		{
			return this._message;
		}

		method getStackTrace()
		{
			return this._stackTrace;
		}

		method toString()
		{
			return this._message;
		}
	}

	class InvalidArgumentException inherits Exception
	{
		constructor()
		{
			this._message = "InvalidArgumentException";
		}
	}

	class Thread
	{
		field id;
		field callback;

		constructor(callback)
		{
			this.callback = callback;
		}

		method start()
		{
			if (this.callback == null)
			{
				throw new Exception("Cannot start thread.");
			}

			isForked = false;
			threadId = 0;

			asm
			{
				fork
				storevar threadId
			}

			if (threadId == 0) // child thread
			{
				callback = this.callback;
				callback.invoke(new [0]);
				asm
				{
					exit
				}
			}
			else // parent thread
			{
				this.id = threadId;
			}
		}

		method join()
		{
			n = this.id;
			asm
			{
				loadvar n
				join
			}
		}

		static method sleep(millisec)
		{
			asm
			{
				loadvar millisec
				sleep
			}
		}

		static method nice()
		{
			asm
			{
				nice
			}
		}

		static method exit()
		{
			asm
			{
				exit
			}
		}

		static method getCurrentThread()
		{
			thread = new Thread(null);
			asm
			{
				loadvar thread
				curthread
				storefield id
			}
			return thread;
		}
	}

	class Semaphore
	{
		method wait()
		{
			asm
			{
				loadvar this
				wait
			}
		}

		method signal()
		{
			asm
			{
				loadvar this
				signal
			}
		}
	}

	class Compiler
	{
		method compile(source, destination, generateAssembly)
		{			
		}
	}

	implicit class String for "String"
	{
		field _this;
		
		method length()
		{
			return sizeof(this._this);
		}

		method indexOf(c)
		{
			s = this._this;
			length = sizeof(s);
			loop (i = 0 to length exclusive)
			{
				if (s[i] == c)
				{
					return i;
				}
			}
			return -1;
		}

		method subString(start)
		{
			s = this._this;
			length = sizeof(s);
			q = "";
			loop (i = start to length exclusive)
			{
				q += s[i];
			}
			return q;
		}

		method subString(start, length)
		{
			s = this._this;
			q = "";
			loop (i = 0 to length exclusive)
			{
				q += s[start + i];
			}
			return q;
		}

		method concat(s)
		{
			return this._this + s;
		}

		method split(ch)
		{
			parts = 1;
			s = this._this;
			length = sizeof(s);
			loop (i = 0 to length exclusive)
			{
				if (s[i] == ch)
				{
					parts ++;
				}
			}
			list = new [parts];
			q = "";
			j = 0;
			loop (i = 0 to length exclusive)
			{
				if (s[i] == ch)
				{
					list[j] = q;
					q = "";
					j ++;
				}
				else
				{				
					q += s[i];
				}
			}
			list[j] = q;
			return list;
		}

		method contains(ch)
		{
			return this.indexOf(ch) != -1;
		}

		method toLower()
		{
			s = this._this;
			q = "";
			length = sizeof(s);
			loop (i = 0 to length exclusive)
			{
				ch = s[i];
				q += ch.toLower();
			}
			return q;
		}

		method toUpper()
		{
			s = this._this;
			q = "";
			length = sizeof(s);
			loop (i = 0 to length exclusive)
			{
				ch = s[i];
				q += ch.toUpper();
			}
			return q;
		}

		method compareTo(s)
		{
			q = this._this;
			mylength = sizeof(q);
			slength = sizeof(s);
			length = mylength > slength ? slength : mylength;
			loop (i = 0 to length exclusive)
			{
				a = q[i];
				b = s[i];
				if(a>b)
				{
					return 1;
				}
				else if (a<b)
				{
					return -1;
				}
			}
			return mylength > slength ? 1 : mylength < slength ? -1 : 0;
		}
	}

	class Callback
	{
		field _object;
		field _method;

		constructor(object, method)
		{
			this._object = object;
			this._method = method;
		}

		method invoke(parameters)
		{
			asm
			{
				loadvar this
				loadfield _object
			}

			loop (i = 0 to sizeof(parameters) exclusive)
			{
				parameter = parameters[i];
				asm
				{
					loadvar parameter
				}
			}

			asm
			{
				loadvar this				
				loadfield _method				
				invokemethod
				retv
			}
		}
	}

	implicit class Array for "Object[]"
	{
		field _this;

		method length()
		{
			return sizeof(this._this);
		}
	}

	implicit class Short for "Int16"
	{
		field _this;

		method toString()
		{
			return "" + this._this;	
		}
	}

	implicit class Integer for "Int32"
	{
		field _this;

		static method parse(s)
		{
			n = 0;
			m = 1;
			if (sizeof(s) ==0)
			{
				return -1;
			}
			loop (i = 0 to sizeof(s) exclusive)
			{
				c = s[i];
				if(c=='-')
				{
					if (i != 0)
					{
						return -1;
					}
					m = -1;
				}
				else if (Character::isDigit(c))
				{
					n *= 10;
					n += (int)c - (int)'0';
				}
				else
				{
					return -1;
				}
			}
			
			return n * m;
		}

		method toString()
		{
			return "" + this._this;	
		}
	}

	implicit class Long for "Int64"
	{
		field _this;

		method toString()
		{
			return "" + this._this;	
		}
	}

	implicit class Character for "Char"
	{
		field _this;

		static method isDigit(c)
		{
			return c >= '0' && c <= '9';
		}

		method toLower()
		{
			ch = this._this;
			return ch >= 'A' && ch <= 'Z' ? ch + 32 : ch;
		}

		method toUpper()
		{
			ch = this._this;
			return ch >= 'a' && ch <= 'z' ? ch - 32 : ch;
		}

		method compareTo(c)
		{
			return this._this < c ? 1 : this._this > c ? -1 : 0;
		}
	}

	implicit class Boolean for "Boolean"
	{
		field _this;
	}

	implicit class Float for "Double"
	{
		field _this;
	}
}