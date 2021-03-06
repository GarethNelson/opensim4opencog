using System;
using System.Threading;
using OpenMetaverse;

namespace Cogbot.World
{
    public class SimObjectUsage : SimUsage
    {
        public SimTypeUsage TypeUsage;
        public SimObject Target { get; set;}

        
        override public FirstOrderTerm GetTerm()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            String verb = TypeUsage.TextName;
            if (String.IsNullOrEmpty(verb))
                verb = TypeUsage.UsageName;

            return verb + " " + Target;
        }

        public SimObjectUsage(SimTypeUsage use, SimObject target) : base (""+ use.ToString() + " " + target.ToString())
        {
            TypeUsage = use;
            Target = target;
        }

        public void InvokeReal(SimActor TheBot)
        {
   
            String use = TypeUsage.UsageName;

            // Create the Side-Effect closure
            ThreadStart closure = new ThreadStart(delegate()
                                                      {
                                                          InvokeBotSideEffect(TheBot);
                                                      });

            SimAssetStore simAssetSystem = SimAssetStore.TheStore;
            bool animFound = TypeUsage.UseSit;
            // IF UseAnim was specified
            if (!String.IsNullOrEmpty(TypeUsage.UseAnim))
            {
                UUID animID = simAssetSystem.GetAssetUUID(TypeUsage.UseAnim, AssetType.Animation);
                if (animID != UUID.Zero)
                {
                    closure = TheBot.WithAnim(animID, closure);
                    animFound = true;
                }
            }
            // else
            if (!animFound)
            {
                //ELSE look for Verb coverage for an anim
                UUID animID = simAssetSystem.GetAssetUUID(use, AssetType.Animation);
                if (animID != UUID.Zero)
                    closure = TheBot.WithAnim(animID, closure);
            }

            // Surround with tough/grab if needed
            if (use == "touch" || use == "grab" || TypeUsage.UseGrab)
                closure = TheBot.WithGrabAt(Target, closure);

            // Surround with Sit if needed
            if (use == "sit" || TypeUsage.UseSit)
                closure = TheBot.WithSitOn(Target, closure);


            // Approach Target
            try
            {
                double maximumDistance = TypeUsage.maximumDistance + Target.GetSizeDistance();
                double howClose = TheBot.Approach(Target, maximumDistance - 0.5);
                ((SimAvatarImpl)TheBot).ApproachVector3D = Target.GlobalPosition;
                TheBot.TurnToward(Target);
                if (howClose > maximumDistance + 1)
                {
                    Debug(TheBot, "Too far away " + howClose + " from " + this);
                    return;
                }
                Target.PathFinding.MakeEnterable(TheBot);
                closure.Invoke();
                //if (Target == TheBot.ApproachPosition)
               // {
                 //   ((SimAvatarImpl) TheBot).ApproachPosition = null;
                 //   TheBot.StopMoving();
                //}
            }
            finally
            {
                Target.PathFinding.RestoreEnterable(TheBot);
            }
        }

        public virtual void InvokeBotSideEffect(SimAvatar TheBot)
        {
            Debug(TheBot, ToString());
            //User.ApplyUpdate(use, simObject);
            BotNeeds CurrentNeeds = (BotNeeds)TheBot["CurrentNeeds"];
            if (CurrentNeeds == null)
            {
                TheBot["CurrentNeeds"] = CurrentNeeds = new BotNeeds(90.0f);   
            }                
            BotNeeds needsBefore = CurrentNeeds.Copy();
            BotNeeds update = Target.Affordances.GetActualUpdate(TypeUsage.UsageName);
            //TODO rate interaction and update TheBot.Assumptions
            CurrentNeeds.AddFrom(update);
            CurrentNeeds.SetRange(0.0F, 100.0F);
            BotNeeds difNeeds = CurrentNeeds.Minus(needsBefore);
            Debug(TheBot,TheBot + " " + ToString() + "\n\t " +
                         TheBot.DistanceVectorString(Target)
                         + "=> " + difNeeds.ShowNonZeroNeeds());
            if (TheBot is SimActor) ((SimActor)TheBot).ExecuteLisp(this, TypeUsage);
            Thread.Sleep(TypeUsage.totalTimeMS);
        }

        private void Debug(SimAvatar TheBot,string theBotDebug)
        {
            TheBot.Debug(theBotDebug);
            //Radegast.RadegastInstance C = TheBot.GetGridClient().TheRadegastInstance;
            //if (C != null)
            //{
            //    C.TabConsole.DisplayNotificationInChat(theBotDebug);
            //}
        }



        public BotNeeds GetProposedChange()
        {
            return Target.Affordances.GetProposedUpdate(TypeUsage.UsageName);
        }

        public Vector3 GetUsePosition()
        {
            return Target.SimPosition;
        }

        public override float RateIt(BotNeeds simAvatar)
        {
            return GetProposedChange().TotalSideEffect(simAvatar);
        }
    }
}