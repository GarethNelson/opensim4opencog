using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using cogbot;
using cogbot.ScriptEngines;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using SbsSW.SwiPlCs;
using Swicli.Library;

namespace PrologScriptEngine
{
    public class PrologScriptInterpreter : CommonScriptInterpreter, ScriptInterpreter
    {
        public static bool AutoInternMethods = false;
        static public void Main(string[] args)
        {
            _reachAllTypes = 2;
            PrologScriptInterpreter pscript = new PrologScriptInterpreter(null);
            pscript.Init(pscript);
            pscript.Intern("pscript", pscript);
            PrologClient.Main(args);
        }
        ///<summary>
        ///</summary>
        public PrologClient prologClient;

        public override bool LoadsFileType(string filename)
        {
            return filename.EndsWith("pl") || filename.EndsWith("swi") || filename.EndsWith("prolog") ||
                   base.LoadsFileType0(filename);
        }
        static HashSet<Type> _types = new HashSet<Type>();
        private static int _reachAllTypes = 1;

        ///<summary>
        ///</summary>
        ///<param name="type"></param>
        ///<exception cref="NotImplementedException"></exception>
        public override void InternType(Type type)
        {
            InternTypeS(type, _reachAllTypes);
        }

        public object Impl
        {
            get { return this; }
        }

        static void InternTypeS(Type type, int depth)
        {
            if (depth == 0) return;
            depth--;
            bool deeper = depth != 0;
            {
                lock (_types) if (!_types.Add(type)) return;                          
                string module = type.Namespace;
                foreach (var list0 in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var list = list0;
                    DLRConsole.SafelyRun(() =>
                    {
                        if (AutoInternMethods) PrologClient.InternMethod(module, PrologClient.ToPrologCase(list.Name), list);
                        if (deeper)
                        {
                            foreach (var s in list.GetParameters())
                            {
                                InternTypeS(s.ParameterType, depth);
                            }
                            InternTypeS(list.ReturnType, depth);
                        }
                    });
                }
            }
        }

        public override void Dispose()
        {
            prologClient.Dispose();
        }

        public override object GetSymbol(string eventName)
        {
            return prologClient.GetSymbol(eventName);
        }

        public override bool IsSubscriberOf(string eventName)
        {
            return prologClient.IsDefined(eventName);
        }

        public PrologScriptInterpreter(object self)
            : base()
        {
            Init(self);
            PrologClient.PlNamed("MyBot");
        }

        static private readonly object GroupInitLock = new object();
        public static bool IsInited = false;

        public override sealed void Init(object self)
        {
            lock (GroupInitLock)
            {
                prologClient = prologClient ?? new PrologClient();
                if (!IsInited)
                {
                    IsInited = true;
                    prologClient.InitFromUser();
                }
            }
        }
        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override bool LoadFile(string filename, OutputDelegate WriteLine)
        {
            try
            {
                return prologClient.Consult(filename);
            }
            catch (Exception e)
            {
                WriteLine("CSERROR: " + e);
                return false;
            }
            if (!File.Exists(filename)) return false;
            System.IO.FileStream f = System.IO.File.OpenRead(filename);
            StreamReader r = new StreamReader(f);
            r.BaseStream.Seek(0, SeekOrigin.Begin);
            return Read(filename, new StringReader(r.ReadToEnd()), WriteLine) != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context_name"></param>
        /// <param name="stringCodeReader"></param>
        /// <returns></returns>
        public override object Read(string context_name, System.IO.TextReader stringCodeReader, OutputDelegate WriteLine)
        {
            object res = null;
            int line = 0;
            while (stringCodeReader.Peek() != -1)
            {
                line++;
                TextWriter tw = new OutputDelegateWriter(WriteLine);
                res = prologClient.Read(stringCodeReader.ReadLine(), tw);
            }
            return res;
        } // method: Read


        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeTree"></param>
        /// <returns></returns>
        public override bool Eof(object codeTree)
        {
            if (codeTree == null) return true;
            String str = codeTree.ToString().Trim();
            return String.IsNullOrEmpty((String)codeTree);
        } // method: Eof


        /// <summary>
        /// 
        /// </summary>
        /// <param name="varname"></param>
        /// <param name="textForm"></param>
        public override void Intern(string varname, object value)
        {
            if (value != null)
            {
                InternTypeS(value.GetType(),1);
            }
            prologClient.Intern(varname, value);
        } // method: Intern


        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override object Eval(object code)
        {
            return prologClient.Eval(code);
        } // method: Eval


        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override string Str(object code)
        {
            return ScriptEventListener.argString(code);
        } // method: Str


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override ScriptInterpreter newInterpreter(object thiz)
        {
            PrologScriptInterpreter si;
            if (prologClient == null || prologClient == thiz) si = this;
            else
                si = new PrologScriptInterpreter(thiz);
            //si.prologClient = thiz as PrologClient;
            return si;
        } // method: newInterpreter
    }
}