using Oriole;
using System;

namespace Oriole.Library.Core
{
    public class NetFacadeClass: IFacadeClass
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
            return new string[0];
        }

        public string[] GetInstanceMethodSignatures()
        {
            return new string[] { "listen(1):1" };
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
            if (methodSignature == "listen(1):1")
            {
                return this.Listen((string)arguments[0]);
            }
            throw new Exception(string.Format("Undefined method: {0}", methodSignature));
        }

        #endregion

        #region implementations

        private object Listen(string ipAddress)
        {
            return null;
        }

        #endregion
    }
}
