using System;
using System.Threading;
using System.Xml;
using System.Text;
using System.IO;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    /// <summary>
    /// The srai element instructs the AIML interpreter to pass the result of processing the contents 
    /// of the srai element to the AIML matching loop, as if the input had been produced by the user 
    /// (this includes stepping through the entire input normalization process). The srai element does 
    /// not have any attributes. It may contain any AIML template elements. 
    /// 
    /// As with all AIML elements, nested forms should be parsed from inside out, so embedded srais are 
    /// perfectly acceptable. 
    /// </summary>
    public class srai : RTParser.Utils.AIMLTagHandler
    {
        RTParser.RTPBot mybot;
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public srai(RTParser.RTPBot bot,
                        RTParser.User user,
                        RTParser.Utils.SubQuery query,
                        RTParser.Request request,
                        RTParser.Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
            mybot = bot;
        }

        private static int depth = 0;
        protected override Unifiable ProcessChange()
        {
            try
            {
                depth++;
                int d = request.GetCurrentDepth();
                if (d > 30)
                {
                    Console.WriteLine("WARNING Depth pretty deep " + templateNode + " returning empty");
                    return Unifiable.Empty;
                }
                if (depth > 30)
                {
                    Console.WriteLine("WARNING Depth pretty deep " + templateNode + " returning empty");
                    return Unifiable.Empty;
                }
                if (this.templateNode.Name.ToLower() == "srai")
                {
                    if (!templateNodeInnerText.IsEmpty)
                    {
                        Unifiable tempTopic = GetAttribValue("topic", request.Topic);
                        AIMLbot.Request subRequest = new AIMLbot.Request(templateNodeInnerText, this.user, this.Proc);
                        String gn = GetAttribValue("bot", null);
                        if (gn != null) subRequest.Graph = Proc.GetGraph(gn,request.Graph);
                        depth = subRequest.depth = request.depth + 1;
                        subRequest.Topic = tempTopic;
                        subRequest.ParentRequest = this.request;
                        subRequest.StartedOn = this.request.StartedOn;
                        // make sure we don't keep adding time to the request
                        bool showDebug = true;
                        string subRequestrawInput = subRequest.rawInput;
                        if (subRequestrawInput.Contains("SYSTEMANIM") || subRequestrawInput.Contains("HANSANIM"))
                        {
                            showDebug = false;
                        }
                        if (showDebug)
                            Console.WriteLine(" SRAI-- (" + depth + ")" + subRequestrawInput + " ----- @ " +
                                              LineNumberTextInfo());

                       
                        if (mybot.chatTrace != null)
                        {
                           //  mybot.chatTrace.WriteLine("\"L{0}\" -> \"S{1}\" ;\n", depth - 1, depth-1);
                          // mybot.chatTrace.WriteLine("\"L{0}\" -> \"S{1}\" ;\n", depth, depth);
                            //mybot.chatTrace.WriteLine("\"S{0}\" -> \"SRC:{1}\" ;\n", depth, LineNumberTextInfo());

                            //mybot.chatTrace.WriteLine("\"S{0}\" -> \"S{1}\" ;\n", depth - 1, depth);
                            //mybot.chatTrace.WriteLine("\"S{0}\" -> \"SIN:{1}\" ;\n", depth, subRequestrawInput);
                            //mybot.chatTrace.WriteLine("\"SIN:{0}\" -> \"LN:{1}\" ;\n", subRequestrawInput, LineNumberTextInfo());
                        }
                        if (depth > 200)
                        {
                            Console.WriteLine(" SRAI TOOOO DEEEEP <-- (" + depth + ")" + subRequestrawInput +
                                              " ----- @ " +
                                              LineNumberTextInfo());
                            return Unifiable.Empty;
                        }
                        AIMLbot.Result subResult = null;
                        var prev = this.Proc.isAcceptingUserInput;
                        try
                        {
                            this.Proc.isAcceptingUserInput = true;
                            subResult = this.Proc.Chat(subRequest);
                        }
                        finally
                        {
                            this.Proc.isAcceptingUserInput = prev;
                        }
                        this.request.hasTimedOut = subRequest.hasTimedOut;
                        var subQueryRawOutput = subResult.RawOutput.ToValue().Trim();
                        if (Unifiable.IsNullOrEmpty(subQueryRawOutput))
                        {
                            if (showDebug)
                                Console.WriteLine(" SRAI<-- (" + depth + ") MISSING <----- @ " + LineNumberTextInfo());
                            if (mybot.chatTrace != null)
                            {
                                mybot.chatTrace.WriteLine("\"L{0}\" -> \"S{1}\" ;\n", depth, depth);
                                mybot.chatTrace.WriteLine("\"S{0}\" -> \"SIN:{1}\" ;\n", depth, subRequestrawInput);
                                //mybot.chatTrace.WriteLine("\"SIN:{0}\" -> \"LN:{1}\" ;\n", subRequestrawInput, LineNumberTextInfo());
                                mybot.chatTrace.WriteLine("\"SIN:{0}\" -> \"PATH:{1}\" [label=\"{2}\"] ;\n", subRequestrawInput, depth,subResult.NormalizedPaths);
                                mybot.chatTrace.WriteLine("\"PATH:{0}\" -> \"LN:{1}\" [label=\"{2}\"] ;\n", depth, depth, LineNumberTextInfo());
                                
                                mybot.chatTrace.WriteLine("\"LN:{0}\" -> \"RPY:MISSING({1})\" ;\n", depth, depth);
                            }

                            return Unifiable.Empty;
                        }
                        else
                        {
                            if (showDebug)
                                Console.WriteLine(" SRAI<-- (" + depth + ")" + subQueryRawOutput + " @ " + LineNumberTextInfo());
                        }

                        if (mybot.chatTrace != null)
                        {
                            mybot.chatTrace.WriteLine("\"L{0}\" -> \"S{1}\" ;\n", depth, depth);
                            mybot.chatTrace.WriteLine("\"S{0}\" -> \"SIN:{1}\" ;\n", depth, subRequestrawInput);
                            mybot.chatTrace.WriteLine("\"SIN:{0}\" -> \"PATH:{1}\" [label=\"{2}\"] ;\n", subRequestrawInput, depth, subResult.NormalizedPaths);
                            mybot.chatTrace.WriteLine("\"PATH:{0}\" -> \"LN:{1}\" [label=\"{2}\"] ;\n", depth, depth, LineNumberTextInfo());
                            mybot.chatTrace.WriteLine("\"LN:{0}\" -> \"RPY:{1}\" ;\n", depth, subQueryRawOutput);
                        }
                        return subResult.Output;
                    }
                }
                return Unifiable.Empty;
            }
            finally
            {
                depth--;
            }
        }
    }
}
