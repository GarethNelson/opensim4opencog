using System;
using System.Runtime;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using AltAIMLbot;
using AltAIMLbot.Utils;
using AltAIMLParser;
using RTParser;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    public class response_topic : RTParser.Utils.AIMLTagHandlerU
    {

        public response_topic(RTParser.AltBot bot,
                User user,
                SubQuery query,
                Request request,
                Result result,
                XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }



        protected override Unifiable ProcessChangeU()
        {

            if (CheckNode("responsetopic"))
            {
                try
                {
                    var varMSM = this.botActionMSM;
                    string payload = templateNodeInnerText.ToValue(query);
                    string payload2 = Recurse();
                    string payload3 = InnerXmlText(templateNode);
                    string machine = GetAttribValue("machine",  varMSM.lastDefMachine);
                    string myState = GetAttribValue("state", varMSM.lastDefState);
                    string myTopic = GetAttribValue("topic", null);
                    string prob_str = GetAttribValue("prob", "0.1");
                    double prob = double.Parse(prob_str);
                    payload= payload.Replace("rcategory", "category");
                    payload= payload.Replace("rpattern", "pattern");
                    string responseCode = "<aiml graph=\"msm\"> <topic name=\"" + myTopic + "\"> " + payload + " </topic> </aiml>";
                    AltBot.writeDebugLine("MSM: response_topic ResponseCode = {0}", responseCode);
                    MachineSideEffect(() => varMSM.addResponse(machine, myState, myTopic, prob));
                    // TODO: define machine-state -> topic
                    AddSideEffect("Add AIML " + responseCode, () => TargetBot.AddAiml(responseCode));

                }
                catch (Exception e)
                {
                    writeToLogWarn("MSMWARN: " + e);
                }

            }
            return Unifiable.Empty;

        }
    }
}