using Oriole;
using System;
using System.Runtime.InteropServices;

namespace Oriole.Library.Core
{
    public class ConsoleFacadeClass : IFacadeClass
    {
        #region interface
        public string[] GetStaticFields()
        {
            return new string[0];
        }

        public string[] GetInstanceFields()
        {
            return new string[0];
        }

        public string[] GetStaticMethodSignatures()
        {
            return new string[] { "puts(1):1", "kbhit(0):1", "getch(0):1", "getche(0):1" };
        }

        public string[] GetInstanceMethodSignatures()
        {
            return new string[0];
        }

        public object GetField(string fieldName)
        {
            return null;
        }

        public void SetField(string fieldName, object value)
        {
        }

        public object CallMethod(string methodSignature, object[] arguments)
        {
            switch (methodSignature)
            {
                case "puts(1):1":
                     Console.Write((string)arguments[0]);
                     return 0;
                case "kbhit(0):1":
                     return Console.KeyAvailable ? 1 : 0;
                case "getche(0):1":
                    return Console.ReadKey(true);
                case "getch(0):1":
                    return Console.ReadKey(false);
                default:
                    throw new Exception(string.Format("Undefined method: {0}", methodSignature));
            }
        }
        #endregion
    }
}
