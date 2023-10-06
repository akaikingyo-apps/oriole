using Oriole;
using System;
using System.Net;

namespace Oriole.Library.Core
{
    public class WebClientFacadeClass: IFacadeClass
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
            return new string[] { "downloadString(1):1" };
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
            if (methodSignature == "downloadString(1):1")
            {
                return this.DownloadString((string)arguments[0]);
            }
            throw new Exception(string.Format("Undefined method: {0}", methodSignature));
        }

        #endregion

        #region implementations

        private string DownloadString(string url)
        {
            return new WebClient().DownloadString(url);
        }

        #endregion
    }
}