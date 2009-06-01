using System;
using System.Text;
using System.Xml;

namespace RTParser.Utils
{
    /// <summary>
    /// The template for all classes that handle the AIML tags found within template nodes of a
    /// category.
    /// </summary>
    abstract public class AIMLTagHandler : TextTransformer
    {

        protected Unifiable templateNodeInnerText
        {
            get { return templateNode.InnerText; }
            set { templateNode.InnerText = value; }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request itself</param>
        /// <param name="result">The result to be passed back to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public AIMLTagHandler(RTParser.RTPBot bot,
                                    RTParser.User user,
                                    RTParser.Utils.SubQuery query,
                                    RTParser.Request request,
                                    RTParser.Result result,
                                    XmlNode templateNode)
            : base(bot, templateNode.OuterXml)
        {
            this.user = user;
            this.query = query;
            this.request = request;
            this.result = result;
            this.templateNode = templateNode;
            this.templateNode.Attributes.RemoveNamedItem("xmlns");
        }

        /// <summary>
        /// Default ctor to use when late binding
        /// </summary>
        public AIMLTagHandler()
        {
        }

        /// <summary>
        /// A flag to denote if inner tags are to be processed recursively before processing this tag
        /// </summary>
        public bool isRecursive = true;

        /// <summary>
        /// A representation of the user who made the request
        /// </summary>
        public RTParser.User user;

        /// <summary>
        /// The query that produced this node containing the wildcard matches
        /// </summary>
        public RTParser.Utils.SubQuery query;

        /// <summary>
        /// A representation of the input into the Proc made by the user
        /// </summary>
        public RTParser.Request request;

        /// <summary>
        /// A representation of the result to be returned to the user
        /// </summary>
        public RTParser.Result result;

        /// <summary>
        /// The template node to be processed by the class
        /// </summary>
        public XmlNode templateNode;
        protected Unifiable Recurse()
        {
            Unifiable templateResult = new Unifiable();
            if (this.templateNode.HasChildNodes)
            {
                // recursively check
                foreach (XmlNode childNode in this.templateNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Text)
                    {
                        templateResult.Append(childNode.InnerXml);
                    }
                    else
                    {
                        Unifiable found = Proc.processNode(childNode, query, request, result, user);
                        if (Unifiable.IsNull(found) || found.Trim() == "" || found.Trim() == "NIL")
                        {
                            return Unifiable.Empty;
                        }
                        templateResult.Append(found);
                    }
                }
                templateNodeInnerText = templateResult;//.ToString();
                return templateNodeInnerText;
            }
            else
            {
                Unifiable before = this.templateNode.InnerXml;               
                return before;                
            }

        }


        #region Helper methods

        /// <summary>
        /// Helper method that turns the passed Unifiable into an XML node
        /// </summary>
        /// <param name="outerXML">the Unifiable to XMLize</param>
        /// <returns>The XML node</returns>
        public static XmlNode getNode(string outerXML)
        {
            XmlDocument temp = new XmlDocument();
            temp.LoadXml(outerXML);
            return temp.FirstChild;
        }

        #endregion

        protected Unifiable GetAttribValue(string attribName,Unifiable defaultIfEmpty)
        {
            attribName = attribName.ToLower();
            foreach (XmlAttribute attrib in this.templateNode.Attributes)
            {
                if (attrib.Name.ToLower() == attribName) return attrib.Value;
            }
            return defaultIfEmpty;
        }
    }
}
