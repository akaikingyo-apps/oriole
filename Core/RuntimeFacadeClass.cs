using Oriole;
using System;

namespace Oriole.Library.Core
{
    class RuntimeFacadeClass: IFacadeClass
    {
        #region interface

        public string[] GetStaticFields()
        {
            return new string[0];
        }

        public string[] GetInstanceFields()
        {
            return new string[] { "eval(1):1" };
        }

        public string[] GetStaticMethodSignatures()
        {
            return new string[0];
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
            if (methodSignature == "eval(1):1")
            {
                return this.Eval((string)arguments[0]);
            }
            throw new Exception(string.Format("Undefined method: {0}", methodSignature));
        }

        #endregion

        #region implementations

        private object Eval(string statements)
        {
            Compiler compiler = new Compiler();
            compiler.Compile(statements);
            
            return null;
        }

        #endregion
    }
}
