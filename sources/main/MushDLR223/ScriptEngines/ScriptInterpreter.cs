using System;
using DotLisp;

namespace MushDLR223.ScriptEngines
{
    public delegate void OutputDelegate(string f, params object[] a);

    public interface ScriptInterpreterFactory
    {
        ScriptInterpreter GetLoaderOfFiletype(string type);
    }

    public interface ScriptInterpreter : ScriptInterpreterFactory, IDisposable
    {        
        bool LoadFile(string filename, OutputDelegate WriteLine);

        bool LoadsFileType(string filenameorext);

        object Read(string context_name, System.IO.TextReader stringCodeReader, OutputDelegate WriteLine);

        bool Eof(object codeTree);

        void Intern(string varname, object value);

        object Eval(object code);

        object ConvertArgToLisp(object code);

        object ConvertArgFromLisp(object code);

        string Str(object code);

        ScriptInterpreter newInterpreter(object self);

        bool IsSubscriberOf(string eventName);

        object GetSymbol(string eventName);
       
        void InternType(Type t);

        object Self { get; set; }
        object Impl { get; }

        bool IsSelf(object self);
        void Init(object self);
    }

    public class subtask
    {
        public Boolean requeue; // should we be re-entered to the queue
        public String code;    // the lisp code as a string
        public String results; // the evaluation results as a string
        public Object codeTree; // the lisp code as an evaluatable object

    }
}