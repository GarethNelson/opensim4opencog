﻿#define MERGED_RDFSTORE
using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using Mono.CSharp;
using MushDLR223.Utilities;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;
using VDS.RDF.Writing;
using VDS.RDF.Nodes;
using StringWriter = System.IO.StringWriter;
//using TermList = LogicalParticleFilter1.TermListImpl;
//using TermList = System.Collections.Generic.List<LogicalParticleFilter1.SIProlog.Part>;///LogicalParticleFilter1.SIProlog.PartListImpl;
//using PartList = System.Collections.Generic.List<LogicalParticleFilter1.SIProlog.Part>;///LogicalParticleFilter1.SIProlog.PartListImpl;

using TermList = LogicalParticleFilter1.SIProlog.PartListImpl;
using PartList = LogicalParticleFilter1.SIProlog.PartListImpl;
#if MERGED_RDFSTORE
using GraphWithDef = LogicalParticleFilter1.SIProlog.PNode;
#endif

using System.Threading;
//using GraphWithDef = LogicalParticleFilter1.SIProlog.;
//using ProveResult = LogicalParticleFilter1.SIProlog.PEnv;
namespace LogicalParticleFilter1
{
    public partial class SIProlog
    {

        public static void Warn(string format, params object[] args)
        {
            if (DLRConsole.IsOnMonoUnix)
            {
                // KHC : temp mono linux patch
                Console.WriteLine(format, args);
                return;
            }

            DLRConsole.DebugLevel = 6;
            string write = DLRConsole.SafeFormat(format, args);
            TextWriter WarnWriter = WebLinksWriter.WarnWriter;
            if (WarnWriter != null)
            {
                try
                {
                    WarnWriter.WriteLine("<hr/><pre><font color=\"red\">{0}</font></pre><hr/>", write);
                    WarnWriter.Flush();
                    return;
                }
                catch (Exception)
                {
                }
            }
            DLRConsole.DebugWriteLine("{0}", write);
        }
        public static void Warn(object arg0)
        {
            Warn("{0}", arg0);
        }
        public static void Warn(string arg0)
        {
            Warn("{0}", arg0);
        }
        public static void ConsoleWriteLine(string format, params object[] args)
        {
            if (DLRConsole.DebugLevel < 1) DLRConsole.DebugLevel = 6;
            string write = DLRConsole.SafeFormat(format, args);
            DLRConsole.DebugWriteLine("{0}", write);
        }

        public void webWriter(TextWriter writer, string action, string query, string mt, string serverRoot)
        {
            writer = WebLinksWriter.AddWarnWriter(writer);
            serverRoot = "/";
            if ((action == "autorefresh") || (action == "autoquery"))
            {
                writer.WriteLine("<META HTTP-EQUIV=\"REFRESH\" content=\"10\">");
            }
            writer.WriteLine("<html>");
            var s = @"<head>
<script>
function showtip(current,e,text)
{
   if (document.all)
   {
      thetitle=text.split('<br>')
      if (thetitle.length > 1)
      {
        thetitles=""""
        for (i=0; i<thetitle.length-1; i++)
           thetitles += thetitle[i] + ""\r\n""
        current.title = thetitles
      }
      else current.title = text
   }

   else if (document.layers)
   {
       document.tooltip.document.write(
           '<layer bgColor=""#FFFFE7"" style=""border:1px ' +
           'solid black; font-size:12px;color:#000000;"">' + text + '</layer>')
       document.tooltip.document.close()
       document.tooltip.left=e.pageX+5
       document.tooltip.top=e.pageY+5
       document.tooltip.visibility=""show""
   }
}

function hidetip()
{
    if (document.layers)
        document.tooltip.visibility=""hidden""
}

function setIframeSource() {
	var theSelect = document.getElementById('location');
	var theIframe = document.getElementById('myIframe');
	var theUrl;
	
	theUrl = theSelect.options[theSelect.selectedIndex].value;
	theIframe.src = theUrl;
}
</script>
</head>
";
            writer.WriteLine(s);
            TOCmenu(writer, serverRoot);
            webWriter0(writer, action, query, mt, serverRoot, true);
            writer.WriteLine("</html>");
            WebLinksWriter.RemoveWarnWriter(writer);
        }

        private void TOCmenu(TextWriter writer, string serverRoot)
        {
            writer.WriteLine("<a href='{0}siprolog/?q=list'>List Mts</a> ", serverRoot);
            writer.WriteLine("<a href='{0}siprolog/?q=selector'>Browse Mts</a> ", serverRoot);
            writer.WriteLine("<a href='{0}siprolog/?q=preds'>List Preds</a> ", serverRoot);
            writer.WriteLine("<a href='{0}siprolog/?q=listing'>List All KB Rules</a> ", serverRoot);
            writer.WriteLine("<a href='{0}query'>Sparql Query</a>", PFEndpoint.serverRoot);
            writer.WriteLine("<a href='{0}list/'>Behavour List</a>", serverRoot);
            //writer.WriteLine("<a href='{0}processes/list/'>Processes List</a>", serverRoot);
            writer.WriteLine("<a href='{0}scheduler/?a=liststatus'>Scheduler List</a>", serverRoot);
            writer.WriteLine("<a href='{0}analysisllist/'>Analysis List</a>", serverRoot);
            writer.WriteLine("<a href='{0}graphmaster/?a=list'>Graphmaster List</a>", serverRoot);
            writer.WriteLine("<a href='{0}'>Home</a><br/>", serverRoot);
        }
        public void webWriter0(TextWriter writer, string action, string queryv, string mt, string serverRoot, bool toplevel)
        {
            try
            {
                if ((action == null) && (queryv == null) && (mt == null))
                {
                    queryv = "list";
                }


                if (action == null)
                {
                    if (queryv != null)
                    {
                        if (queryv.ToLower() == "list")
                        {
                            writer.WriteLine("<h2>Siprolog Mt List</h2>");
                            foreach (PNode p in KBGraph.SortedTopLevelNodes)
                            {
                                writer.WriteLine(p.ToLink(serverRoot) + "<br/>");
                            }
                            writer.WriteLine("<h2>Siprolog Mt Treed</h2>");
                            KBGraph.PrintToWriterTreeMts(writer, serverRoot);
                            return;
                        }
                        if (queryv.ToLower() == "selector")
                        {
                            writer.WriteLine("<table width='100%' style='height: 100%;' cellpadding='10' cellspacing='0' border='0'>");
                            //header
                            writer.WriteLine ("<tr>");
                            writer.WriteLine ("<td colspan='2' style='height: 100px;' bgcolor='#777d6a'>");
                            writer.WriteLine("<h2>Siprolog Mt Selector view</h2>");
                            writer.WriteLine("</td/tr>");
                            //left column
                            writer.WriteLine("<tr>");
                            writer.WriteLine("<td width='20%' valign='top' bgcolor='#999f8e'>");
                            writer.WriteLine("<form id=\"form1\" name=\"form1\" method=\"post\" action=\"\"> <label> Select a Mt:");

                            writer.WriteLine("<select name=\"location\" id=\"location\" onchange=\"setIframeSource()\"  size='32'>");
                            foreach (PNode p in KBGraph.SortedTopLevelNodes)
                            {
                                writer.WriteLine(p.ToOptionLink(serverRoot) );
                            }
                            writer.WriteLine("</select>");
                            writer.WriteLine("</label></form>");
                            writer.WriteLine("</td>");
                            writer.WriteLine("<td width='80%' valign='top' bgcolor='#d2d8c7'>");

                            writer.WriteLine("<p>&nbsp;</p>");
                            writer.WriteLine("<iframe id='myIframe' src='"+serverRoot+"siprolog/?mt=baseKB' width='100%' height='100%' frameborder='0' marginheight='0' marginwidth='0'></iframe>");
                            writer.WriteLine("<p>&nbsp;</p>");
                            writer.WriteLine("</td></tr></table>");

                            //writer.WriteLine("<h2>Siprolog Mt Treed</h2>");
                            //KBGraph.PrintToWriterTreeMts(writer, serverRoot);
                            return;
                        }


                        if (queryv.ToLower() == "preds")
                        {
                            writer.WriteLine("<h2>Siprolog Preds List</h2>");
                            SharedGlobalPredDefsDirty = true;
                            UpdateSharedGlobalPredDefs();
                            lock (SharedGlobalPredDefs)
                            {
                                foreach (var kpp in SharedGlobalPredDefs)
                                {
                                    kpp.Value.WriteHtmlInfo(writer);
                                }
                            }
                            UpdateSharedGlobalPredDefs();
                            WriteMtInfo(writer, rdfDefMT, serverRoot, false);
                            return;
                        }
                        if (queryv.ToLower() == "listing")
                        {
                            List<string> allMts = new List<string>();
                            //writer.WriteLine("<h2>Siprolog Mt List</h2>");
                            foreach (PNode p in KBGraph.SortedTopLevelNodes)
                            {
                                string pname = p.id;
                                allMts.Add(pname);
                                //writer.WriteLine("<a href='{1}siprolog/?mt={0}'>{0}  (prob={2})</a><br/>", pname, serverRoot, p.probability);
                            }
                            writer.WriteLine("<h2>Siprolog Mt Treed</h2>");
                            KBGraph.PrintToWriterTreeMts(writer, serverRoot);
                            foreach (var list in allMts)
                            {
                                writer.WriteLine("<hr/>");
                                webWriter0(writer, action, null, list, null, false);
                            }
                            interactFooter(writer, "", serverRoot);
                            return;
                        }
                    }
                    if (mt != null)
                    {
                        WriteMtInfo(writer, mt, serverRoot, toplevel);
                        if (toplevel) interactFooter(writer, mt, serverRoot);
                        return;
                    }
                }
                else
                {
                    switch (action.ToLower())
                    {
                        case "append":
                            appendKB(queryv, mt);
                            break;
                        case "insert":
                            insertKB(queryv, mt);
                            break;
                        case "clear":
                            clearKB(mt);
                            break;
                        case "query":
                            interactQuery(writer, queryv, mt, serverRoot);
                            break;
                        case "autoquery":
                            interactQuery(writer, queryv, mt, serverRoot);
                            return;
                            break;
                    }
                    webWriter0(writer, null, null, mt, serverRoot, false);
                }
            }
            catch (Exception e)
            {
                writer.WriteLine("<font color='red'>{0} {1} {2}</font>", e.GetType(), e.Message, e.StackTrace);
                return;
            }


        }


        public void WriteMtInfo(TextWriter writer, string mt, string serverRoot, bool toplevel)
        {
            threadLocal.tl_ServerRoot = serverRoot;
            threadLocal.tl_writer = writer;
            writer.WriteLine("<h2>Siprolog Mt {0}</h2>", mt);
            PNode qnode = FindKB(mt);
            if (qnode != null)
            {
                writer.WriteLine("<h3> OutgoingEdges </h3>");
                KBGraph.PrintToWriterOutEdges(qnode, 0, writer, serverRoot);
                writer.WriteLine("<h3> IncomingEdges </h3>");
                KBGraph.PrintToWriterInEdges(qnode, 0, writer, serverRoot);
                mt = qnode.Id;
            }
            threadLocal.tl_mt = mt;
            //KBGraph.ShowGenlMts(qnode, null, 0, writer, serverRoot);
            writer.WriteLine("<h3> KB Operations for {0}</h3>&nbsp;", mt);
            writer.WriteLine("<a href='{0}plot/?mt={1}&q=plot(X,Y)'>Plot Mt</a>&nbsp;", serverRoot, mt);
            writer.WriteLine("<a href='{0}plot/?mt={1}&a=autorefresh&q=plot(X,Y)'>Scope Mt</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}siprolog/?mt={1}&a=autorefresh'>Watch Mt</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}siprolog/?mt={1}&q=clear'>Clear Prolog KB</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}xrdf/?mt={1}&q=clearcache'>Clear RDF Cache for KB</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}xrdf/?mt={1}&q=syncfromremote'>Sync From Remote</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}xrdf/?mt={1}&q=synctoremote'>Sync To Remote</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}xrdf/?mt={1}&q=pl2rdf'>Prolog2RDF</a> ", serverRoot, mt);
            writer.WriteLine("<a href='{0}xrdf/?mt={1}&q=rdf2pl'>RDF2Prolog</a> ", serverRoot, mt);
            writer.WriteLine("<br/>");

            if (qnode != null)
            {
                ensureCompiled(qnode, ContentBackingStore.Prolog);
                if (RdfDeveloperSanityChecks > 2)
                {
                    var kbContents0 = findVisibleKBRulesSorted(mt);
                    ensureCompiled(qnode, ContentBackingStore.RdfMemory);
                    ensureCompiled(qnode, ContentBackingStore.Prolog);
                    var kbContents1 = findVisibleKBRulesSorted(mt);
                    if (kbContents0.Count != kbContents1.Count)
                    {
                        Warn("findVisibleKBRulesSorted changed size {0}->{1}", kbContents0.Count, kbContents1.Count);
                    }
                }
            }
            var kbContents = findVisibleKBRulesSorted(mt);
            int total = kbContents.Count;
            int local = 0;
            int showInheritedCount = 300;
            if (qnode != null) local = qnode.pdb.rules.Count;
            int inherited = total - local;
            bool showInherited = (local > inherited);
            if (local == 0) showInherited = true;
            if (inherited < showInheritedCount)
            {
                showInheritedCount = inherited;
            }
            writer.WriteLine(
                "<h3> KB Contents (<font color='blue'>Blue local</font> {0}) (<font color='darkgreen'>Green Inherited ({2})</font> {1})</h3>",
                local, inherited,
                !showInherited ? "unshown" :
                showInheritedCount >= inherited ? "all shown" : "showing the first " + showInheritedCount);
            if (toplevel) writer.WriteLine("<hr/>");
            int shown = 0;
            var qnodeID = qnode == null ? mt : qnode.id;
            foreach (Rule r in kbContents)
            {
                var rmt = r.optHomeMt;
                bool localMT = (qnodeID == rmt);
                if (!localMT)
                {
                    if (!showInherited)
                        continue;
                    shown++;
                    if (shown > showInheritedCount) showInherited = false;
                }
                WriteRule(writer, r, qnode);
            }
            if (qnode == null) return;

            if (!qnode.IsDataFrom(ContentBackingStore.Prolog))
            {
                WriteMtInfoRDF(writer, qnode, mt, serverRoot, toplevel);
            }
        }
        public void WriteMtInfoRDF(TextWriter writer, PNode qnode, string mt, string serverRoot, bool toplevel)
        {
            if (qnode != null)
            {
                if (qnode.SyncFrequency != FrequencyOfSync.Never) ensureCompiled(qnode, ContentBackingStore.RdfMemory);
            }
            var gwf = FindOrCreateKB(mt);
            if (gwf != null)
            {
                try
                {
                    WriteGraph(writer, gwf.rdfGraph, gwf.definations, mt);
                }
                catch (Exception e)
                {
                    writer.WriteLine("<font color='red'>{0} {1} {2}</font>", e.GetType(), e.Message, e.StackTrace);
                }
            }
            else
            {
                writer.WriteLine("<h3> KB Triples {1}</h3> Not synced <a href='{0}xrdf/?mt={1}&q=pl2rdf'>Sync Now</a> ", serverRoot, mt);
            }
        }

        private void WriteRule(TextWriter writer, Rule r, PNode qnode)
        {
            var mt = r.optHomeMt;
            bool localMT = qnode.id == mt;
            string color = localMT ? "blue" : "darkgreen";
            string ext = localMT ? "" : string.Format("&nbsp;&nbsp;%<a href='{0}xrdf/?mt={1}'>{1}</a>", threadLocal.tl_ServerRoot, mt);


            string toolTip = "";
            /*var rdf = r.RdfRuleValue(); 
            if (rdf != null)
            {
                toolTip = string.Format("onmouseover=\"showtip('{0}')\" ", rdf.ToString().Replace("\"", "\\\"").Replace("'", "\\'"));
            }*/
            writer.WriteLine("<font color='{0}' {1}>{2}</font>{3}<br/>", color, toolTip,
                           WebLinksWriter.EntityFormat(r.ToSource(SourceLanguage.Prolog)), ext);
        }

    }

    public static class threadLocal
    {
        [ThreadStatic] public static SourceLanguage tl_console_language = null;//SourceLanguage.Text;
        public static string tl_languageName
        {
            get
            {
                if (tl_console_language == null) return null;
                return tl_console_language.Name;
            }
        }
        [ThreadStatic]
        internal static string tl_ServerRoot;
        [ThreadStatic]
        internal static string tl_mt;
        [ThreadStatic]
        internal static string tl_rule_mt;
        internal static string curKB
        {
            get
            {
                return tl_mt;
            }
            set
            {
                tl_mt = value;
            }
        }
        [ThreadStatic]
        internal static TextWriter tl_writer;

        [ThreadStatic]
        private static int tl_StructToStringDepth = 4;
        public static string StructToString(object t)
        {
            int before = tl_StructToStringDepth;
            try
            {
                return StructToString1(t, 2);
            }
            finally
            {
                tl_StructToStringDepth = before;
            }
        }

        private static bool HasElements(ICollection props)
        {
            return props != null && props.Count > 0;
        }
        public static string StructToString1(object t, int depth)
        {
            if (t == null) return "NULL";
            Type structType = t.GetType();
            if (t is IConvertible || t is String || t is Uri || t is Stream || t is IComparable<string>) return "" + t;
            if (tl_StructToStringDepth > depth)
            {
                tl_StructToStringDepth = depth;
            }
            else
            {
                depth = tl_StructToStringDepth;
            }
            if (structType.IsValueType) depth++;
            if (depth < 0) return "^";// +t;
            StringBuilder result = new StringBuilder();
            if (t is IEnumerable)
            {
                IEnumerable ic = t as IEnumerable;
                int max = 10;
                int fnd = 0;
                bool printSomething = true;
                result.Append("Items: [");
                foreach (var i in ic)
                {
                    if (printSomething)
                    {
                        result.Append(fnd + ": " + StructToString1(i, depth - 1) + " ");
                        tl_StructToStringDepth = depth;
                    }
                    fnd++;
                    max--;
                    if (max < 1)
                    {
                        if (printSomething) result.Append("...");
                        printSomething = false;
                    }
                }
                result.Append("]");
                return "CollectionType: " + structType + " Count: " + fnd + " " + result.ToString().TrimEnd();
            }
            const BindingFlags fpub = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags fpriv = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = structType.GetFields(fpub);
            PropertyInfo[] props = structType.GetProperties(fpub);
            bool hasProps = HasElements(props);
            if (!HasElements(fields) && !hasProps)
            {
                fields = structType.GetFields(fpriv);
            }
            if (!HasElements(props))
            {
                props = structType.GetProperties(fpriv);
            }
            bool needSimpleToString = true;

            HashSet<string> unneeded = new HashSet<string>();
            //if (HasElements(fields))
            {
                foreach (PropertyInfo prop in props)
                {
                    if (prop.GetIndexParameters().Length != 0) continue;
                    needSimpleToString = false;
                    string propname = prop.Name;
                    if (propname == "AToString") continue;
                    unneeded.Add(propname.Trim('m', '_').ToUpper());
                    result.Append("{" + propname + ": " + StructToString1(prop.GetValue(t, null), depth - 1) + "}");
                    tl_StructToStringDepth = depth;
                }
            }
            //if (needSimpleToString)
            {
                foreach (FieldInfo prop in fields)
                {
                    string propname = prop.Name;
                    if (unneeded.Contains(propname.Trim('m', '_').ToUpper())) continue;
                    needSimpleToString = false;
                    result.Append("{" + propname + ": " + StructToString1(prop.GetValue(t), depth - 1) + "}");
                    tl_StructToStringDepth = depth;
                }
            }

            if (needSimpleToString)
            {
                return "" + t;
            }

            return result.ToString().TrimEnd();
        }
 
    }
    public partial class SIProlog
    {
 
        public void interactQuery(TextWriter writer, string query, string mt, string serverRoot)
        {
            int testdepth = 64;

            List<Dictionary<string, string>> bingingsList = new List<Dictionary<string, string>>();
            while ((bingingsList.Count == 0) && (testdepth < 1024))
            {
                testdepth = (int)(testdepth * 1.5);
                //ConsoleWriteLine("Trying depth {0}", testdepth);
                maxdepth = testdepth;
                askQuery(query, mt, out bingingsList);
            }
            writer.WriteLine("<h3>Query:'{0}' in mt={1}</h3><br/>", query, mt);
            if (bingingsList.Count == 0)
            {
                writer.WriteLine("No bindings found at depth {0} in {1}<br/>", testdepth, mt);
            }
            else
            {
                writer.WriteLine("{2} bindings found at depth {0} in {1}<br/>", testdepth, mt, bingingsList.Count);
                int index = 0;
                foreach (Dictionary<string, string> bindings in bingingsList)
                {
                    index++;
                    writer.Write("{0}: ", index);
                    foreach (string k in bindings.Keys)
                    {
                        string v = bindings[k];
                        writer.Write("{0}={1} ", k, v);
                    }
                    writer.WriteLine("<br/>");
                }
                writer.WriteLine("<hr/>");

            }
        }
        public void interactFooter(TextWriter writer, string mt, string serverRoot)
        {
            writer.WriteLine("<hr/>");

            writer.WriteLine(" <form method='get' ACTION='{1}siprolog/'>", mt, serverRoot);
            writer.WriteLine(" Query: <INPUT TYPE='text' name='q'/>");
            MtSelector(writer, mt);
            writer.WriteLine(" <INPUT TYPE='hidden' name='a' VALUE='query'/>");
            writer.WriteLine(" <INPUT TYPE='submit' VALUE='submit'/>");
            writer.WriteLine(" </FORM>");
            writer.WriteLine(" <form method='get' ACTION='{1}siprolog/'>", mt, serverRoot);
            writer.WriteLine(" AutoQuery: <INPUT TYPE='text' name='q'/>");
            MtSelector(writer, mt);
            writer.WriteLine(" <INPUT TYPE='hidden' name='a' VALUE='autoquery'/>");
            writer.WriteLine(" <INPUT TYPE='submit' VALUE='submit'/>");
            writer.WriteLine(" </FORM>");
            writer.WriteLine(" <form method='get' ACTION='{1}siprolog/'>", mt, serverRoot);
            writer.WriteLine(" Append: <INPUT TYPE='text' name='q'/>");
            MtSelector(writer, mt);
            writer.WriteLine(" <INPUT TYPE='hidden' name='a' VALUE='append'/>");
            writer.WriteLine(" <INPUT TYPE='submit' VALUE='submit'/>");
            writer.WriteLine(" </FORM>");
            writer.WriteLine(" <form method='get' ACTION='{1}siprolog/'>", mt, serverRoot);
            writer.WriteLine(" Overwrite: <INPUT TYPE='text' name='q'/>");
            MtSelector(writer, mt);
            writer.WriteLine(" <INPUT TYPE='hidden' name='a' VALUE='insert'/>");
            writer.WriteLine(" <INPUT TYPE='submit' VALUE='submit'/>");
            writer.WriteLine(" </FORM>");

        }

        private void MtSelector(TextWriter writer, string mt)
        {
            if (string.IsNullOrEmpty(mt))
            {
                writer.WriteLine(" MT: <INPUT TYPE='text' name='mt' VALUE='{0}'/>", mt);
            }
            else
            {
                writer.WriteLine(" MT: <INPUT TYPE='text' name='mt' VALUE='{0}'/>", mt);
            }
        }

    }
}

