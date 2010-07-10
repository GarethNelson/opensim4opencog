﻿using System;
using System.Xml;
using System.Text;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    /// <summary>
    /// IMPLEMENTED FOR COMPLETENESS REASONS
    /// </summary>
    public class or : UnifibleTagHandler
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public or(RTParser.RTPBot bot,
                        RTParser.User user,
                        RTParser.Utils.SubQuery query,
                        RTParser.Request request,
                        RTParser.Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }

        public override float CanUnify(Unifiable with)
        {
            if (templateNode.NodeType==XmlNodeType.Text)
            {
                string srch = (" " + with.ToValue() + " ").ToUpper();
                return ((" " + templateNode.InnerText + " ").ToUpper().Contains(srch)) ? OR_TRUE : OR_FALSE;
            }
            if (templateNode.HasChildNodes)
            {
                // recursively check
                foreach (XmlNode childNode in templateNode.ChildNodes)
                {
                    try
                    {
                        if (childNode.NodeType == XmlNodeType.Text)
                        {
                            string srch = (" " + with.ToValue() + " ").ToUpper();
                            return ((" " + childNode.InnerText + " ").ToUpper().Contains(srch)) ? OR_TRUE : OR_FALSE;
                        }
                        AIMLTagHandler part = Proc.GetTagHandler(user, query, request, result, childNode, this);
                        float rate1 = part.CanUnify(with);
                        if (rate1 == 0) return rate1 + OR_TRUE;
                    }
                    catch (Exception e)
                    {
                        Proc.writeToLog(e);
                        writeToLogWarn("" + e);
                    }
                }
                return OR_FALSE;
            }
            return OR_FALSE;
        }

        protected override Unifiable ProcessChange()
        {
            return Unifiable.Empty;
        }
    }
}
