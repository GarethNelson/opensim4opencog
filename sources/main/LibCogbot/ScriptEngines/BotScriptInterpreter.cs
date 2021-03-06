using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cogbot.Actions;
using DotLisp;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;

namespace Cogbot.ScriptEngines
{
    public class BotScriptInterpreter : CommonScriptInterpreter, ScriptInterpreter
    {
        public  BotClient BotClient;

        public override ScriptInterpreter GetLoaderOfFiletype(string filenameorext)
        {
            return LoadsFileType(filenameorext) ? this : null;
        }

        public override bool LoadsFileType(string filename)
        {
            return filename.EndsWith("bot") || filename.EndsWith("txt") || filename.EndsWith("note") ||
                   base.LoadsFileType(filename);
        }

        public override void InternType(Type t)
        {
            if (BotClient == null)
            {
                if (OriginalSelf != null) return;
                ScriptManager.WriteLine(this + "cannot intern type " + t);
                return;
            }
            BotClient.InternType(t);
        }

        public override void Dispose()
        {
            BotClient.Dispose();
        }

        public override object GetSymbol(string eventName)
        {
            eventName = eventName.ToLower();
            CommandInstance o;
            BotClient.Commands.TryGetValue(eventName,out o);
            return o;
        }

        public override bool IsSubscriberOf(string eventName)
        {
            return GetSymbol(eventName) != null;
        }

        public BotScriptInterpreter(object bc)
            : base()
        {
           // if (bc is ClientManager) bc = ((ClientManager)bc).LastBotClient ?? bc;
            BotClient = (BotClient) bc;
            Self = bc;
        }

        public override object Self
        {
            get { return BotClient; }
            set { if (value is BotClient) BotClient = value as BotClient; }
        }

        override public object Impl
        {
            get { return this; }
        }

        public override void Init(object self)
        {
            Self = self;
        }        
        
        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override bool LoadFile(string filename, OutputDelegate WriteLine)
        {
            if (!File.Exists(filename)) return false;
            System.IO.FileStream f = System.IO.File.OpenRead(filename);
            StreamReader r = new StreamReader(f);
            r.BaseStream.Seek(0, SeekOrigin.Begin);
            return Read(filename, new StringReader(r.ReadToEnd()),WriteLine) != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context_name"></param>
        /// <param name="stringCodeReader"></param>
        /// <returns></returns>
        public override object Read(string context_name, System.IO.TextReader stringCodeReader, OutputDelegate WriteLine)
        {
            CmdResult res = null;
            int line = 0;
            while (stringCodeReader.Peek() != -1)
            {
                line++;
                res = BotClient.ExecuteCommand(stringCodeReader.ReadLine(), context_name, WriteLine, CMDFLAGS.Foregrounded);
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
            return String.IsNullOrEmpty(str);
        } // method: Eof


        /// <summary>
        /// 
        /// </summary>
        /// <param name="varname"></param>
        /// <param name="textForm"></param>
        public override void Intern(string varname, object value)
        {
            BotClient.Intern(varname, value);
        } // method: Intern


        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override object Eval(object code)
        {
            return BotClient.ExecuteCommand(code.ToString(), CMDFLAGS.Foregrounded);
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

            if (thiz is ClientManager) thiz = ((ClientManager)thiz).LastBotClient ?? thiz;

            BotClient bc = thiz as BotClient;
            if (bc == null) bc = BotClient;

            BotScriptInterpreter si;
            if (BotClient == null || BotClient == bc) si = this;
            else
                si = new BotScriptInterpreter(thiz);
            si.BotClient = thiz as BotClient;
            return si;
        } // method: newInterpreter

    }
}
