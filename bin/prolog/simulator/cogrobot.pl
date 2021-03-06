%------------------------------------------------------------------------------
%
%  cogrobot.pl
%
%     Module for use of _cogbot_ in SecondLife!!!
%
% _cogbot_ is usually in this mode
% set_prolog_flag(double_quotes,string).
%
%------------------------------------------------------------------------------
:-module(cogrobot,
  [
   run_sl/0,
   gridclient_ref/1,
   world_ref/1, world_get/2, client_manager_ref/1,
   current_bot/1, botget/2,
   botcall/1, botcall/2,
   pcall/1,

   botdo/1,wbotdo/2,wabdo/1,

   botcmd/1, botcmd/2, botcmd/3,   
   wbotcmd/2, wbotcmd/3, wbotcmd/4,
   to_avatar/2,
   grid_object/1, world_avatar/1, world_object/1,
   grid_asset/1, grid_account/1,
   simAvDistance/3, 
   name_to_location_ref/2,
   vectorAdd/3,
   distance_to/2,
   position_to_v3d/2,
   position_to_v3/2,
   %%on_sim_event/3,  %% uses user:* wrapper
   sim_event_db/3,
   obj2Npl/2,
   npl2Obj/2,
   create_write_hook/2,
   create_write_hook/1,
   uuid_to_cli_image/2,
   uuid_to_image_parts/2,
   request_texture/1,
   object_color/2,
   set_current_bot/1,
   unset_current_bot/1,
   %%robot_to_str/2,   %% uses user:* wrapper
   cmdargs_to_atomstr/2,
   set_bot_writeln_delegate/1,
   bot_writeln_delegate/1,
   prolog_in_thread/3,
   current_botname/1,
   logon_bot/6,
   create_bot/6,
   wbot_inventory/3,
   inventory_node_name/2
   ]).


:-set_prolog_flag(double_quotes,string).
:-at_initialization(set_prolog_flag(double_quotes,string)).

% let bv module see into use_module/1 caller

%% app_init(+Call)
% On app load run Call
app_init(Call):-term_to_atom(Call,Atom),atom_concat(Atom,'_done',Did),dynamic(arestore:Did),app_init_call(app_init(arestore:Did,Call)).
app_init_call(Call):-at_initialization(Call),Call.
app_init(Did,_Call):-Did,!.
app_init(Did,Call):-assert(Did),!,Call.

btrace:-attach_console,trace.

%%:- absolute_file_name('.',X),asserta(prev_dir6(X)),listing(prev_dir6).

%%:-source_location(File,_Line),file_directory_name(File, Directory),cd(Directory).


assert_once(Gaf):-catch(call(Gaf),_,fail),!.
assert_once(Gaf):-assert(Gaf).

:- assert_once(user:file_search_path(foreign, '.')).
:- assert_once(user:file_search_path(jpl_examples, 'examples/prolog')).
:- assert_once(user:file_search_path(jar, '.')).
:- assert_once(user:file_search_path(library, '.')).
:- assert_once(user:file_search_path(library, '..')).
:- assert_once(user:file_search_path(library, '../..')).
:- assert_once(user:file_search_path(library, '../../test')).
:- assert_once(user:file_search_path(test, '../test')).

%%:- use_module(library(testsupport)).
:-use_module(library(swicli)).


%:-app_init(cli_load_assembly('AForge.Imaging.dll')).
%:-app_init(cli_load_assembly('AForge.Imaging.Formats.dll')).

% needed for botvars
:-app_init(cli_load_assembly('PrologBotModule')).
%------------------------------------------------------------------------------

% % load needed modules

:-use_module(library(swicli)).
%%:-use_module(library(jpl)).

%------------------------------------------------------------------------------

% % load the cogbot assembly
:-dynamic(loaded_cogbot_assembly/0).
load_cogbot_assembly:-loaded_cogbot_assembly,!.
load_cogbot_assembly:-assert(loaded_cogbot_assembly),current_prolog_flag(address_bits,32) -> cli_load_assembly('Cogbot32.exe') ; cli_load_assembly('Cogbot.exe').
:-app_init(load_cogbot_assembly).

:-cli_set('Cogbot.ClientManagerConfig','DoNotCreateBotClientsFromBotConfig','@'(true)).

% % cache the type names
% prevents us having to use long names for things like SimAvatar
%
cache_cogbot_types:-
  cli_members('Cogbot.World.SimAvatar',_),
  cli_members('Cogbot.WorldObjects',_),
  cli_members('OpenMetaverse.Primitive',_).

:-app_init(cache_cogbot_types).
%------------------------------------------------------------------------------
% some type layout conversions (to make cleaner code)
%
%  Layouts are records - it's a field layout
%  cli_add_layout is a way to register an automagic conversion type
%  cli_add_layout adds a conversion between C# type and Prolog type
%------------------------------------------------------------------------------

add_layouts:-
  cli_add_layout('Vector3',v3(x,y,z)),
  cli_add_layout('Vector3d',v3d('X','Y','Z')),
  cli_add_layout('Vector4',v4('X','Y','Z','W')),
  cli_add_layout('Quaternion',quat('X','Y','Z','W')),
 %%  cli_add_layout('UUID',uuid('_guid')),
 cli_add_layout('UUID',uuid('ToString'),'Parse'),
 %%  cli_add_layout('Guid',guid(string)),
  !.

:-app_init(add_layouts).

%------------------------------------------------------------------------------
% much code uses current_bot(Me) like botcmd and say/n  
%
%  and is a thread_local predicate if no bot has been registered by thread its the last created bot
%  
% usage:
%  set_current_bot(Me), .....  unset_current_bot(Me)
%
% bug prone antipattern, but it's supported:
%  current_bot(OldBot), set_current_bot(Me) .....   set_current_bot(OldBot)
% though current_bot(OldBot) may throw if not bot is set
% ------------------------------------------------------------------------------
%

throw_on_nobj(Var,_Why):-cli_is_object(Var),!.
throw_on_nobj(Var,Why):-cogbot_throw(throw_on_nobj(Var,Why)).

:-dynamic current_bot_db/2.
current_bot_nt(BotID):-thread_self(TID),current_bot_db(TID,BotID),!.
current_bot_nt(BotID):-client_manager_ref(Man),cli_get(Man,'LastBotClient',BotID).

current_bot(BotID):-current_bot_nt(BotID),throw_on_nobj(BotID,current_bot/1).

set_current_bot(BotID):-thread_self(TID),retractall(current_bot_db(TID,_)),asserta(current_bot_db(TID,BotID)).

unset_current_bot(BotID):-thread_self(TID),current_bot_db(TID,OLD), 
    (OLD=BotID -> retract(current_bot_db(TID,OLD)) ; cogbot_throw(unset_current_bot(tid(TID),used(BotID),expected(OLD)))).

current_botname(Name) :- botcall('GetName',X),string_to_atom(X,Name).
wbotname(BotID,Name) :- wbotcall(BotID,'GetName',X),string_to_atom(X,Name).
%------------------------------------------------------------------------------
% throws cogbot based exceptions
% PRIVATE
% ------------------------------------------------------------------------------
cogbot_throw(Error):-throw(cogbot_user_error(Error)).

%------------------------------------------------------------------------------
% syncronously log a bot onto simulator and set the current_bot/1
% PUBLIC    
% TODO - should we remove the return result since we set current bot?
% ------------------------------------------------------------------------------
logon_bot(First, Last, Password, Loginuri, Location, BotID):-
        create_bot(First, Last, Password, Loginuri, Location, BotID),
        set_current_bot(BotID),
	cli_call(BotID,'LoginBlocked',_).

rescope_modules.

ahook_bot_created(BotID):-rescope_modules,forall(bv:hook_bot_created(BotID),true).

ahook_bot_loggedin(BotID):-rescope_modules,forall(catch(bv:hook_bot_loggedin(BotID),_,true),true).

ahook_bot_event(BotID,B,C):-rescope_modules,forall(catch(bv:hook_bot_event(BotID,B,C),_,true),true).

%------------------------------------------------------------------------------
% create a botclient (will call startups (like botconfig.xml) but no call to implicit login)
% PUBLIC
% ------------------------------------------------------------------------------
create_bot(First, Last, Password, Loginuri, Location, BotID):-        
	client_manager_ref(CM),
        %% DoNotCreateBotClientsFromLispScript
	cli_call(CM,'CreateBotClientNonScript'(First, Last, Password, Loginuri, Location), BotID),
        asserta(bot_client_db(First, Last, Password, Loginuri, Location, BotID)),
      %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
      % register our examples
      %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
      wbot_addvars(BotID,'@'(null), ahook_botvar_get, ahook_botvar_set, ahook_botvar_key).



%------------------------------------------------------------------------------
% System shutdowns
% ------------------------------------------------------------------------------
:-dynamic(bot_client_db/6).

ignore_caught(Call):-ignore(catch(Call,_,true)).


kill_pl_threads:-thread_property(ID,status(running)),ID\=main,thread_signal(ID,thread_exit(true)),fail.
kill_pl_threads:-cli_halt(0).

app_quit:-write('logoutbots\n'),flush_output,client_manager_ref(CM),cli_call(CM,'Quit',_),kill_pl_threads.
%%logoutBots:-bot_client_db(_First, _Last, _Password, _Loginuri, _Location, BotID),ignore_caught(cli_call(BotID,'Dispose',_)),fail.
%%logoutBots:-ignore_caught((client_manager_ref(CM),cli_call(CM,'ShutDown',_))),cli_call('System.Environment','Exit'(0),_).

:-at_halt(app_quit).

%------------------------------------------------------------------------------
% Refernce to the scene is world_ref
%------------------------------------------------------------------------------
world_ref(Sys):-cli_get('Cogbot.WorldObjects','GridMaster',Sys).

% gets some property of the GridMaster
%  world_get(+Field, -Value)
%
world_get(Field,Value):-world_ref(Sys),cli_get(Sys,Field,Value).

%% grid_object(?Ele).
% get each SimObject derezed or not
% this is every primitive, linked or not, as a complex term
% it's a partially marshalled object from the simulator
% cli_col iterates thru elements
grid_object(Ele):-world_get('SimObjects',Objs),cli_col(Objs,Ele),not(cli_is_type(Ele,'SimAvatar')).

%------------------------------------------------------------------------------
% ways of iterating in world objects
%------------------------------------------------------------------------------

%% world_object(?Ele).
% get each SimObject in world except attachments
world_object(Ele):-grid_object(Ele),cli_get(Ele,isattachment,@(false)).

%% world_root_object(?Ele).
% get each SimObject in world except child objects
world_root_object(Ele):-world_get('SimRootObjects',Objs),cli_col(Objs,Ele).

%% world_avatar(Ele)
% this is the set of av's that are known to the simulator, they are only actually present if they have a prim
world_avatar(Ele):-grid_account(Ele),cli_get(Ele,hasprim,@(true)).

%% grid_account(?Ele)
% a grid_account/1 is like world_avatar (they are avatars known about in system.  including friends not logged in)
grid_account(Ele):-world_get('SimAvatars',Objs),cli_col(Objs,Ele).

%% grid_region(?Ele)
% returns all regions known to cogbot
grid_region(Ele):-cli_get('Cogbot.World.SimRegion','CurrentRegions',Objs),cli_col(Objs,Ele).

%% grid_parcels(?Ele)
%  returns all parcels known to cogbot
grid_parcels(Ele):-grid_region(Sim),cli_get(Sim,parcels,Objs),cli_col(Objs,Ele).

%% client_manager_ref(-SingleInstance)
% get the client_manager_ref/1 Instance
%  A class that holds all the static singletons
%  A Client is a logged on acct in this context
%
%  Clientmanager binds radegast to the Client
%  botconfig is run from Clientmanager
client_manager_ref(SingleInstance):-cli_get('Cogbot.ClientManager','SingleInstance',SingleInstance),!,throw_on_nobj(SingleInstance,client_manager_ref/1).


%% botget(+Property,-Value).
% given a property list returns value for the avatar
%
% walks down property tree
% botget([name,length,X)
% botget([name,length],X)
% botget([position,z],X)
%
% a.b.c.d
% botget([a.b.c.d],X).
% botget([a,b,c,d],X).
% botget([a,b,c,d],X). = mybot.a.b.c.d
% prolog botget([a,b,c,d],X). = c# object X = myBotClient.a.b.c.d
% ==
% botget(['Inventory','Store',rootfolder,name],Y).
% Y = "My Inventory".
%
% botget(['Inventory','Store',rootnode,nodes,values],Y),
%	findall(S,(cli_col(Y,Z),cli_to_str(Z,S)),L),writeq(L).
%	["Scripts","Photo Album","*MD* Brown Leather Hat w/Bling",
%	"Body Parts","Notecards","Objects","Clothing","Landmarks","Textures",
%     "Gestures","boxed fem_talk","Calling Cards","Animations","Sounds",
%	"Trash","Lost And Found"]
%       Y = @'C#720558400'
%
%       finds all grandchildren
%       botget(['Inventory','Store',rootnode,nodes,values],Y),
%	     findall(S,(cli_col(Y,Z),cli_get(Z,'children',GC),
%	     cli_collecton(GC,'children',GCReal),cli_to_str(GCReal,S)),L),writeq(L).
%

wbotget(BotID,Property,Value):-cli_get(BotID,Property,Value),!.

% a way to call a method on c#?
% cli_call('System.Console.Out',writeline("32"),Y).
% cli_call('System',printf(32),Y).



wbotcall(BotID,Call):-wbotcall(BotID,Call,Res),cli_writeln(Res).
wbotcall(BotID,[P|N],Value):-!,cli_get(BotID,P,Mid),cli_get(Mid,N,Value).
wbotcall(BotID,Property,Value):-cli_call(BotID,Property,Value).


% wrappered execute command in a convenience pred
% botCmd(say("hi"))
%
wbotdo(BotID,In):-wbotcmd(BotID,In,cli_fmt(botcmd),_).

wabdo(In):-current_bot_db(_,BotID),wbotdo(BotID,In),fail.
wabdo(_).

wabcall(Call):-once(((thread_self(TID),current_bot_db(TID,Save))->TODO=set_current_bot(Save),TODO=true)),
   forall(bot_client_db(_, _, _, _, _, BotID),(set_current_bot(BotID),Call)),TODO,!.

% wrappered execute command in a convenience pred
% botcmd(say("hi"))
%
wbotcmd(BotID,StrIn):-wbotcmd(BotID,StrIn,Out),cli_get(Out,success,@(true)),!.
wbotcmd(BotID,StrIn,Out):-cmdargs_to_atomstr(StrIn,Str),wbotcmd(BotID,Str,{pluggable_callback(botcmd)},Out),!.

:-meta_predicate(wbotcmd(+,+,//,?)).
:-meta_predicate(botcmd(+,//,?)).
wbotcmd(BotID,StrIn,WriteDelegate,Out):-cmdargs_to_atomstr(StrIn,Str),cli_call(BotID,executeCommand(Str,BotID,WriteDelegate,'@'(true)),Out),!.


% wrappered execute command in a convenience pred
% cmdargs_to_atomstr(say("hi"),Out)
%
cmdargs_to_atomstr(C,C):-var(C),!,cogbot_throw(instantiation_error(command(C))).
cmdargs_to_atomstr(C,C):-string(C),!.
cmdargs_to_atomstr(S,Out):-string_rep(S),!,string_rep_to_atom(S,A),quoteAtom(A,Out).
cmdargs_to_atomstr([C|Cmd],Out):-toStringableArgs([C|Cmd],SCmd),!,concat_atom(SCmd,' ',Str),cmdargs_to_atomstr(Str,Out).
%cmdargs_to_atomstr(C,Out):-compound(C),C=..[F,A|B],is_movement_proc(F),\+ is_vector(A),\+ cli_is_type(A,'SimPosition'),
%    name_to_location_ref(A,AA),cli_get(AA,'ID',AAA),!,CC=..[F,AAA|B],cmdargs_to_atomstr(CC,Out).
cmdargs_to_atomstr(C,Out):-compound(C),!,C=..[F|A],listifyFlat([F|A],FL),cmdargs_to_atomstr(FL,Out).
cmdargs_to_atomstr(Str,Str):-!. %%toStringableArg(StrIn,Str).

is_movement_proc(astargoto).
is_movement_proc(moveto).
is_movement_proc(_):-fail.

is_vector(V3):-notrace((compound(V3),functor(V3,F,3),(F==v3;F==v3d))).

toStringableArgs(Var,Var):-var(Var),!.
toStringableArgs([C|Cmd],[A|Amd]):-toStringableArg(C,A),toStringableArgs(Cmd,Amd),!.
toStringableArgs(CCmd,CCmd).

toStringableArg(Var,Var):-var(Var),!,throw(toStringableArgVar(Var)).
toStringableArg(v3d(X,Y,Z),A):-concat_atom([X,Y,Z],'/',A).
toStringableArg(uuid(ID),ID):-!.
toStringableArg(v3(X,Y,Z),A):-concat_atom([X,Y,Z],'/',A).
toStringableArg(S,Out):-string_rep(S),!,string_rep_to_atom(S,A),quoteAtom(A,Out),!.
toStringableArg('@'(OBJ),Out):-cli_is_type('@'(OBJ),'SimObject'),!,cli_get('@'(OBJ),id,uuid(Out)).
toStringableArg(S,S):-atomic(S),!.
toStringableArg(Var,Var).

quoteAtom(A,A):-atom(A),atom_concat('"',_,A),!.
quoteAtom(A,Out):-atom(A),!,concat_atom(['"',A,'"'],'',Out).

string_rep_to_atom(S,A):-catch(string_to_atom(S,A),_,fail),!.
string_rep_to_atom(S,A):-catch(atom_concat(S,' ',A),_,fail),!.

string_rep(Var):-var(Var),!,fail.
string_rep(S):-string(S),!.
string_rep([S|R]):-integer(S),string_rep_code(R),!.
string_rep([S|R]):-atom_char(S),string_rep_char(R).

string_rep_code([]).
string_rep_code([S|R]):-integer(S),string_rep_code(R).

atom_char(S):-atom(S),atom_length(S,1).

string_rep_char([]).
string_rep_char([S|R]):-atom_char(S),string_rep_char(R).

char_or_code(C):-integer(C).

% helper pred for botcmd
listifyFlat([],[]):-!.
listifyFlat(S,[S]):-string_rep(S),!.
listifyFlat([H|T],HT):-!,listifyFlat(H,HL),listifyFlat(T,TL),!,append(HL,TL,HT).
listifyFlat(C,FA):-functor(C,F,1),!,C=..[F,A],!,listifyFlat(A,FA).
listifyFlat(v3d(X,Y,Z),[v3d(X,Y,Z)]).
listifyFlat(v3(X,Y,Z),[v3(X,Y,Z)]).
listifyFlat(C,FA):-compound(C),!,C=..[F|A],!,listifyFlat([F|A],FA).
listifyFlat(C,[C]).

%% gridclient_ref(+Obj)
% get the GridClient Instance 
% libOMV's version of gridclient_ref/1, in case you want the direct one
gridclient_ref(Obj):-current_bot(BC),cli_get(BC,'gridClient',Obj).


%------------------------------------------------------------------------------
% create a writeline delegate
%------------------------------------------------------------------------------
create_write_hook(WID):-create_write_hook({pluggable_callback(cogrobot)},WID).
create_write_hook(WriteDelegate,WID):-cli_new_delegate('MushDLR223.ScriptEngines.OutputDelegate',WriteDelegate,WID).

null_callback(_,_,_).

:-dynamic(bot_writeln_delegate/1).

bot_writeln_delegate(null_callback).

user:pluggable_callback(A,B,C):-cogrobot:pluggable_callback(A,B,C).
pluggable_callback(A,B,C):-bot_writeln_delegate(Pred),call(Pred,A,B,C).


%------------------------------------------------------------------------------
% set a default a writeline delegate
%------------------------------------------------------------------------------
set_bot_writeln_delegate(Pred/3):-!,set_bot_writeln_delegate(Pred).
set_bot_writeln_delegate(Pred):-retractall(bot_writeln_delegate(_)),assert(bot_writeln_delegate(Pred)).


%------------------------------------------------------------------------------
% event handler functions
%------------------------------------------------------------------------------
robot_to_str(C,C):-var(C).
robot_to_str([],[]).
robot_to_str([A|B],[AA|BB]):-robot_to_str(A,AA),robot_to_str(B,BB).
robot_to_str(Obj,array(ArrayS)):-Obj='@'(_O), cli_is_type(Obj,'System.Array'),cli_array_to_termlist(Obj,Array),!,robot_to_str(Array,ArrayS).
robot_to_str(Obj,list(ArrayS)):-Obj='@'(_O), cli_is_type(Obj,'System.Collections.Generic.IList'('MushDLR223.ScriptEngines.NamedParam')),cli_call(Obj,'ToArray',[],Array),robot_to_str(Array,ArrayS).
robot_to_str(Obj,enumr(ArrayS)):-Obj='@'(_O), cli_is_type(Obj,'System.Collections.IEnumerable'),cli_array_to_termlist(Obj,Array),robot_to_str(Array,ArrayS).
robot_to_str(C,AS):-compound(C),C=..[F|Args],not(member(F,['@'])),robot_to_str(Args,ArgS),AS=..[F|ArgS].
robot_to_str(C,AS):-cli_to_str(C,AS).

user:robot_to_str(A,B):-cogrobot:robot_to_str(A,B).


nop(_).


% print some events
:-dynamic(sim_event_db/3).
on_sim_event(_A,B,C):-contains_var("On-Log-Message",a(B,C)),!.
on_sim_event(_A,B,C):-contains_var('DATA_UPDATE',a(B,C)),!.
on_sim_event(A,B,C):-once(ahook_bot_event(A,B,C)),fail.
on_sim_event(A,B,C):-contains_var("On-Login-Success",a(B,C)),once(ahook_bot_loggedin(A)),fail.
on_sim_event(_A,_B,_C):-!. % comment out this first line to print them
on_sim_event(A,B,C):-!,nop(assertz(sim_event_db(A,B,C))),!,robot_to_str(C,AS),!,writeq(on_sim_event(AS)),nl.

user:on_sim_event(A,B,C):-cogrobot:on_sim_event(A,B,C).

% trim_sim_events(NumToLeave):- predicate_property(sim_event_db(_,_,_),number_of_clauses(N)),Remove is N-Num, (Remove<=0->true;( ... )).
trim_sim_events(Num):- predicate_property(sim_event_db(_,_,_),number_of_clauses(N)),Num>=N,!.
trim_sim_events(Num):- retract(sim_event_db(_,_,_)),predicate_property(sim_event_db(_,_,_),number_of_clauses(N)),Num >= N,!.
trim_sim_events(_Num).

prolog_in_thread(Named,_,_):-thread_property(N,_),N==Named,!.
prolog_in_thread(Named,Goal,Options):-thread_create(Goal,_,[alias(Named)|Options]).

% Every minute trim EventLog to 1000 entries
keep_1000_events:-repeat,sleep(60),trim_sim_events(1000),fail.
:-app_init(prolog_in_thread(keep_1000_events,keep_1000_events,[detached(true)])).

%%:-module_transparent(first_bot_client_hook/2).

% on first bot Client created register the global event handler
user:first_bot_client_hook(A,B):- %%%attach_console,trace,
 current_bot(Obj),
  % uncomment the next line if you want all commands to run thru the universal event handler
   cli_add_event_handler(Obj,'EachSimEvent',on_sim_event(_,_,_)),
   cli_to_str(first_bot_client_hook(A-B-Obj),Objs),writeq(Objs),nl,
   ahook_bot_created(Obj).

% register first_bot_client_hook
register_on_first_bot_client:- cli_add_event_handler('Cogbot.ClientManager','BotClientCreated',first_bot_client_hook(_,_)).
:-app_init(register_on_first_bot_client).

%------------------------------------------------------------------------------
% start Radegast!
%------------------------------------------------------------------------------
:-dynamic(ran_sl).

run_sl:-current_bot_nt(BotID),cli_is_object(BotID),wbotcmd(BotID,show_gui),!.
run_sl:-ran_sl,!.
% this is so you can reconsult this file without restarting radegast
run_sl:-asserta(ran_sl),!,
   cli_set('MushDLR223.Utilities.DLRConsole','NoConsoleVisible','@'(true)),
   cli_set('ABuildStartup.Program','UseApplicationExit','@'(false)),
   cli_set('Cogbot.ClientManagerConfig','noGUI','@'(true)),
   cli_set('Cogbot.ClientManagerConfig','DoNotCreateBotClientsFromBotConfig','@'(true)),   
   cli_call('ABuildStartup.Program','Main',[],_).

% assert_once is assert a new grounded atomic fact only if the predicate
% was previously undefined

%:-retractall(cli_subproperty(_,_)).
:-assert_once(swicli:cli_subproperty('Cogbot.World.SimAvatar','ProfileProperties')).
:-assert_once(swicli:cli_subproperty('Cogbot.World.SimAvatar','AvatarInterests')).
:-assert_once(swicli:cli_subproperty('Cogbot.World.SimAvatar','FriendshipInfo')).
:-assert_once(swicli:cli_subproperty('Cogbot.World.SimObject','Prim')).
:-assert_once(swicli:cli_subproperty('Cogbot.World.SimObject','Properties')).


obj2Npl(O,npl(66,O)).
npl2Obj(npl(66,O),O).

registerNamedParamRecomposer:-!.
registerNamedParamRecomposer:-cli_add_recomposer('System.Collections.Generic.IList'('MushDLR223.ScriptEngines.NamedParam'),'npl'(_,_),obj2Npl,npl2Obj).

:-app_init(registerNamedParamRecomposer).

%------------------------------------------------------------------------------
% CLR Introspection of event handlers
%------------------------------------------------------------------------------
grid_asset(Asset):-  cli_get('Cogbot.World.SimAssetStore','SimAssets',Assets),cli_col(Assets,Asset).


gridCliientEvents(E):-cli_memb('OpenMetaverse.GridClient',f,M),arg(3,M,Type),cli_memb(Type,e,E).


listMembs:-cli_new('System.Collections.Generic.List'(string),[int],[10],O),cli_members(O,M),member(E,M),writeq(E),nl,fail.
listMembs:-gridCliientEvents(E),writeq(E),nl,fail.
listMembs. % so pred doesnt fail

%%:-listMembs.

% coerces anything to avatar object
to_avatar(Name,Name):-cli_is_object(Name),cli_is_type(Name,'SimAvatar'),!.
to_avatar(Name,Object):-cli_is_object(Name),cli_to_str(Name,String),!,to_avatar(String,Object).
to_avatar(Name,Object):-cli_call('Cogbot.WorldObjects','GetSimAvatarFromNameIfKnown'(string),[Name],Object).

% name_to_location_ref(start_hill_walk,O),object_color(O,C),cli_writeln(C).
% cli_call(static('Cogbot.World.SimImageUtils'),'ToNamedColors'('OpenMetaverse.Color4'),[struct('Color4',1,0,1,0)],Named),cli_col(Named,NamedE),cli_writeln(NamedE).
object_color(A,NamedE):-grid_object(A),cli_call(static('Cogbot.World.SimImageUtils'),'ToNamedColors'('Cogbot.World.SimObject'),[A],Named),cli_col(Named,NamedE).
object_color(A,NamedE):-fail,grid_object(A),cli_get(A,textures,B),cli_get(B,faceTextures,C),cli_col(C,E),E\=='@'(null),cli_get(E,rgba,CC),
  cli_call(static('Cogbot.World.SimImageUtils'),'ToNamedColors'('OpenMetaverse.Color4'),[CC],Named),cli_col(Named,NamedE).

/*

38 ?- grid_asset(A),cli_get(A,assetType, enum('AssetType', 'Texture')),cli_get(A,id,UUID),cli_get(A,assetData,Data),Data\=='@'(null), cli_call('OpenMetaverse.Imaging.OpenJPEG','DecodeToImage'(Data,O1,O2),_),cli_get_type(O2,T),cli_writeln(T).
"System.D_rawing.Bitmap"
A = @'C#664147632',
UUID = uuid("38b86f85-2575-52a9-a531-23108d8da837"),
Data = @'C#664150288',
O1 = @'C#664150368',
O2 = @'C#664150128',
T = @'C#664150520' .

*/

%%b2img('@'(null),'@'(null)):-!.
b2img(Data,Image):-Data\=='@'(null),cli_call('OpenMetaverse.Imaging.OpenJPEG','DecodeToImage'(Data,_O1,Image),_).

uuid_to_cli_image(UUID,Image):-nonvar(UUID),!,cli_call('Cogbot.WorldObjects',['GridMaster','TextureBytesForUUID'(UUID)],Bytes),b2img(Bytes,Image).
uuid_to_cli_image(UUID,Image):-var(UUID),!,grid_asset(A),cli_get(A,assetType, enum('AssetType', 'Texture')),cli_get(A,id,UUID),cli_get(A,assetData,Data),b2img(Data,Image).
uuid_to_image_parts(UUID,Part):-grid_asset(A),cli_get(A,assetType, enum('AssetType', 'Texture')),cli_get(A,id,UUID),cli_get(A,imageStats,Parts),cli_array_to_termlist(Parts,List),List=[_|_],member(Part,List).


request_texture(UUID):-world_ref(Sys),cli_call(Sys,'StartTextureDownload'(UUID),_O).

cache_objects(_Object,DB,_PROC):-call(DB),!.
cache_objects(Object,DB,PROC):-once(PROC),cli_is_object(Object)->asserta(DB);true.

:-dynamic name_to_location_ref_cache/2.

add_cache_pred(Spec):-assert(cache_pred_db(Spec)).
expire_caches:-cache_pred_db(Spec),retractall(Spec),fail.
expire_caches.
:-add_cache_pred(name_to_location_ref_cache(_,_)).

expire_caches_120:-repeat,sleep(120),expire_caches,fail.
:-app_init(prolog_in_thread(expire_caches_120,expire_caches_120,[detached(true)])).

name_to_location_ref(Object,Object):-cli_is_type(Object,'SimPosition'),!.
name_to_location_ref(Name,Object):-
   cache_objects(Object,name_to_location_ref_cache(Name,Object), cli_call('Cogbot.WorldObjects','GetSimPositionByName'(string),[Name],Object)).

sayTo(Speaker,ToWho,What):-to_avatar(ToWho,Listener),cli_call(Speaker,talkto('SimAvatar',string),[Listener,What],_O).

% gives you a list of all the properties on all grid objects
grid_object(X,OE):-grid_object(X),cli_get(X,infoMap,Y),cli_col(Y,PE),cli_unify(OE,PE).

:-set_prolog_flag(double_quotes,string).
%------------------------------------------------------------------------------
%------------------------------------------------------------------------------
simDistance(V1,V2,D):-cli_call(V1,distance(V1,V2),D).

simAvDistance(A,C,E):-var(A),nonvar(C),!,simAvDistance(C,A,E).
simAvDistance(A,C,E):-world_avatar(A),cli_get(A,globalposition,B),world_avatar(C),A\=C,cli_get(C,globalposition,D),simDistance(B,D,E).
simObjDistance(A,C,E):-var(A),nonvar(C),!,simObjDistance(C,A,E).

%------------------------------------------------------------------------------
%------------------------------------------------------------------------------


%------------------------------------------------------------------------------
% Position/Vectort calls
%
%------------------------------------------------------------------------------
vectorAdd(A1,A2,R):-cli_call(A1,add(A1,A2),R).

% already global vect!
position_to_v3d(Vect,Vect):-functor(Vect,v3d,3),!.
position_to_v3d(v3(A,B,C),Vect):-botget(['Network','CurrentSim','Handle'],S),cli_call('SimRegion','HandleLocalToGlobal'(S,v3(A,B,C)),Vect),!.
% ?- position_to_v3d('annies haven/129.044327/128.206070/81.519630',D).
position_to_v3d(A,Vect):-atom(A),concat_atom([R,X,Y,Z|_],'/',A),!,gridclient_ref(BC),cli_call('SimRegion','GetRegionByName'(R,BC),Reg),cli_call(Reg,'LocalToGlobal'(v3(X,Y,Z)),Vect).
% ?- position_to_v3d('129.044327/128.206070/81.519630',D).
position_to_v3d(A,Vect):-atom(A),concat_atom([X,Y,Z],'/',A),!,position_to_v3d(v3(X,Y,Z),Vect).
% ?- position_to_v3d('CyberPunk Buddha - L',D).
position_to_v3d(A,Vect):-atom(A),!,name_to_location_ref(A,Obj),cli_get(Obj,globalposition,Vect),!.
position_to_v3d(Obj,Vect):-cli_get(Obj,globalposition,Vect),!.

position_to_v3(Obj,LV):-position_to_v3d(Obj,Vect),cli_call('SimRegion','GlobalToLocalStatic'(Vect),LV).

%% deprecated
distance_to(A,R):-bot_distance_to(A,R).
% % get the distance of a primspec
wbot_distance_to(BotID,A,R):-position_to_v3d(A,A2),!,wbotget(BotID,['Self','GlobalPosition'],A1),cli_get(A1,'Z',Z),cli_set(A2,'Z',Z), cli_call(A2,distance(A1,A2),R).

% ?- bot_moveto('CyberPunk Buddha - L',4,FD).

wbot_moveto(BotID,Dest,Time,FDist):-wbotcmd(BotID,moveto(Dest)),wbotcmd(BotID,waitpos(Time,Dest)),wbotcmd(BotID,stopMoving),wbot_distance_to(BotID,Dest,FDist).

wbot_chat(BotID,Msg):-wbot_chat(BotID,Msg,0).
wbot_chat(BotID,Msg,Ch):-wbot_chat(BotID,Msg,Ch,'Normal').
wbot_chat(BotID,Msg,Ch,Type):-cli_call(BotID,talk(Msg,Ch,Type),_).

%%:-prev_dir6(X),cd(X).

%------------------------------------------------------------------------------
% ways of examining inventory
%------------------------------------------------------------------------------
% wbot_inventory(+BotID,?Path,?NodeDataRef).
% if just checking for items existence you  _  the third arg
% (the third arg is the Node contents)
%
% ?- current_bot(BotID),wbot_inventory(BotID,[A,'Clothing'],X).
% BotID = @'C#508280816',
% A = "My Inventory",
% X = @'C#598007568' ;
% BotID = @'C#508280816',
% A = "Library",
% X = @'C#598013968' ;
% false.
%
% ?- current_bot(BotID),wbot_inventory(BotID,[A,'Clothing',What],X).
% BotID = @'C#508280816',
% A = "Library",
% What = "Female Shape & Outfit",
% X = @'C#598060416' ;
% BotID = @'C#508280816',
% A = "Library",
% What = "Gamer Male",
% X = @'C#598062912' 

%------------------------------------------------------------------------------
% Start at 'My Inventory'
%------------------------------------------------------------------------------
wbot_my_inventory(BotID,[TopName|Path],Node):-
   wbotget(BotID,['Inventory','Store','RootNode'],StartNode),
   wbot_ensure_inventory(BotID,StartNode),
   cli_get(StartNode,[nodes,values],StartCol),
   cli_col(StartCol,Top),cli_get(Top,data,TopData),
   cli_get(TopData,name,TopName),
   inventory_children(Top,TopData,Path,Node).

%------------------------------------------------------------------------------
% Start at 'Library Folder'
%------------------------------------------------------------------------------
wbot_lib_inventory(BotID,[TopName|Path],Node):-
   wbotget(BotID,['Inventory','Store','LibraryRootNode'],StartNode),
   wbot_ensure_inventory(BotID,StartNode),
   cli_get(StartNode,[nodes,values],StartCol),
   cli_col(StartCol,Top),cli_get(Top,data,TopData),
   cli_get(TopData,name,TopName),
   inventory_children(Top,TopData,Path,Node).

%------------------------------------------------------------------------------
% Start at above 'Library Folder' and 'My Inventory'
%------------------------------------------------------------------------------
wbot_inventory(BotID,[StartName,TopName|Path],Node):-
 cli_notrace((
   lists:member(Start,['RootNode','LibraryRootNode']),   
   wbotget(BotID,['Inventory','Store',Start],StartNode),
   wbot_ensure_inventory(BotID,StartNode),
   cli_get(StartNode,[nodes,values],StartCol),
   cli_get(StartNode,[data,name],StartName),
   cli_col(StartCol,Top),cli_get(Top,data,TopData),
   cli_get(TopData,name,TopName),
   inventory_children(Top,TopData,Path,Node))).

inventory_children(_Top,TopData,[],NodeData):-cli_unify(NodeData,TopData).
inventory_children(Mid,_MidData,[TopName|Path],NodeData):-
   cli_get(Mid,[nodes,values],StartCol),
   cli_col(StartCol,Top),cli_get(Top,data,TopData),
   cli_get(TopData,name,TopName),
   inventory_children(Top,TopData,Path,NodeData).
      
inventory_node_name(Node,Name):-cli_get(Node,[data,name],Name),!.
inventory_node_name(_Node,'unk').

wbot_ensure_inventory(BotID,StartNode):-cli_get(BotID,['BotInventory'],Inv),cli_call(Inv,'TraverseNodes'(StartNode),_).
%------------------------------------------------------------------------------
% ways of testing in inventory
%------------------------------------------------------------------------------
wbot_has_inventory(BotID,Mask):-wbot_inv_absolute(BotID,Mask,_).
wbot_inv_absolute(BotID,Mask,Path):-wbot_inventory(BotID,Path,_),cli_sublist(Mask,Path).

%------------------------------------------------------------------------------
% Renaming an inventory item or folder
%------------------------------------------------------------------------------
wbot_inventory_rename(BotID,Mask,NewName):-wbot_find_node(BotID,Mask,Item),
    wbot_inv_parent(BotID,Item,Folder),
    wbotcall(BotID,['Inventory','Move'(Item,Folder,NewName)],_).

wbot_inv_parent(BotID,Mask,Folder):-wbot_find_node(BotID,Mask,Item),cli_call(Item,parent,Folder).
%------------------------------------------------------------------------------
% Moving an inventory item or folder
%------------------------------------------------------------------------------
wbot_inventory_move(BotID,Mask,NewParent):-wbot_find_node(BotID,Mask,Item),wbot_find_node(BotID,NewParent,NewFolder),
    inventory_node_name(Item,OldName),
    wbotcall(BotID,['Inventory','Move'(Item,NewFolder,OldName)],_).

%------------------------------------------------------------------------------
% ways of manipulating worn (not attached) items (cogbot will rebake w/in 20 seconds of outfit changes)
%------------------------------------------------------------------------------
wbot_is_wearable_item(_BotID,Item):-inv_type(Item,T),memberchk(T,['Object','Attachment','Wearable']).
wbot_is_attachable_item(_BotID,Item):-inv_type(Item,T),memberchk(T,['Object','Attachment']).

inv_type(Item,T):-cli_is_object(Item),cli_get(Item,'InventoryType',enum(_,T)).

% return clothing matching pathmask
wbot_is_wearable(BotID,Mask):-wbot_inventory(BotID,Path,Item),cli_sublist(Mask,Path),wbot_is_wearable_item(BotID,Item).
% return clothing matching pathmask
wbot_is_wearing(BotID,Mask):-wbot_inventory(BotID,Path,Item),cli_sublist(Mask,Path),wbot_is_worn(BotID,Item).
% remove clothing matching pathmask
wbot_unwear(BotID,Mask):-wbot_find_node(BotID,Mask,Item),wbotcall(BotID,[appearance,removefromoutfit(Item)],_).
% remove all clothing
wbot_unwearall(BotID):-forall(wbot_is_wearing(BotID,Path),wbot_unwear(BotID,Path)),wbot_rebake_appearance(BotID).
% wear clothing matching pathmask
wbot_wear(BotID,Mask):-wbot_find_node(BotID,Mask,Item),wbotcall(BotID,[appearance,addtooutfit(Item)],_).
% replace clothing using start path such as a folder
wbot_replaceoutfit(BotID,Mask):-
     wbot_path_to_absolute(BotID,Mask,StartPath),
     findall(ItemName,wbot_inventory(BotID,Path,_),append(StartPath,[ItemName],Path),Items),
     wbot_replaceoutfit(BotID,Path,Items),wbot_send_appearance(BotID).

%wbot_replaceoutfit(+BotID,+Mask,+Items).
% replace clothing using start path + items below
wbot_replaceoutfit(BotID,Mask,Items):-
            wbot_path_to_absolute(BotID,Mask,StartPath),!,
            findall(Item,((member(ItA,Items),string_to_atom(It,ItA),append(StartPath,[It],Path),wbot_inventory(BotID,Path,Item))),Refs),
            Refs=[_|_],
            cli_make_list(Refs,'OpenMetaverse.InventoryItem',List),wbotcall(BotID,[appearance,replaceoutfit(List)],_).

wbot_worn_where(BotID,Mask,Position):-wbot_find_node(BotID,Mask,Item),wbot_is_worn_item(BotID,Item),wbot_will_attach_to(BotID,Item,Position).

wbot_wear(BotID,Mask,Position):-wbot_find_node(BotID,Mask,Item),
      wbot_is_attachable_item(BotID,Item),!,wbotcall(BotID,[appearance,attach(Item,Position)],_).
wbot_wear(BotID,Mask,Position):-wbot_find_node(BotID,Mask,Item),wbot_will_attach_to(BotID,Item,Position),wbotcall(BotID,[appearance,addtooutfit(Item)],_).


wbot_is_worn(BotID,Mask):-wbot_find_node(BotID,Mask,Item),wbot_is_worn_item(BotID,Item).
wbot_is_worn_item(BotID,Item):-wbot_is_attachable_item(BotID,Item),!,wbot_inv_eval(BotID,'IsAttached'(Item),@(true)).
wbot_is_worn_item(BotID,Item):-wbot_is_wearable_item(BotID,Item),!,wbot_inv_eval(BotID,'IsWorn'(Item),@(true)).

wbot_inv_eval(BotID,Call,Res):-cli_get(BotID,['BotInventory'],Inv),cli_call(Inv,Call,Res).

wbot_path_to_absolute(BotID,Mask,Absolute):-wbot_inventory(BotID,Absolute,_),cli_sublist(Mask,Absolute).

wbot_find_node(_BotID,'@'(O),'@'(O)):-nonvar(O),!,cli_is_type('@'(O),'OpenMetaverse.InventoryBase'),!.
wbot_find_node(BotID,Mask,Item):-wbot_inventory(BotID,What,Item),cli_sublist(Mask,What).

wbot_will_attach_to(BotID,Wearable,Position):-wbot_find_node(BotID,Wearable,Item),wbot_inv_eval(BotID,'AttachesTo'(Item),enum(_,Position)).
%------------------------------------------------------------------------------
% ways of sending appearance and rebaking
%------------------------------------------------------------------------------
wbot_send_appearance(BotID):-wbotcall(BotID,['Appearance','RequestSetAppearance'],_).
wbot_rebake_appearance(BotID):-wbotcall(BotID,['Appearance','RequestSetAppearance'(@(true))],_).

%%object_children(OBject,Child):-wbot_inventory(BotID,Path,Item),cli_sublist(Mask,Path),wbot_is_worn(BotID,Item).

%%wbot_attachments(BotID,Attachment,Joint):-

wbot_sitting_on(BotID,What):-wbotget(BotID,['TheSimAvatar'],Self),cli_call(Self,'Parent',What),!,Self \= What.

%------------------------------------------------------------------------------
% sysvar interface
%------------------------------------------------------------------------------
wbot_get_sysvar(_BotID,Name,Value):-cli_get('MushDLR223.ScriptEngines.ScriptManager',['SysVars','Values'],Col),
   cli_col(Col,Var),get_modeless(Var,['Name'=Name,'Value'=Value]).
wbot_set_sysvar(_BotID,Name,Value):-cli_get('MushDLR223.ScriptEngines.ScriptManager',['SysVars','Values'],Col),
   cli_col(Col,Var),cli_get(Var,'Key',Name),set_modeless(Var,['Value'=Value]).

get_modeless(Var,List):-forall(member(Member=Value,List),cli_get(Var,Member,Value)).

set_modeless(Var,List):-forall(member(Member=Value,List),cli_set(Var,Member,Value)).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%% BOTVAR INTERFACE
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%% botvar_get(?Name,?Value).
%% botvar_get(?NameSpace,?Name,?Value).
%% botvar_set(+Name,+Value).
%% botvar_set(+NameSpace,+Name,+Value).
%
% get/set a system botvar
%
%  NameSpace is 'bot' (equiv to "examplebot_resident" ) or some other string 
%
%  Name is "favfood"
%
%  Value can be any datatpye (except a Prolog term?!)
%    Well is can be a prolog term but obeys the same semanticsa of setting a CLR Field of type System.Object


wbotvar_set(BotID,Name,ValueO):- wbotvar_set(BotID,bot,Name,ValueO).
wbotvar_set(BotID,NS,Name,ValueO):-wbot_safe_namespace(BotID,NS,NS1),cli_call('MushDLR223.ScriptEngines.ScriptManager','AddSetting',[BotID,NS1,Name,ValueO],_).

wbotvar_get(BotID,Name,ValueO):- wbotvar_get(BotID,bot,Name,ValueO).
wbotvar_get(BotID,NS,Name,ValueO):-ground(NS+Name),!,wbot_safe_namespace(BotID,NS,NS1),
   cli_call('MushDLR223.ScriptEngines.ScriptManager','GetGroup',[BotID,NS1,Name],Value),once(value_deref(Value,ValueO)).
wbotvar_get(BotID,NS,Name,ValueO):- wbot_safe_namespace(BotID,NS,NS1),wbotvar_keys(BotID,NS1,Name,CP),
   cli_call(CP,'GetGroup'(object,string),[BotID,Name],Value),once(value_deref(Value,ValueO)).


wbot_safe_namespace(BotID,bot,NS1):-!,wbotname(BotID,NS),wbot_safe_namespace(BotID,NS,NS1).
wbot_safe_namespace(BotID,"bot",NS1):-!,wbotname(BotID,NS),wbot_safe_namespace(BotID,NS,NS1).
wbot_safe_namespace(_,NS,NS1):-global_tokey(NS,NS1).

global_tokey(Name,Key):-var(Name),Key=Name.
global_tokey(Name,Key):-cli_call('MushDLR223.ScriptEngines.ScriptManager','ToKey',[Name],Key),!.

wbot_samekey(_BotID,Name1,Name2):-ground(Name1),Name1=Name2,!.
wbot_samekey(_BotID,Name1,Name2):-var(Name1),ground(Name2),Name1=Name2,!.
wbot_samekey(BotID,Name1,Name2):-member(Wild,["",bot]),member(Wild,[Name1,Name2]),wbotname(BotID,Name),ignore(wbot_safe_namespace(BotID,Name2,Name)),!.
wbot_samekey(_BotID,Name1,Name2):-ground(Name1+Name2),global_tokey(Name1,Key),global_tokey(Name2,Key).

wbotvar_namespaces(BotID,NSO):-cli_call('MushDLR223.ScriptEngines.ScriptManager','GetNameSpaces',[BotID],NSs),cli_col(NSs,NS),wbot_samekey(BotID,NS,NSO).

wbotvar_keys(BotID,NSO,NameO,CP):-
   wbotvar_namespaces(BotID,NSO),cli_call('MushDLR223.ScriptEngines.ScriptManager','GetProviders',[BotID,NSO],CPs),cli_col(CPs,CP),
   cli_call(CP,'SettingNames'('MushDLR223.ScriptEngines.ICollectionRequester',int),[BotID,1],Names),cli_col(Names,Name),wbot_samekey(BotID,Name,NameO).


wbot_gp_to_vars(_BotID,GP,Var):- cli_call('MushDLR223.ScriptEngines.SingleNameValue','MakeKVP'('MushDLR223.ScriptEngines.ICollectionProvider',int),[GP,2],Col),cli_col(Col,Var).

value_deref(Value,ValueO):-cli_col(Value,ValueO).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%% AIMLBOT VAR Interface
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
wbot_aimlbot_ref(BotID,AIMLBot):-wbotget(BotID,'Plugins',Plugs),cli_map(Plugs,"AIMLBotModule",AIMLBotModule),cli_get(AIMLBotModule,'MyBot',AIMLBot),!.

% ?-  bot_aimlvars_get("bot",Y,Z),cli_to_str(Z,S).

wbot_aimlvar_get(BotID,NameSpace,VarName,Value):- hwbot_aimlvar_get_0(BotID,NameSpace,VarName,ValueU),aimlvar_value(ValueU,Value).

aimlvar_value(MISSING, ""):- cli_is_void(MISSING),!.
aimlvar_value(MISSING, ""):- cli_is_null(MISSING),!.
aimlvar_value(ValueU,Value):- cli_get(ValueU,'ToString',Value).

hwbot_aimlvar_get_0(BotID,NameSpace,VarName,Value):-wbot_aimlbot_ref(BotID,AIMLBot),var(NameSpace),!,cli_get(AIMLBot,'AllDictionaries',ADS),
   cli_map(ADS,NameSpace,BVs),cli_map(BVs,VarName,Value).
hwbot_aimlvar_get_0(BotID,NameSpace,VarName,Value):-wbot_aimlbot_ref(BotID,AIMLBot),cli_get(AIMLBot,'GetDictionary'(NameSpace),BVs),cli_map(BVs,VarName,Value).

wbot_aimlvar_set(BotID,NameSpace,VarName,Value):-wbot_aimlbot_ref(BotID,AIMLBot),var(NameSpace),!,cli_get(AIMLBot,'AllDictionaries',ADS),
   cli_map(ADS,NameSpace,BVs),cli_set(BVs,VarName,Value).
wbot_aimlvar_set(BotID,NameSpace,VarName,Value):-wbot_aimlbot_ref(BotID,AIMLBot),cli_get(AIMLBot,'GetDictionary'(NameSpace),BVs),cli_set(BVs,VarName,Value).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%% BOTVAR HOOKS
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
wbot_addvars(BotID,NameSpace,GET,SET,KEYS):-
   strip_module(GET,Module,_),
   cli_new('PrologScriptEngine.BotVarProvider',0,[Module,NameSpace,GET,SET,KEYS],Provider),
   cli_call('MushDLR223.ScriptEngines.ScriptManager','AddGroupProvider',[BotID,Provider],_).


% register a small helper for ourselves
wbotkey_match(BotID,"",Key,MyKey):-wbot_samekey(BotID,Key,MyKey),!.
wbotkey_match(BotID,NameSpace,Key,MyKey):-wbotname(BotID,BotName),wbot_samekey(BotID,NameSpace,BotName),!,wbot_samekey(BotID,Key,MyKey),!.
wbotkey_match(BotID,BotID,Key,MyKey):-wbot_samekey(BotID,Key,MyKey),!.

% Need freedom to declare in multiple codeblocks
:- PREDS = (bv:hook_botvar_get/4,bv:hook_botvar_set/4,bv:hook_botvar_desc/4,bv:hook_botvar_props/4,bv:hook_botvar_key/3,
   bv:hook_bot_created/1, bv:hook_bot_loggedin/1, bv:hook_bot_event/3),
   discontiguous(PREDS),
   %%module_transparent(PREDS),
   multifile(PREDS),
   dynamic(PREDS).

wbot_to_namespace(BotID,"",BotName):-wbotname(BotID,Name),global_tokey(Name,BotName),!.
wbot_to_namespace(BotID,bot,BotName):-wbotname(BotID,Name),global_tokey(Name,BotName),!.
wbot_to_namespace(_BotID,NameSpaceS,NameSpace):-global_tokey(NameSpaceS,NameSpace).

/*
ahook_botvar_get(BotID,NameSpace0,Key0,Value):-
   wbot_to_namespace(BotID,NameSpace0,NameSpace),global_tokey(Key0,Key),
   clause(bv:hook_botvar_get(BotID,MNameSpace,MKey,Value),BODY),
   once((wbot_samekey(BotID,NameSpace,MNameSpace),wbot_samekey(BotID,Key,MKey))), catch(user:(BODY),_,fail).

ahook_botvar_set(BotID,NameSpace0,Key0,Value):-
   wbot_to_namespace(BotID,NameSpace0,NameSpace),global_tokey(Key0,Key),
   clause(bv:hook_botvar_set(BotID,MNameSpace,MKey,Value),BODY),
   once((wbot_samekey(BotID,NameSpace,MNameSpace),wbot_samekey(BotID,Key,MKey))), catch(user:(BODY),_,fail).

*/

ahook_botvar_get(BotID,NameSpace0,Key0,Value):-
   wbot_to_namespace(BotID,NameSpace0,NameSpace),global_tokey(Key0,Key),
   catch(bv:hook_botvar_get(BotID,NameSpace,Key,Value),E,cli_debug(ahook_botvar_get(BotID,NameSpace,Key,E))).

ahook_botvar_set(BotID,NameSpace0,Key0,Value):-
   wbot_to_namespace(BotID,NameSpace0,NameSpace),global_tokey(Key0,Key),
   catch(bv:hook_botvar_set(BotID,NameSpace,Key,Value),E,cli_debug(ahook_botvar_set(BotID,NameSpace,Key,Value,E))).


ahook_botvar_key(BotID,NameSpace0,Key):-
   wbot_to_namespace(BotID,NameSpace0,NameSpace),
   clause(bv:hook_botvar_key(BotID,MNameSpace,MKey),BODY),
   once((wbot_samekey(BotID,NameSpace,MNameSpace),wbot_samekey(BotID,Key,MKey))), catch(user:(BODY),_,fail).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%% BOTVAR DYNAMIC HOOKS
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
wbot_addvars_dynpred(BotID,PredImpl):-wbotname(BotID,NameSpace),wbot_addvars_dynpred(BotID,NameSpace,PredImpl).

wbot_addvars_dynpred(BotID,NameSpace,PredImpl):-module_functor(PredImpl,_Module,Pred,_Arity),
   atom_concat(Pred,'_get',GET),atom_concat(Pred,'_set',SET),atom_concat(Pred,'_remove',REM),atom_concat(Pred,'_clear',CLR),atom_concat(Pred,'_keys',KEYS),
   PANON =..[Pred,NameSpace,_,_],PGET =..[GET,NameSpace,Key,Val], PSET =..[SET,NameSpace,Key,Val],PREM =..[REM,NameSpace,Val],
   PDYN =..[Pred,NameSpace,Key,Val],
   PKEYSHEAD =..[KEYS,NameSpace,Key],
   PKEYSBODY =..[Pred,NameSpace,Key,_],
   asserta(( PGET :- PDYN )),
   asserta(( PSET :- assert(PDYN) )),
   asserta(( PREM :- retract(PDYN) )),
   asserta(( CLR :- retractall(PANON) )),
   asserta(( PKEYSHEAD :- retractall(PKEYSBODY) )),  
   wbot_addvars(BotID,NameSpace,GET,SET,KEYS).

/*
wbot_add_botvar(BotID,Var,PredImpl):-wbotget(BotID,['WorldSystem','simGroupProviders'],GPs),
      cli_call('MushDLR223.ScriptEngines.SingleNameValue','MakeKVP'

      cli_lib_call('CreatePrologBackedDictionary',[PredImpl],PBD),
      cli_call('MushDLR223.ScriptEngines.DictionaryWrapper','CreateDictionaryWrapper',[PBD],Provider),
      cli_call('MushDLR223.ScriptEngines.ScriptManager','AddNamedProvider',[NameSpace, Provider],_).

               cli_new_prolog_dictionary(Module:Pred/Arity,string,object,PBD),
               cli_call('MushDLR223.ScriptEngines.DictionaryWrapper','CreateDictionaryWrapper',[NameSpace, PBD],Provider),
*/

%------------------------------------------------------------------------------
% listing functions
%------------------------------------------------------------------------------

listS(P):-call(P,A),cli_to_str(A,S),writeq(S),fail.
listS(P):-writeq('done'(P)),nl.

listAvatars:-listS(world_avatar).
listPrims:-listS(grid_object).

bot_apiname(N):- \+ atom_concat(_,'Handler',N),\+ atom_concat('On',_,N),\+ sub_atom(N,_,_,_,'_').

bot_cms0(N,Y,P):-cli_memb('OpenMetaverse.GridClient',f,M),arg(3,M,Type),cli_memb(Type,m,Y),arg(6,Y,decl(_,Type)),arg(7,Y,access_pafv(true, _, _, _)),arg(2,Y,N),bot_apiname(N),arg(8,Y,P).
bot_cms(N,Doc,PP):-bot_cms0(N,_Y,P),cli_get(P,'GetParameters',PPs),bot_params_to_list(PPs,PP),cli_member_doc(P,Doc,_XML).

bot_params_to_list(PPs,PNs):-findall(T:N,(cli_col(PPs,PI),bot_param(PI,T,N)),PNs).

bot_param(PI,T,N):-cli_get(PI,'ParameterType',TR),cli_type_to_typespec(TR,T),cli_get(PI,'Name',N).


%------------------------------------------------------------------------------
% Event Waiting Code Idioms
%  waits 10 seconds by default
%------------------------------------------------------------------------------

wbot_with_manager(BotID,Manager,Request,Event:Vars,EventCheck,CreateResult):-   
   once((wbotget(BotID,Manager,AvMan),
   cli_new_event_waiter(AvMan,Event,WaitOn),
   request_break(Request,PreCall,LRequest),
   pcall(PreCall),
   cli_call(AvMan,LRequest,_))),
   cli_block_until_event(WaitOn, 10000, lambda(Vars,EventCheck)),
   pcall(CreateResult).

/*
%% Example of what the above code idiom does 
findUUIDForName00(NameString,Found):-
   botget('Avatars',AvMan),          % get the AvatarManager object
   cli_new_event_waiter(AvMan,'AvatarPickerReply',WaitOn),  % create a new event handler object (and register it)
   cli_call('UUID','Random',QID),      % make a random UUID
   cli_call(AvMan,'RequestAvatarNameSearch'(NameString,QID),_),   % make the request to the server
   cli_block_until_event(WaitOn, 10000, lambda(Evt,cli_get(Evt,'QueryID',QID)), _),  %  wait for up to 10 seconds for the right the QueryID
   cli_get(Evt,'Avatars',U2SDict),  % get the UUID2Name dictionary
   cli_map(U2SDict,Found,NameString),   % Search that dictionary for the UUID by matching the String
   cli_call(WaitOn,'Dispose',_).  % unregister the handler and dispose
*/


wbot_with_manager_requestavatar_reply(BotID,AvatarID,Found,WhatNot,WhatNots):-
   concat_atom(['RequestAvatar',WhatNot],'',RequestAvatarWhatNot),
   RequestAvatarWhatNotWID=..[RequestAvatarWhatNot,AvatarID],
   concat_atom(['Avatar',WhatNot,'Reply'],'',AvatarWhatNotReply),
   wbot_with_manager(BotID,'Avatars',
        RequestAvatarWhatNotWID,
        AvatarWhatNotReply:Evt,
        cli_get(Evt,'AvatarID',AvatarID),
        cli_get(Evt,WhatNots,Found)).

pcall(A+B):-!,pcall(A),pcall(B).
pcall((A,B)):-!,pcall(A),pcall(B).
pcall({Call}):-!,call(Call).
pcall(Call):-call(Call).


request_break(PreCall:Request,PreCall,Request).
request_break(Request,true,Request).

prim_to_uuid(UUID,UUID):-cli_is_type(UUID,'OpenMetaverse.UUID'),!.
prim_to_uuid(Prim,UUID):-cli_get(Prim,'ID',UUID),!.
prim_to_uuid(Prim,UUID):-name_to_location_ref(Prim,Obj),cli_get(Obj,'ID',UUID),!.
prim_to_uuid(Prim,_):-cogbot_throw('Cant get UUID from'(Prim)).

%------------------------------------------------------------------------------
%% Agent Manager Methods
%------------------------------------------------------------------------------
/*
        public void RequestScriptSensor(string name, UUID searchID, ScriptSensorTypeFlags type, float range, float arc, UUID requestID, Simulator sim)
        {
            ScriptSensorRequestPacket request = new ScriptSensorRequestPacket();
            request.Requester.Arc = arc;
            request.Requester.Range = range;
            request.Requester.RegionHandle = sim.Handle;
            request.Requester.RequestID = requestID;
            request.Requester.SearchDir = Quaternion.Identity; // TODO: this needs to be tested
            request.Requester.SearchID = searchID;
            request.Requester.SearchName = Utils.StringToBytes(name);
            request.Requester.SearchPos = Vector3.Zero;
            request.Requester.SearchRegions = 0; // TODO: ?
            request.Requester.SourceID = Client.Self.AgentID;
            request.Requester.Type = (int)type;

            Client.Network.SendPacket(request, sim);
        }

        /// <summary>Get the ID of the primitive sending the sensor</summary>
        public UUID RequestorID { get { return m_RequestorID; } }
        /// <summary>Get the ID of the group associated with the primitive</summary>
        public UUID GroupID { get { return m_GroupID; } }
        /// <summary>Get the name of the primitive sending the sensor</summary>
        public string Name { get { return m_Name; } }
        /// <summary>Get the ID of the primitive sending the sensor</summary>
        public UUID ObjectID { get { return m_ObjectID; } }
        /// <summary>Get the ID of the owner of the primitive sending the sensor</summary>
        public UUID OwnerID { get { return m_OwnerID; } }
        /// <summary>Get the position of the primitive sending the sensor</summary>
        public Vector3 Position { get { return m_Position; } }
        /// <summary>Get the range the primitive specified to scan</summary>
        public float Range { get { return m_Range; } }
        /// <summary>Get the rotation of the primitive sending the sensor</summary>
        public Quaternion Rotation { get { return m_Rotation; } }
        /// <summary>Get the type of sensor the primitive sent</summary>
        public ScriptSensorTypeFlags Type { get { return m_Type; } }
        /// <summary>Get the velocity of the primitive sending the sensor</summary>
        public Vector3 Velocity { get { return m_Velocity; } }

wbot_script_sensor(BotID,NameString,Evt):-
   cli_call('UUID','Random',QID),
   cli_new('ScriptSensorRequestPacket',[],Packet),
   wbotget(BotID,'Self',AvMan),
   wbotget(BotID,['Network','CurrentSim','Handle'],RegionHandle),
   cli_get(AvMan,{'AgentID'=AgentID}
   cli_set(Packet,{'Arc'=Arc,'Range'=Range,
                  'RegionHandle'=eval(wbotget(BotID,['Network','CurrentSim','Handle'])),
                  'SourceID'=AgentID})
   cli_new_event_waiter(AvMan,'ScriptSensorReply',WaitOn),
   wbotcall(BotID,['Network','SendPacket'(Packet)],_),
   cli_block_until_event(WaitOn, 10000, lambda(Evt,cli_get(Evt,'RequestorID',QID))).

*/
wbot_tf(_,CallAct,TF):-once((call(CallAct),TF='@'(true));TF='@'(false)).

wbot_sit(BotID,'ground',TF):-wbotget(BotID,'Self',AvMan),cli_call(AvMan,'SitOnGround',[],_),TF='@'(true),!.
wbot_sit(BotID,Prim,TF):-prim_to_uuid(Prim,PrimUUID),wbot_request_sit(BotID,PrimUUID,v3(0,0,0),TF).
    

wbot_request_sit(BotID,PrimUUID,OffsetV3,TF):-
   wbotget(BotID,'Self',AvMan),
   cli_new_event_waiter(AvMan,'AvatarSitResponse',WaitOn),
   cli_add_event_waiter(WaitOn,AvMan,'AlertMessage',NewWaitOn),
   cli_call(AvMan,'RequestSit'(PrimUUID,OffsetV3),_),
   cli_block_until_event(NewWaitOn, 10000, lambda(Evt,pass_or_fail_sit(PrimUUID,Evt,TF))),
   (cli_is_true(TF)->cli_call(AvMan,'Sit',[],_);true).

pass_or_fail_sit(PrimUUID,Evt,TF):-cli_is_type(Evt,'AvatarSitResponseEventArgs'),!,cli_get(Evt,'ObjectID',PrimUUID),
   cli_get(Evt,'AutoPilot',AP),((cli_is_true(AP)->cli_false(TF);cli_true(TF))).
pass_or_fail_sit(_,Evt,TF):-cli_is_type(Evt,'AlertMessageEventArgs'),cli_get(Evt,'Message',Message),!,
   cli_call(Message,'Contains'("enough room"),'@'(true)),cli_false(TF).
   

%------------------------------------------------------------------------------
%% Avatar Manager Methods
%------------------------------------------------------------------------------

findUUIDForName(NameString,AvatarID):-
   bot_with_manager('Avatars',
        cli_call('UUID','Random',QID):'RequestAvatarNameSearch'(NameString,QID),
        'AvatarPickerReply':Evt,
        cli_get(Evt,'QueryID',QID),
        {cli_get(Evt,'Avatars',U2SDict),
          once(cli_map(U2SDict,AvatarID,NameString);cli_get('UUID','Zero',AvatarID))}).

findNameForUUID(AvatarID,NameString):-
   bot_with_manager('Avatars',
        'RequestAvatarName'(AvatarID),
        'UUIDNameReply':Evt,
        ((cli_get(Evt,'Names',U2SDict),cli_map(U2SDict,AvatarID,NameString))),
        true).



findAvatarProperties(AvatarID,Found):-bot_with_manager_requestavatar_reply(AvatarID,Found,'Properties','Properties').

findAvatarPicks(AvatarID,Found):-bot_with_manager_requestavatar_reply(AvatarID,Found,'Picks','Picks').

findAvatarClassified(AvatarID,Found):-bot_with_manager_requestavatar_reply(AvatarID,Found,'Classified','Classifieds').

findAvatarName(AvatarID,Found):-bot_with_manager_requestavatar_reply(AvatarID,Found,'Classified','Classifieds').

cap_word(In,Out):-atom_codes(In,[L|Rest]),code_type(U,to_upper(L)),atom_codes(Out,[U|Rest]).

ppList2Args(PP,Args):-ppList2Args0(PP,Args).

ppList2Args0([],[]):-!.
ppList2Args0([P|PP],[A|Args]):-
   ppList2Arg(P,A),
   ppList2Args0(PP,Args).

ppList2Arg('PlTerm':A,AA):-!,ppList2Arg(A,AA).
ppList2Arg('Int32':A,AA):-!,ppList2Arg(A,AA).
ppList2Arg(A:B,AA):-ppList2Arg(A,A1),ppList2Arg(B,B1),atom_concat(A1,B1,AB),!,ppList2Arg(AB,AA).
ppList2Arg(F,B):-compound(F),F=..List,concat_atom(List,A),!,ppList2Arg(A,B).
ppList2Arg(A,BB):- concat_atom([B,''],"Out",A),!,cap_word(B,BB1),concat_atom([-,BB1],'',BB).
ppList2Arg(A,BB):- concat_atom([B,''],"In",A),!,cap_word(B,BB1),concat_atom([+,BB1],'',BB).
ppList2Arg(A,BB):- concat_atom([_,_|_],"Byref",A),!,A=B,cap_word(B,BB1),concat_atom([?,BB1],'',BB).
ppList2Arg(A,BB):- concat_atom([_,_|_],"Out",A),!,A=B,cap_word(B,BB1),concat_atom([-,BB1],'',BB).
ppList2Arg(A,BB):- concat_atom([_,_|_],"In",A),A=B,!,cap_word(B,BB1),concat_atom([+,BB1],'',BB).
ppList2Arg(A,BB):-concat_atom([A],'',B),cap_word(B,BB).

%------------------------------------------------------------------------------
% scans and export predicates defined from this module.. 
%  if a predicate has wbot* at the front it is exported..
%
% wbot_foo(BotID,Bar)
%     will create a stub like
% bot_foo(X):-current_bot(BotID),wbot_foo(BotID,X).
%------------------------------------------------------------------------------
make_current_bot_ver(_,N,_,AA):-current_predicate(cogrobot:N/AA),!,export(N/AA).
make_current_bot_ver(F,N,_,AA):-length(List,AA),Head=..[N|List],Body=..[F,BotID|List],assert((Head:-current_bot(BotID),Body)),export(N/AA).

scan_and_export_wb:-current_predicate(cogrobot:F/A),
   once((atom_concat(wbot,Before,F),export(F/A),atom_concat(bot,Before,New),AA is A-1,make_current_bot_ver(F,New,A,AA))),fail.
scan_and_export_wb:-current_predicate(cogrobot:F/A),once((member(S,[ahook,bot,find,sim,grid,cli,glob,world,hook,app]),atom_concat(S,_,F),export(F/A))),fail.
scan_and_export_wb.

:-scan_and_export_wb.

:-cli_hide(cogrobot:current_bot/1).
:-cli_hide(cogrobot:current_bot_db/2).
:-cli_hide(cogrobot:wbot_inventory/3).
:-cli_hide(cogrobot:wbot_my_inventory/3).
:-cli_hide(cogrobot:wbot_lib_inventory/3).

end_of_file.

?- cli_find_type('SbsSW.SwiPlCs.PlEngine',T),cli_get(static(T),'SavedRegisterForeign',SRF),cli_map(SRF,K,V),cli_get(V,'Method',M),cli_writeln(V=M).
cli_find_type('Swicli.Library.PrologCLR',T),cli_get(static(T),'AutoDocInfos',SRF),cli_map(SRF,K,V),cli_writeln(K=V),fail.



