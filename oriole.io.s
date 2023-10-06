
module oriole.io
{
	facade class _File ("Oriole.Library.Core.FileFacadeClass", "Core.dll");	

	class File
	{
		static method readText(path)
		{
			return _File::readText(path);
		}

		static method writeText(path, text)
		{
            return _File::writeText(path, text);
		}

		static method appendText(path, text)
		{
			return _File::appendText(path, text);
		}

		static method exists(path)
		{
			return _File::exists(path);
		}

		static method delete(path)
		{
			return _File::delete(path);
		}

        static method copy(source, destination)
		{
			return _File::copy(source, destination);
		}

		static method move(source, destination)
		{
			return _File::move(source, destination);
		}

		method openRead(path)
		{
            return _File.openread(path);
		}

		method openWrite(path)
		{
            return _File.openwrite(path);
		}

		method close()
		{
            return _File.close();
		}

		method readln()
		{
            return _File.readln();
		}

		method writeln(text)
		{
            return _File.writeln(text);
		}
	}
}
