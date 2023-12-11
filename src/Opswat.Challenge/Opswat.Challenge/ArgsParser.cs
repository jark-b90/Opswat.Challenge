using System;
using System.Collections.Generic;

namespace Opswat.Challenge
{
    /// <summary>
    /// Very simple console argumnt parser
    /// </summary>
    internal class ArgsParser
    {
        private Dictionary<string, string> internalDict;

        private string[] args;

        public ArgsParser(string[] args)
        {
            this.args = args;
            this.internalDict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            string currentKey = "File";
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    currentKey = arg.Substring(1);
                }
                else
                {
                    internalDict[currentKey] = arg.Trim();
                }
            }
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return this.internalDict[key];
                }
                catch
                {
                    throw new Exception($"The key [{key}] can't be found on the startup arguments, make sure it was supplied.");
                }
            }
        }

        public bool TryGetValue(string key, out string result)
        {
            return this.internalDict.TryGetValue(key, out result);
        }
    }
}
