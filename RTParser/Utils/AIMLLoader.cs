using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.IO;
using System.Text;
using RTParser.AIMLTagHandlers;
using UPath = RTParser.Unifiable;

namespace RTParser.Utils
{
    /// <summary>
    /// A utility class for loading AIML files from disk into the graphmaster structure that 
    /// forms an AIML RProcessor's "brain"
    /// </summary>
    public class AIMLLoader
    {
        #region Attributes
        /// <summary>
        /// The RProcessor whose brain is being processed
        /// </summary>
        private RTParser.RTPBot RProcessor;

        /// <summary>
        /// Allow all chars in RawUserInput
        /// </summary>
        public bool RawUserInput = false;
        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot whose brain is being processed</param>
        public AIMLLoader(RTParser.RTPBot bot, Request request)
        {
            this.RProcessor = bot;
            this.CurrentRequest = request;
        }

        #region Methods

        /// <summary>
        /// Loads the AIML from files found in the RProcessor's AIMLpath into the RProcessor's brain
        /// </summary>
        public void loadAIML(Request request)
        {
            this.loadAIML(this.RProcessor.PathToAIML, LoaderOptions.GetDefault(request), request);
        }

        public void loadAIML(string path)
        {
            this.RProcessor.loadAIMLFromURI(path, null);
        }

        /// <summary>
        /// Loads the AIML from files found in the path
        /// </summary>
        /// <param name="path"></param>
        public void loadAIML(string path, LoaderOptions options, Request request)
        {
            request = EnsureRequest(request);
            path = ResolveToURI(path, options, request);
            writeToLog("*** Begin loadAIML From Location: '{0}' ***", path);
            RProcessor.ReloadHooks.Add(() => loadAIML(path, options, request));
            if (Directory.Exists(path))
            {
                // process the AIML
                loadAIMLDir(path, options, request);
            }
            else if (File.Exists(path))
            {
                this.loadAIMLFile(path, options, request);
            }
            else
            {
                this.loadAIMLURI(path, options, request);
            }
            writeToLog("*** End loadAIML From Location: '{0}' ***", path);
        }

        private void loadAIMLDir(string path, LoaderOptions options, Request request)
        {
            request = EnsureRequest(request);
            path = ResolveToURI(path, options, request);
            writeToLog("Starting to process AIML files found in the directory " + path);           

            string[] fileEntries = Directory.GetFiles(path, "*.aiml");
            if (fileEntries.Length > 0)
            {
                foreach (string filename in fileEntries)
                {
                    try
                    {
                        if (RProcessor.IsFileLoaded(filename))
                        {
                            //writeToLog("(skipping) " + filename);
                            continue;
                        }
                        this.loadAIMLFile(filename, options, request);
                    }
                    catch (Exception ee)
                    {
                        RProcessor.RemoveFileLoaded(filename);
                        RTPBot.writeDebugLine("" + ee);
                        RProcessor.writeToLog("Error in loadAIMLFile " + ee);
                    }
                }
                writeToLog("Finished processing the AIML files. " + Convert.ToString(request.Graph.Size) + " categories processed.");
            }
            else
            {
                writeToLog("Could not find any .aiml files in the specified directory (" + path + "). Please make sure that your aiml file end in a lowercase aiml extension, for example - myFile.aiml is valid but myFile.AIML is not.");
            }

            if (options.recurse)
            {
                foreach (string filename in Directory.GetDirectories(path))
                {
                    loadAIMLDir(path + Path.DirectorySeparatorChar + filename, options, request);
                }
            }

        }

        string ResolveToURI(string pathIn, LoaderOptions loadOpts, Request request)
        {
            string baseFile = GetBaseFile(loadOpts);
            IEnumerable<String> baseFiles = new string[] {"", ".", baseFile, "aiml"};
            foreach (var s in baseFiles)
            {
                string path = (s != null && s != "") ? Path.Combine(s, pathIn) : pathIn;
                if (Directory.Exists(path))
                {
                    return path;
                }
                if (File.Exists(path))
                {
                    return path;
                }
                if (Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                {
                    try
                    {
                        var uri = new Uri(path);
                        if (uri.IsFile || uri.IsUnc)
                        {
                            return uri.AbsolutePath;
                        }
                    }
                    catch (Exception exception)
                    {

                    }
                }
                
            }
            return pathIn;
        }

        private string GetBaseFile(LoaderOptions loadOpts)
        {
            string baseFile = loadOpts.Filename ?? loadOpts.PrevFilename;
            if (baseFile == null) return ".";
            if (Directory.Exists(baseFile))
            {
                baseFile = (new DirectoryInfo(baseFile)).FullName;
            }
            if (File.Exists(baseFile))
            {
                baseFile = (new FileInfo(baseFile)).DirectoryName;
            }
            return baseFile;
        }

        private void loadAIMLURI(string path, LoaderOptions loadOpts, Request request)
        {
            request = EnsureRequest(request);
            path = ResolveToURI(path, loadOpts, request);
            try
            {
                writeToLog("Processing AIML URI: " + path);
                if (Directory.Exists(path))
                {
                    loadAIMLDir(path, loadOpts, request);
                }
                else if (File.Exists(path))
                {
                    loadAIMLFile(path, loadOpts, request);
                }
                else if (Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                {                   
                    var uri = new Uri(path);
                    if (uri.IsFile)
                    {
                        loadAIMLURI(uri.AbsolutePath, loadOpts, request);
                        return;
                    }
                    WebRequest req = WebRequest.Create(uri);
                    WebResponse resp = req.GetResponse();
                    Stream stream = resp.GetResponseStream();
                    string pfile = loadOpts.Filename;
                    try
                    {
                        loadOpts.Filename = path;
                        loadAIMLStream(stream, loadOpts, request);
                    }
                    finally
                    {
                        loadOpts.Filename = pfile; 
                    }
                }
                else
                {
                    String nf = "ERROR: XmlTextReader of AIML files (" + path + ")";
                    throw new FileNotFoundException(nf);
                }
                writeToLog("Completed AIML URI: " + path);
            }
            catch (Exception e)
            {
                RProcessor.writeToLog(e);
                throw e;
            }
        }

        /// <summary>
        /// Given the name of a file in the AIML path directory, attempts to load it into the 
        /// graphmaster
        /// </summary>
        /// <param name="filename">The name of the file to process</param>
        public void loadAIMLFile(string filename, LoaderOptions opt, Request request)
        {
            request = EnsureRequest(request);
            filename = ResolveToURI(filename, opt, request);
            if (Directory.Exists(filename))
            {
                writeToLog("Processing directory: " + filename);
                if (opt.recurse) loadAIMLDir(filename, opt, request);
                return;
            }
            try
            {
                // load the document
                string s = new FileInfo(filename).FullName;
                if (RProcessor.IsFileLoaded(filename))
                {
                    writeToLog("Already loaded! " + filename + " => " + s);
                    return;
                }
                writeToLog("Processing AIML file: " + filename + " into " + request.Graph);
                RProcessor.AddFileLoaded(filename);
                var tr = File.OpenRead(filename);
                try
                {
                    string pfile = opt.Filename;
                    try
                    {
                        opt.Filename = filename;
                        this.loadAIMLStream(tr, opt, request);
                    }
                    finally
                    {
                        opt.Filename = pfile;
                    }
                }
                finally
                {
                    try
                    {
                        tr.Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                writeToLog("Loaded AIMLFile: '{0}'", filename + " into " + request.Graph);
                return;
            }
            catch (Exception e)
            {
                RProcessor.RemoveFileLoaded(filename);
                writeToLog("Error in AIML Stacktrace: " + filename + "\n  " + e.Message + "\n" + e.StackTrace);
                writeToLog("Error in AIML file: " + filename + " Message " + e.Message);
            }
        }
        /// <summary>
        /// Given an XML document containing valid AIML, attempts to load it into the graphmaster
        /// </summary>
        /// <param name="doc">The XML document containing the AIML</param>
        /// <param name="filename">Where the XML document originated</param>
        public void loadAIMLString(string input, LoaderOptions filename, Request request)
        {
            request = EnsureRequest(request);
            try
            {
                byte[] byteArray = Encoding.ASCII.GetBytes(input);
                MemoryStream stream = new MemoryStream(byteArray);
                loadAIMLStream(stream, filename, request);
            }
            catch (Exception e2)
            {
                String s = "which causes loadAIMLString '" + input + "' " + filename;
                s = s + "\n" + e2.Message + "\n" + e2.StackTrace + "\n" + s;
                writeToLog(s);
                throw e2;
            }

            return;
        }

        /// <summary>
        /// Given an XML document containing valid AIML, attempts to load it into the graphmaster
        /// </summary>
        /// <param name="doc">The XML document containing the AIML</param>
        /// <param name="filename">Where the XML document originated</param>
        public void loadAIMLStream(Stream input, LoaderOptions filename, Request request)
        {
            request = EnsureRequest(request);
            var xtr = XmlDocumentLineInfo.CreateXmlTextReader(input);
            while (!xtr.EOF)
            {
                try
                {
                    XmlDocumentLineInfo doc = new XmlDocumentLineInfo(input, "" + filename);
                    doc.Load(xtr);
                    if (doc.DocumentElement == null)
                    {
                        RProcessor.writeToLog("No Document at " + filename);
                        continue;
                    }
                    this.loadAIMLNode(doc.DocumentElement, filename, request);
                }
                catch (Exception e2)
                {
                    String s = "which causes loadAIMLStream '" + input + "' " + filename + " charpos=" + input.Position;
                    s = s + "\n" + e2.Message + "\n" + e2.StackTrace + "\n" + s;
                    writeToLog(s);
                    //System.Console.Flush();
                    if (!xtr.Read())
                    {
                        throw e2;
                    }
                    // 
                }

            }
            return;
        }

        public void loadAIMLNode(XmlNode currentNode, LoaderOptions filename, Request request)
        {
            request = EnsureRequest(request);
            var prev = RProcessor.Loader;
            try
            {
                RProcessor.Loader = this;
                if (currentNode.NodeType == XmlNodeType.Comment) return;
                // Get a list of the nodes that are children of the <aiml> tag
                // these nodes should only be either <topic> or <category>
                // the <topic> nodes will contain more <category> nodes
                string currentNodeName = currentNode.Name.ToLower();
                if (currentNodeName == "aiml")
                {
                    string graphname = RTPBot.GetAttribValue(currentNode, "graph", null);
                    GraphMaster g = request.Graph;
                    if (graphname != null)
                    {
                        GraphMaster g2 = RProcessor.GetGraph(graphname, g);
                        request.Graph = g2;
                        filename.Graph = g2;
                    }
                    try
                    {
                        // process each of these child nodes
                        foreach (XmlNode child in currentNode.ChildNodes)
                        {
                            loadAIMLNode(child, filename, request);
                        }
                        return;
                    }
                    finally
                    {
                        request.Graph = g;
                        filename.Graph = g;
                    }
                }
                if (currentNodeName == "root")
                {
                    // process each of these child "settings"? nodes
                    foreach (XmlNode child in currentNode.ChildNodes)
                    {
                        loadAIMLNode(child, filename, request);
                    }
                    return;
                }
                if (currentNodeName == "item")
                {
                    this.RProcessor.GlobalSettings.loadSettingNode(currentNode);
                    return;
                }

                if (currentNodeName == "topic")
                {
                    this.processTopic(currentNode, currentNode.ParentNode, filename);
                }
                else if (currentNodeName == "category")
                {
                    this.processCategory(currentNode, currentNode.ParentNode, filename);
                }
                else if (currentNodeName == "meta")
                {
                    writeToLog("UNUSED: " + currentNode.OuterXml);
                }
                else if (currentNodeName == "srai")
                {
                    EvalNode(currentNode, request, filename);
                }
                else
                {
                    writeToLog("ImmediateAiml:: " + currentNode.OuterXml);
                    EvalNode(currentNode, request, filename);
                }
            }
            finally
            {
                RProcessor.Loader = prev;
            }
        }

        private Request EnsureRequest(Request request)
        {
            if (request == null)
            {
                writeToLog("ERROR! Ensuring Request=" + CurrentRequest);
                request = CurrentRequest;
            }
            return request;
        }

        private void EvalNode(XmlNode currentNode, Request request, LoaderOptions filename)
        {
            request = EnsureRequest(request);
            try
            {
                RProcessor.ImmediateAiml(currentNode, request, this, null);
            }
            catch (Exception e)
            {
                RProcessor.writeToLog(e);
                writeToLog("ImmediateAiml: ERROR: " + e);
                XmlNode element = currentNode; 
                string s = LineTextInfo(element) + " " + LineNumberInfo(element);
                writeToLog("ERROR PARSING: " + s);
            }
        }

        /// <summary>
        /// Given a "topic" topicNode, processes all the categories for the topic and adds them to the 
        /// graphmaster "brain"
        /// </summary>
        /// <param name="topicNode">the "topic" node</param>
        /// <param name="filename">the file from which this topicNode is taken</param>
        public void processTopic(XmlNode topicNode, XmlNode outerNode, LoaderOptions filename)
        {
            // find the name of the topic or set to default "*"
            Unifiable topicName = RTPBot.GetAttribValue(topicNode, "name", Unifiable.STAR);
            // process all the category nodes
            foreach (XmlNode cateNode in topicNode.ChildNodes)
            {
                if (cateNode.Name == "category")
                {
                    processCategoryWithTopic(cateNode, topicName, topicNode, filename);
                }
            }
        }

        /// <summary>
        /// Adds a category to the graphmaster structure using the default topic ("*")
        /// </summary>
        /// <param name="cateNode">the XML node containing the category</param>
        /// <param name="filename">the file from which this category was taken</param>
        public void processCategory(XmlNode cateNode, XmlNode outerNode, LoaderOptions filename)
        {
            this.processCategoryWithTopic(cateNode, Unifiable.STAR, outerNode, filename);
        }

        /// <summary>
        /// Adds a category to the graphmaster structure using the given topic
        /// </summary>
        /// <param name="cateNode">the XML node containing the category</param>
        /// <param name="topicName">the topic to be used</param>
        /// <param name="filename">the file from which this category was taken</param>
        private void processCategoryWithTopic(XmlNode cateNode, Unifiable topicName, XmlNode outerNode, LoaderOptions filename)
        {
            // reference and check the required nodes
            List<XmlNode> patterns = FindNodes("pattern", cateNode);
            List<XmlNode> templates = FindNodes("template", cateNode);
            foreach (XmlNode pattern in patterns)
            {
                foreach (var template in templates)
                {
                    if (Equals(null, pattern))
                    {
                        throw new XmlException("Missing pattern tag in a cateNode found in " + filename);
                    }
                    if (Equals(null, template))
                    {
                        throw new XmlException("Missing template tag in the cateNode with pattern: " + pattern.InnerText + " found in " + filename);
                    }
                    addCatNode(cateNode, pattern, filename, template, topicName, outerNode);
                }
            }
        }

        private void addCatNode(XmlNode cateNode, XmlNode patternNode, LoaderOptions filename, XmlNode templateNode,
            Unifiable topicName, XmlNode outerNode)
        {
            XmlNode guardnode = FindNode("guard", cateNode, FindNode("guard", outerNode, null));
            GuardInfo guard = guardnode == null ? null : GuardInfo.GetGuardInfo(guardnode);


            XmlNode newPattern;
            Unifiable patternText;
            Unifiable that = extractThat(patternNode, "that", cateNode, out patternText, out newPattern).InnerXml;
            patternNode = newPattern;
            Unifiable cond = extractThat(patternNode, "flag", cateNode, out patternText, out newPattern).InnerXml;
            patternNode = newPattern;
            XmlNode topicTagText = extractThat(patternNode, "topic", cateNode, out patternText, out newPattern);
            patternNode = newPattern;
            if (filename.DebugFiles && !ContansNoInfo(topicTagText.InnerXml))
            {
                var s = RTPBot.GetAttribValue(topicTagText, "name", Unifiable.STAR);
                if (topicName != s)
                {
                    throw new InvalidOperationException(cateNode.OuterXml);
                }
            }
            Unifiable categoryPath = generateCPath(patternText, that, cond, topicName, false);
            PatternInfo patternInfo = PatternInfo.GetPattern(filename, patternNode, categoryPath);
            TopicInfo topicInfo = TopicInfo.FindTopic(filename, topicName);
            ThatInfo thatInfo = ThatInfo.GetPattern(filename, that);

            // o.k., add the processed AIML to the GraphMaster structure
            if (!categoryPath.IsEmpty)
            {
                try
                {
                    CategoryInfo categoryInfo = CategoryInfo.GetCategoryInfo(patternInfo, cateNode, filename);
                    filename.Graph.addCategoryTag(categoryPath, patternInfo, categoryInfo, 
                                                  outerNode, templateNode, guard, thatInfo);
                }
                catch (Exception e)
                {
                    string s = "ERROR! Failed to load a new category into the graphmaster where the path = " +
                               categoryPath + " and templateNode = " + templateNode.OuterXml +
                               " produced by a category in the file: " + filename + "\n";
                    writeToLog(s + e + "\n" + s);
                }
            }
            else
            {
                writeToLog("WARNING! Attempted to load a new category with an empty patternNode where the path = " + categoryPath + " and templateNode = " + templateNode.OuterXml + " produced by a category in the file: " + filename);
            }
        }

        public void writeToLog(string message, params object[] args)
        {
            this.RProcessor.writeToLog("LOADERTRACE: "+message, args);
            try
            {
                Console.Out.Flush();
                Console.Error.Flush();
            }
            catch { }
        }

        private static bool ContansNoInfo(Unifiable cond)
        {
            return cond == null || cond == Unifiable.STAR || cond == Unifiable.Empty;
        }

        /// <summary>
        /// Generates a path from a category XML cateNode and topic name
        /// </summary>
        /// <param name="cateNode">the category XML node</param>
        /// <param name="topicName">the topic</param>
        /// <param name="isUserInput">marks the path to be created as originating from user input - so
        /// normalize out the * and _ wildcards used by AIML</param>
        /// <returns>The appropriately processed path</returns>
        static XmlNode extractThat(XmlNode patternNode, String tagname, XmlNode cateNode, out Unifiable patternText, out XmlNode newPattern)
        {
            // get the nodes that we need
            XmlNode that = FindNodeOrHigher(tagname, cateNode, null);

            //Unifiable thatText = Unifiable.STAR;

            if (Equals(null, patternNode))
            {
                patternText = Unifiable.Empty;
            }
            else
            {
                patternText = Unifiable.Create(Unifiable.InnerXmlText(patternNode));
            }

            string patternString = patternNode.InnerXml;
            int f = patternString.IndexOf("<" + tagname);
            if (f >= 0)
            {
                that = FindNode(tagname, patternNode, null);
                if (that == null)
                {
                    throw new NotImplementedException("generatePathExtractWhat: " + patternString);
                }
                string thatString = that.OuterXml;
                if (!patternString.Contains(thatString))
                {
                    throw new NotImplementedException("generatePathExtractWhat: " + patternString);
                }
                patternString = MatchKeyClean(patternString.Replace(thatString, ""));
                var newLineInfoPattern = AIMLTagHandler.getNode("<pattern>" + patternString + "</pattern>", patternNode);
                newLineInfoPattern.SetParentFromNode((LineInfoElement)patternNode);
                patternNode = newLineInfoPattern;
                patternText = Unifiable.Create(Unifiable.InnerXmlText(patternNode));
            }

            newPattern = patternNode;
            if (!Equals(null, that))
            {
                //thatText = that.InnerXml;
            }
            return that ?? PatternStar;// hatText;//this.generatePath(patternText, thatText, topicName, isUserInput);
        }

        protected static XmlNode PatternStar = AIMLTagHandler.getNode("<pattern name=\"*\">*</pattern>");
        public bool ThatWideStar = false;
        private Request CurrentRequest;

        /// <summary>
        /// Given a name will try to find a node named "name" in the childnodes or return null
        /// </summary>
        /// <param name="name">The name of the node</param>
        /// <param name="node">The node whose children need searching</param>
        /// <returns>The node (or null)</returns>
        static public XmlNode FindNode(string name, XmlNode node, XmlNode ifMissing)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return ifMissing;
        }
        static public XmlNode FindNodeOrHigher(string name, XmlNode node, XmlNode ifMissing)
        {
            if (node == null) return ifMissing;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return FindHigher(name, node.ParentNode, ifMissing);
        }
        static public XmlNode FindHigher(string name, XmlNode node, XmlNode ifMissing)
        {
            if (node == null) return ifMissing;
            if (node.Name == name)
            {
                return node;
            }
            return FindHigher(name, node.ParentNode, ifMissing);
        }
        static public List<XmlNode> FindNodes(string name, XmlNode node)
        {
            name = name.ToLower();
            List<XmlNode> nodes = new List<XmlNode>();
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name.ToLower() == name)
                {
                    nodes.Add(child);
                }
            }
            return nodes;
        }

        /// <summary>
        /// Generates a path from the passed arguments
        /// </summary>
        /// <param name="pattern">the pattern</param>
        /// <param name="that">the that</param>
        /// <param name="topicName">the topic</param>
        /// <param name="isUserInput">marks the path to be created as originating from user input - so
        /// normalize out the * and _ wildcards used by AIML</param>
        /// <returns>The appropriately processed path</returns>
        public Unifiable generatePath(Unifiable pattern, Unifiable that, Unifiable flag, Unifiable topicName, bool isUserInput)
        {
            return Unifiable.MakePath(generateCPath(pattern, that, flag, topicName, isUserInput));
        }

        /// <summary>
        /// Generates a path from the passed arguments
        /// </summary>
        /// <param name="pattern">the pattern</param>
        /// <param name="that">the that</param>
        /// <param name="topicName">the topic</param>
        /// <param name="isUserInput">marks the path to be created as originating from user input - so
        /// normalize out the * and _ wildcards used by AIML</param>
        /// <returns>The appropriately processed path</returns>
        public Unifiable generateCPath(Unifiable pattern, Unifiable that, Unifiable flag, Unifiable topicName, bool isUserInput)
        {
            // to hold the normalized path to be entered into the graphmaster
            Unifiable normalizedPath = Unifiable.CreateAppendable();
            string normalizedPattern;// = Unifiable.Empty;
            Unifiable normalizedThat;// = Unifiable.STAR;
            Unifiable normalizedTopic;// = Unifiable.STAR;
            bool UseRawUserInput = RawUserInput;
            string patString = " " + pattern.AsString() + " ";
            if (patString.Contains(" exec ") || patString.Contains(" aiml ")
                || patString.Contains(" lisp ") || patString.Contains(" tag ")
                || patString.Contains("<") || patString.Contains(">")
                || patString.Contains("\"") || patString.Contains("=") || patString.Contains("#$")
                || patString.Contains("~") || patString.Contains("*"))
            {
                UseRawUserInput = true;
            }
            if ((this.RProcessor.TrustAIML) & (!isUserInput || UseRawUserInput))
            {

                normalizedPattern = pattern.Trim();
                while (normalizedPattern.EndsWith("?") || normalizedPattern.EndsWith("."))
                {
                    normalizedPattern = normalizedPattern.Substring(0, normalizedPattern.Length - 1).Trim();
                }

                normalizedPattern = MatchKeyClean(normalizedPattern);
                normalizedThat = MatchKeyClean(that);
                normalizedTopic = MatchKeyClean(topicName);
            }
            else
            {
                normalizedPattern = this.Normalize(pattern, isUserInput).Trim();
                normalizedThat = this.Normalize(that, isUserInput).Trim();
                normalizedTopic = this.Normalize(topicName, isUserInput).Trim();
            }

            // check sizes
            if (normalizedPattern.Length > 0)
            {
                if (normalizedThat.IsEmpty)
                {
                    normalizedThat = Unifiable.STAR;
                }
                else
                {
                    if (!normalizedThat.IsWildCard())
                    {
                        // normalizedThat = "* " + normalizedThat;
                    }
                }
                if (normalizedTopic.IsEmpty)
                {
                    normalizedTopic = Unifiable.STAR;
                }

                if (ThatWideStar)
                {
                    if (!isUserInput)
                        normalizedThat = "* " + normalizedThat;
                    else
                        normalizedThat = "* " + normalizedThat;
                    //normalizedThat = "<br/> " + normalizedThat;
                }

                // This check is in place to avoid huge "that" elements having to be processed by the 
                // graphmaster. 
                //if (normalizedThat.Length > this.RProcessor.MaxThatSize)
                //{
                //    normalizedThat = Unifiable.STAR;
                //}

                // o.k. build the path
                normalizedPath.Append(Unifiable.Create(normalizedPattern));
                if (RProcessor.UseInlineThat)
                {
                    normalizedPath.Append(Unifiable.ThatTag);
                    normalizedPath.Append(normalizedThat);
                }
                normalizedPath.Append(Unifiable.FlagTag);
                normalizedPath.Append(flag);
                normalizedPath.Append(Unifiable.TopicTag);
                normalizedPath.Append(normalizedTopic);

                return normalizedPath;//.Frozen();
            }
            else
            {
                return Unifiable.Empty;
            }
        }
        public static string MatchKeyClean(Unifiable unifiable)
        {
            return MatchKeyClean(unifiable.AsString());
        }

        public static string MatchKeyClean(string s)
        {
            s = CleanWhitepaces(s).Trim();
            s = s.Replace("  ", " ");
            if (s == "")
            {
                return "*";
            }
            return s;
        }

        /// <summary>
        /// Given an input, provide a normalized output
        /// </summary>
        /// <param name="input">The Unifiable to be normalized</param>
        /// <param name="isUserInput">True if the Unifiable being normalized is part of the user input path - 
        /// flags that we need to normalize out * and _ chars</param>
        /// <returns>The normalized Unifiable</returns>
        public Unifiable Normalize(string input, bool isUserInput)
        {

            input = CleanWhitepaces(input);
            while (input.EndsWith("?") || input.EndsWith(".") || input.EndsWith("!"))
            {
                input = input.Substring(0, input.Length - 1).Trim();
            }
            if (isUserInput && false)
            {
                return input;
            }

            Unifiable result = Unifiable.CreateAppendable();

            // objects for normalization of the input
            Normalize.ApplySubstitutions substitutor = new RTParser.Normalize.ApplySubstitutions(this.RProcessor);
            Normalize.StripIllegalCharacters stripper = new RTParser.Normalize.StripIllegalCharacters(this.RProcessor);

            Unifiable substitutedInput = substitutor.Transform(" " + input + " ").Trim();
            // split the pattern into it's component words
            string[] substitutedWords = substitutedInput.AsString().Split(' ');

            // Normalize all words unless they're the AIML wildcards "*" and "_" during AIML loading
            foreach (Unifiable word in substitutedWords)
            {
                Unifiable normalizedWord;
                if (isUserInput)
                {
                    normalizedWord = stripper.Transform(word);
                }
                else
                {
                    if (word.IsWildCard())
                    {
                        normalizedWord = word;
                    }
                    else
                    {
                        normalizedWord = stripper.Transform(word);
                    }
                }
                result.Append(normalizedWord);
            }

            return  Unifiable.ToVMString(result).Replace("  ", " "); // make sure the whitespace is neat
        }
        #endregion

        public static bool AimlSame(string xml1, string xml2)
        {
            if (xml1 == xml2) return true;
            if (xml1 == null) return String.IsNullOrEmpty(xml2);
            if (xml2 == null) return String.IsNullOrEmpty(xml1);
            xml1 = CleanWhitepacesLower(xml1);
            xml2 = CleanWhitepacesLower(xml2);
            if (xml1.Length != xml2.Length) return false;
            if (xml1 == xml2) return true;
            if (xml1.ToUpper() == xml2.ToUpper())
            {
                return true;
            }
            return false;
        }


        public static string CleanWhitepaces(string xml2)
        {
            if (xml2 == null) return xml2;
            const long maxCleanSize = 2 << 16;
            if (xml2.Length > maxCleanSize)
            {
                return xml2;
            }
            String s = "";
            foreach (var c0 in xml2)
            {
                char c = c0;
                if (c < 32) c = ' ';
                s += c;
            }
            s = s.Replace("\n", " ").Replace("\r", " ").
                Replace(" <", "<").Replace("< ", "<").
                Replace(" >", ">").Replace("> ", ">").
                Replace("  ", " ").
                Replace(" />", "/>").Replace("<star index=\"1\"", "<star").Replace(" index=\"1\"", "").Trim();            
            return s;
        }

        public static string CleanWhitepacesLower(string xml2)
        {
            if (xml2 == null) return xml2;
            return CleanWhitepaces(xml2).ToLower().Replace(".", "").Replace("?", "").Replace("!", "");
        }

        public static bool ContainsAiml(Unifiable unifiable)
        {
            String s = unifiable.AsString();
            if (s.Contains(">") && s.Contains("<")) return true;
            if (s.Contains("&"))
            {
                return true;
            }
            return false;
        }

        public static string LineNumberInfo(XmlNode templateNode)
        {

            string s = "<!-- ";
            if (templateNode is LineInfoElement)
            {
                LineInfoElement li = (LineInfoElement)templateNode;
                if (li.lineNumber == 0)
                {
                    XmlNode Parent = templateNode.ParentNode;
                    s = s + " " + li.OwnerDocument.ToString();
                    if (Parent != null && Parent != templateNode)
                    {
                        s = s + " " + LineNumberInfo(Parent);
                    }
                    else
                    {
                        s = s + " (" + li.lineNumber + ":" + li.linePosition + ")";
                    }
                }
                else
                {
                    s = s + " (" + li.OwnerDocument.ToString() + ":line " + li.lineNumber + "," + li.linePosition + ") ";
                }
            }
            return s + " -->";
        }

        public static object LineTextInfo(XmlNode templateNode)
        {
            XmlNode textNode = templateNode;//.ParentNode ?? templateNode;
            string s = CleanWhitepaces(textNode.OuterXml);
            if (String.IsNullOrEmpty(s))
            {
                XmlNode Parent = textNode.ParentNode;
                LineInfoElement li = (LineInfoElement)templateNode;
                s = s + " " + li.OwnerDocument.ToString();
                if (Parent != null && Parent != templateNode)
                {
                    s = s + " " + LineTextInfo(Parent);
                }
                else
                {
                    return s;
                }
            }
            return s;
        }

        public static string CatTextInfo(LineInfoElement element)
        {
            return LineTextInfo(element.ParentNode ?? element) + " " + LineNumberInfo(element);
        }

        public static void PrintResult(Result result, RTPBot.OutputDelegate console)
        {
            console("-----------------------------------------------------------------");
            console("Result: " + result.Graph + " Request: " + result.request);
            foreach (var s in result.InputSentences)
            {
                console("input: \"" + s + "\"");
            }
            PrintTemplates(result.UsedTemplates, console);
            foreach (var s in result.SubQueries)
            {
                console("\n" + s);
            }
            console("-");
            foreach (var s in result.OutputSentences)
            {
                console("outputsentence: " + s);
            }
            console("-----------------------------------------------------------------");
        }

        public static string GetTemplateSource(IEnumerable<TemplateInfo> CI)
        {
            if (CI == null) return "";
            string hide = "";
            foreach (var ci in CI)
            {
                string c = ci.ToFileString();
                string ss = "" + CleanWhitepaces(c) + "\n";
                if (hide.Contains(ss)) continue;
                hide += ss;
            }
            return hide;
        }

        public static void PrintTemplates(IList<TemplateInfo> templates, RTPBot.OutputDelegate console)
        {                
            console(" " + GetTemplateSource(templates));
        }

        public static string CleanWhitepaces(object info)
        {
            if (info is XmlNode)
            {
                XmlNode n = (XmlNode) info;
                if (n.Name == "template") info = n.ParentNode;
            }
            if (info is TemplateInfo)
            {
                info = ((TemplateInfo)info).CategoryInfo;
            }
            return CleanWhitepaces("" + info);
        }

        public static bool IsSilentTag(XmlNode node)
        {
            // if (true) return false;
            if (node.Name == "think") return true;
            if (node.NodeType == XmlNodeType.Text)
            {
                string innerText = node.InnerText;
                if (innerText.Trim().Length == 0)
                {
                    return true;
                }
                return false;
            }
            if (node.Name == "template")
            {
                foreach (XmlNode xmlNode in node.ChildNodes)
                {
                    if (!IsSilentTag(xmlNode)) return false;
                }
                if (node.ChildNodes.Count!=1)
                {
                    return true;
                }
                return true;
            }
            return false;
        }
    }

    public class XmlDocumentLineInfo : XmlDocument
    {
        public override void Load(Stream reader)
        {
            if (reader is IXmlLineInfo)
            {
                LineTracker = (IXmlLineInfo)reader;
            }
            base.Load(reader);
        }

        public override void Load(XmlReader reader)
        {
            if (reader is IXmlLineInfo)
            {
                LineTracker = (IXmlLineInfo)reader;
            }
            base.Load(reader);
        }
        public override void Load(TextReader reader)
        {
            base.Load(new XmlTextReader(reader));
        }

        public override string ToString()
        {
            return InfoString ?? base.ToString();
        }
        private Stream LineInfoReader;
        public IXmlLineInfo LineTracker;
        public string InfoString;
        public XmlDocumentLineInfo(Stream lineInfoReader, string toString)
        {
            InfoString = toString;
            LineInfoReader = lineInfoReader;
        }
        public XmlDocumentLineInfo(string toString)
        {
            InfoString = toString;
        }
        public XmlDocumentLineInfo(IXmlLineInfo lineInfoReader, string toString)
        {
            InfoString = toString;
            LineTracker = lineInfoReader;
        }
        public override void LoadXml(string xml)
        {
            base.Load(new XmlTextReader(new StringReader(xml)));
        }

        public override XmlElement CreateElement(string prefix, string localname, string nsURI)
        {
            LineInfoElement elem = new LineInfoElement(prefix, localname, nsURI, this);
            if (LineInfoReader != null)
                elem.SetPos(LineInfoReader.Position);
            if (LineTracker != null)
            {
                elem.SetLineInfo(LineTracker.LineNumber, LineTracker.LinePosition);
            }
            return elem;
        }

        public static XmlReader CreateXmlTextReader(Stream stream)
        {
            return XmlTextReader.Create(stream, DefaultSettings);
        }
        internal static XmlReader CreateXmlTextReader(StringReader tr)
        {
            XmlTextReader xmlTextReader = new XmlTextReader(tr);
            return XmlTextReader.Create(tr, DefaultSettings);
        }


        /***
         
         This document already has a 'DocumentElement' node.
          at System.Xml.XmlDocument.IsValidChildType(XmlNodeType type)
          at System.Xml.XmlDocument.AppendChildForLoad(XmlNode newChild, XmlDocument doc)
          at System.Xml.XmlLoader.LoadDocSequence(XmlDocument parentDoc)
          at System.Xml.XmlLoader.Load(XmlDocument doc, XmlReader reader, Boolean preserveWhitespace)
          at System.Xml.XmlDocument.Load(XmlReader reader)
                 
         */

        protected static XmlReaderSettings DefaultSettings
        {
            get
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                return settings;
            }
        }

    }

    public class LineInfoElement : XmlElement, IXmlLineInfo
    {
        public int lineNumber = 0;
        public int linePosition = 0;
        public long charPos = 0;
        public LineInfoElement lParent;
        internal LineInfoElement(string prefix, string localname, string nsURI, XmlDocument doc)
            : base(prefix, localname.ToLower(), nsURI, doc)
        {
            //((XmlDocumentLineInfo)doc).IncrementElementCount();
        }
        public void SetLineInfo(int linenum, int linepos)
        {
            lineNumber = linenum;
            linePosition = linepos;            
        }
        public int LineNumber
        {
            get
            {
                return lineNumber;
            }
        }
        public int LinePosition
        {
            get
            {
                return linePosition;
            }
        }
        public bool HasLineInfo()
        {
            return true;
        }

        public void SetPos(long position)
        {
            charPos = position;
        }

        internal void SetParentFromNode(XmlNode xmlNode)
        {
            if (!(xmlNode is LineInfoElement))
            {
                return;
            }
            LineInfoElement lie = (LineInfoElement) xmlNode;
            var pn = xmlNode.ParentNode;
            if (pn is LineInfoElement)
            {
                lParent = (LineInfoElement) pn;
            }
            lineNumber = lie.LineNumber;
            linePosition = lie.linePosition;
            charPos = lie.charPos;
        }

        public override XmlNode ParentNode
        {
            get
            {
                if (lParent == null) return base.ParentNode;
                return lParent;
            }
        }
    } // End LineInfoElement class.
}
