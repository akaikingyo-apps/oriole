
module oriole.collections
{
	uses oriole.core;

	class List
	{
		field items;
		field count;

		constructor(initialSize)
		{
			if (initialSize < 0)
			{
				throw new InvalidArgumentException("initialSize must be greater than zero");
			}
			this.items = new [initialSize];
			this.count = 0;
		}

		constructor()
		{
			this.items = new [100];
			this.count = 0;
		}

		method add(item)
		{
			if (this.count + 1 > sizeof(this.items))
			{
				source = this.items;
				destination = new [this.count * 2];
				for (i = 0; i < this.count; i += 1)
				{
					destination[i] = source[i];
				}
				this.items = destination;
			}

			this.items[this.count] = item;
			this.count = this.count + 1;
			return this;
		}

		method get(index)
		{
			if (index < this.count)
			{
				return this.items[index];
			}
			else
			{
				return false;
			}
		}

		method removeAt(index)
		{
			if (index >= 0 && index < this.count)
			{
				for (i = index; i < this.count ; i++)
				{
					this.items[i] = this.items[i + 1];
				}
				this.count = this.count - 1;
				return true;
			}
			return false;
		}

		method remove(value)
		{
			for (i = 0; i < this.count ; i++)
			{
				if (this.items[i] == value)
				{
					this.removeAt(i);
					return true;
				}
			}
			return false;
		}
		
		method contains(value)
		{
			for (i = 0; i < this.count ; i++)
			{
				if (this.items[i] == value)
				{
					return true;
				}
			}
			return false;
		}

		method count()
		{
			return this.count;
		}

		method getEnumerator()
		{
			return new ListEnumerator(this);
		}

		method sort()
		{
			for (i = 0 ; i < this.count;i++)
			{
				for (j = i; j < this.count; j++)
				{
					a = this.items[i];
					b = this.items[j];
					if (a.compareTo(b) > 0)
					{
						this.items[i] = b;
						this.items[j] = a;
					}					
				}
			}
		}

		method sort(comparator)
		{
			for (i = 0 ; i < this.count;i++)
			{
				for (j = i; j < this.count; j++)
				{
					if (comparator.compare(this.items[i], this.item[j]) > 0)
					{
						tmp = this.items[i];
						this.items[i] = this.items[j];
						this.items[j] = tmp;
					}					
				}
			}
		}
	}

	class Enumerator
	{
		method moveNext()
		{
			return false;
		}

		method getElement()
		{
			return null;
		}
	}

	class ListEnumerator inherits Enumerator
	{
		field _list;
		field _index;

		constructor(list)
		{
			this._list = list;
			this._index = -1;
		}

		method moveNext()
		{
			list = this._list;
			if (this._index + 1 < list.count())
			{
				this._index = this._index + 1;
				return true;
			}
			else
			{
				return false;
			}
		}

		method getElement()
		{
			list = this._list;
			if (this._index < 0 || this._index >= list.count())
			{
				throw new Exception("Index out of range");
			}

			return list.get(this._index);
		}
	}

	class LinkedListItem
	{
		field item;
		field next;
	}

	class LinkedList
	{
		field head;
		field tail;
		field count;

		constructor()
		{
			this.head = null;
			this.tail = null;
			this.count = 0;
		}

		method add(item)
		{
			newitem = new LinkedListItem();
			newitem.item = item;
			newitem.next = null;
			
			if (this.head == null)
			{				
				this.head = newitem; 
				this.tail = newitem; 
			}
			else
			{
				t = this.tail;
				t.next = newitem;
				this.tail = newitem;
			}
			
			this.count = this.count + 1;
			return this;
		}

		method get(index)
		{
			if (index < this.count)
			{
				i = 0;
				for (currentitem = this.head ; currentitem != null ; currentitem = currentitem.next)
				{
					if (i == index)
					{
						return currentitem;
					}
					i += 1;
				}
			}
			
			return null;			
		}

		method removeAt(index)
		{
			if (index < this.count)
			{
				i = 0;
				parentitem = null;
				for (currentitem = this.head ; currentitem != null ; currentitem = currentitem.next)
				{
					if (i == index)
					{						
						if (parentitem != null)
						{
							parentitem.next = currentitem.next;
						}
						else
						{
							this.head = currentitem.next;
						}
						if (currentitem.next == null)
						{
							this.tail = parentitem;
						}
						this.count = this.count - 1;
						return true;
					}
					i ++;
					parentitem = currentitem;
				}
			}
			return false;
		}

		method remove(value)
		{	
			parentitem = null;
			for (currentitem = this.head ; currentitem != null ; currentitem = currentitem.next)
			{
				if (currentitem.item == value)
				{						
					if (parentitem != null)
					{
						parentitem.next = currentitem.next;
					}
					else
					{
						this.head = currentitem.next;
					}
					if (currentitem.next == null)
					{
						this.tail = currentitem;
					}
					this.count = this.count - 1;
					return true;
				}
				parentitem = currentitem;
			}
			
			return false;
		}

		method contains(value)
		{
			for (currentitem = this.head ; currentitem != null ; currentitem = currentitem.next)
			{
				if (currentitem.item == value)
				{
					return true;
				}
			}
			return false;
		}

		method count()
		{
			return this.count;
		}

		method getEnumerator()
		{
			return new LinkedListEnumerator(this);
		}
	}

	class LinkedListEnumerator inherits Enumerator
	{
		field _list;
		field _current;
		field _initialized;

		constructor(list)
		{
			this._list = list;
			this._initialized = false;
		}

		method moveNext()
		{
			if (this._initialized == true)
			{
				current = this._current; 
				if (current != null)
				{
					this._current = current.next;
					return this._current != null;
				}
				else
				{
					return false;
				}
			}
			else
			{
				list = this._list;
				this._current = list.head;
				this._initialized = true;
				return this._current != null;
			}
		}

		method getElement()
		{
			current = this._current;
			return current.item;
		}
	}

	class Dictionary
	{	
		field _map;
		field _mapSize;
		field _keys;

		constructor(mapSize)
		{ 
			if (mapSize < 0)
			{
				throw new InvalidArgumentException("mapSize must be greater than zero");
			}
			this._mapSize = mapSize;
			this._map = new [this._mapSize];
			this._keys = new List();
		}

		constructor()
		{ 
			this._mapSize = 100;
			this._map = new [this._mapSize];
			this._keys = new List();
		}

		method get(key)
		{
			hash = hashof(key) % this._mapSize;
			hash = hash < 0 ? -hash : hash;
			value = this._map[hash];			
			if (value != null)
			{
				for (i = 0; i < value.count() ; i++)
				{
					pair = value.get(i);
					if (pair[0] == key)
					{
						return pair[1];
					}
				}
			}
			throw new Exception("Key not found: " + key);
		}

		method set(key, value)
		{
			keys = this._keys;
			if (!keys.contains(key))
			{
				keys.add(key);
			}
			hash = hashof(key) % this._mapSize;
			hash = hash < 0 ? -hash : hash;
			list = this._map[hash];
			pair = new [2];
			pair[0] = key;
			pair[1] = value;
			if (list != null)
			{
				for (i = 0; i < list.count(); i++)
				{
					pair = list.get(i);
					if (pair[0] == key)
					{
						pair[1] = value;
						return true;
					}
				}
				list.add(pair);
			}
			else
			{
				list = new List();				
				list.add(pair);
				this._map[hash] = list;
			}
			return true;
		}

		method keys()
		{
			return new Enumerable(new ListEnumerator(this._keys));
		}
	}

	class Enumerable
	{
		field _enumerator;

		constructor(enumerator)
		{
			this._enumerator = enumerator;
		}

		method getEnumerator()
		{
			return this._enumerator;
		}
	}
}