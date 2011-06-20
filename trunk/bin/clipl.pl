
:- module(clipl,
          [ 
            cli_debug/1,
            cli_Eval/3,
            cli_GetSymbol/3,
            cli_Intern/3,
            cli_IsDefined/2,
            cliAddEventHandler/3,
            cliAddLayout/2,
            cliCall/3,
            cliCall/4,
            cliCol/2,
            cliCollection/2,
            cliFindClass/2,
            cliFindMethod/3,
            cliFindType/2,
            cliGet/3,
            cliGetRaw/3,
            cliGetType/2,
            cliIsNull/1,
            cliIsObject/1,
            cliIsType/2,            
            cliLoadAssembly/1,
            cliMemb/2,
            cliMemb/3,
            cliMembers/2,
            cliNew/3,
            cliNew/4,
            cliPropsForType/2,
            cliSet/3,
            cliSetRaw/3,
            cliShortType/2,
            cliSubclass/2,
            cliToData/2,
            cliToData/3,
            cliToFromLayout/3,
            cliToString/2,
            cliToStringRaw/2,
            cliToTagged/2,
            cliWriteln/1,
            link_swiplcs/1,
            to_string/2
          ]).

:-dynamic(shortTypeName/2).

:-module_transparent(shortTypeName/2).
:-module_transparent(cliGet/3).

%------------------------------------------------------------------------------
% Load C++ DLL
%------------------------------------------------------------------------------
:-load_foreign_library(swicli35).

%------------------------------------------------------------------------------
% The C++ DLL should have given us cli_load_lib/3
%  ?- cli_load_lib( +AssemblyName, +FullClassName, +StaticMethodName).
%------------------------------------------------------------------------------
:-cli_load_lib('SwiPlCs','SbsSW.SwiPlCs.swipl_win','install').

%------------------------------------------------------------------------------
%% cli_load_lib/3 should have given us 
%   cliLoadAssembly/1
%------------------------------------------------------------------------------
:-cliLoadAssembly('SwiPlCs.dll').
% the cliLoadAssembly/1 should have give us a few more cli<Predicates>



%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliIsType(+Impl,+Type) tests to see if the Impl Object is assignable to Type
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliIsType(Impl,Type):-cliFindType(Type,RealType),cliCall(RealType,'IsInstance'(object),[Impl],'@'(true)).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliSubclass(+Subclass,+Superclass) tests to see if the Subclass is assignable to Superclass
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliSubclass(Sub,Sup):-cliFindType(Sub,RealSub),cliFindType(Sup,RealSup),cliCall(RealSup,'IsAssignable'('System.Type'),[RealSub],'@'(true)).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliCol(+Col,-Elem) iterates out Elems for Collection
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
% old version:s cliCollection(Obj,Ele):-cliCall(Obj,'ToArray',[],Array),cliArrayToTerm(Array,Vect),!,arg(_,Vect,Ele).
cliCollection(Obj,Ele):-  
      cliCall(Obj,'GetEnumerator',[],Enum),
      call_cleanup(cli_enumerator_element(Enum,Ele),cliFree(Enum)).

cliCol(X,Y):-cliCollection(X,Y).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliWriteln(+Obj) writes an object out
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliWriteln(S):-cliToString(S,W),writeq(W),nl.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliToString(+Obj,-String) writes an object out to string
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliToString(Term,Term):- not(compound(Term)),!.
cliToString(Term,String):-cliIsObject(Term),!,cliToStringRaw(Term,String).
cliToString([A|B],[AS|BS]):-!,cliToString(A,AS),cliToString(B,BS).
cliToString(Term,String):-Term=..[F|A],cliToString(A,AS),String=..[F|AS],!.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliIsNull(+Obj) is Object null or void or variable
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliIsNull(Obj):-(var(Obj);Obj='@'(null);Obj='@'(void)),!.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% cliIsObject(+Obj) is Object a tagged object and not null or void
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliIsObject(X):- \+ cliIsNull(X),functor(X,F,_),F='@'.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cliMemb(O,F,X) Object to the member infos of it
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliMemb(O,X):-cliMembers(O,Y),member(X,Y).
cliMemb(O,F,X):-cliMemb(O,X),member(F,[f,p, c,m ,e]),functor(X,F,_).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cliNew(+Obj, +CallTerm, -Out).
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%% ?- cliNew('java.lang.Long'(long),[44],Out),cliToString(Out,Str).
%% same as..
%% ?- cliNew('java.lang.Long',[long],[44],Out),cliToString(Out,Str).
%% arity 4 exists to specify generic types
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliNew(Clazz,ConstArgs,Out):-Clazz=..[BasicType|ParmSpc],cliNew(BasicType,ParmSpc,ConstArgs,Out).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cliCall(+Obj, +CallTerm, -Out).
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliCall(Obj,CallTerm,Out):-CallTerm=..[MethodName|Args],cliCall(Obj,MethodName,Args,Out).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cliGet(+Obj, +PropTerm, -Out).
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliGet(Obj,_,_):-cliIsNull(Obj),!,fail.
cliGet(Obj,[P],Value):-!,cliGet(Obj,P,Value).
cliGet(Obj,[P|N],Value):-!,cliGet(Obj,P,M),cliGet(M,N,Value),!.
cliGet(Obj,P,Value):-cliGetRaw(Obj,P,Value).


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cliSet(+Obj, +PropTerm, +NewValue).
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
cliSet(Obj,_,_):-cliIsNull(Obj),!,fail.
cliSet(Obj,[P],Value):-!,cliSet(Obj,P,Value).
cliSet(Obj,[P|N],Value):-!,cliGet(Obj,P,M),cliSet(M,N,Value),!.
cliSet(Obj,P,Value):-cliSetRaw(Obj,P,Value).


%type   jpl_iterator_element(object, datum)

% jpl_iterator_element(+Iterator, -Element) :-

cli_iterator_element(I, E) :- %%cliIsInstance('java.util.Iterator',I),!,
	(   cliCall(I, hasNext, [], @(true))
	->  (   cliCall(I, next, [], E)        % surely it's steadfast...
	;   cli_iterator_element(I, E)
	)
	).
cli_enumerator_element(I, E) :- %%cliIsInstance('System.Collections.IEnumerator',I),!,
	(   cliCall(I, 'MoveNext', [], @(true))
	->  (   cliGet(I, 'Current', E)        % surely it's steadfast...
	;   cli_enumerator_element(I, E)
	)
	).



cliToData(Term,String):- cliNew('System.Collections.Generic.List'(object),[],[],Objs),cliToData(Objs,Term,String).
cliToData(_,Term,Term):- not(compound(Term)),!.
cliToData(_Objs,[A|B],[A|B]):-!.
cliToData(_Objs,[A|B],[A|B]):-'\+' '\+' A=[_=_],!.
cliToData(Objs,[A|B],[AS|BS]):-!,cliToData(Objs,A,AS),cliToData(Objs,B,BS).
cliToData(Objs,Term,String):-cliIsObject(Term),!,cliGetTermData(Objs,Term,Mid),(Term==Mid-> true; cliToData(Objs,Mid,String)).
cliToData(Objs,Term,String):-Term=..[F|A],cliToData(Objs,A,AS),String=..[F|AS],!.

cliGetTermData(_Objs,Term,Mid):-cliGetType(Term,Type),cliPropsForType(Type,Props),cliGetMap(Term,Props,Name,Value,Name=Value,Mid),!.
cliGetTermData(Objs,Term,Mid):-cliGetTerm(Objs,Term,Mid),!.


cliGetMap(Term,_,_,_,_,Term):- \+ cliIsObject(Term).
cliGetMap(Term,Props,Name,Value,NameValue,List):-findall(NameValue,(member(Name,Props),cliGet(Term,Name,Value)),List).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cli_debug/[1,2]
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5

cli_debug(format(Format,Args)):-atom(Format),sformat(S,Format,Args),cli_debug(S).
cli_debug(Data):-format(user_error,'~n %% CLI-DEBUG: ~q~n',[Data]),flush_output(user_error).

%%cli_debug(Engine,Data):- format(user_error,'~n %% ENGINE-DEBUG: ~q',[Engine]),cli_debug(Data).

to_string(Object,String):-jpl_is_ref(Object),!,jpl_call(Object,toString,[],String).
to_string(Object,String):-Object=String.

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cli_Intern/3
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
:-dynamic(cli_Interned/3).
:-multifile(cli_Interned/3).
:-module_transparent(cli_Interned/3).
cli_Intern(Engine,Name,Value):-retractall(cli_Interned(Engine,Name,_)),assert(cli_Interned(Engine,Name,Value)),cli_debug(cli_Interned(Name,Value)),!.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cli_Eval/3
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5

:-dynamic(cli_Eval_Hook/3).
:-multifile(cli_Eval_Hook/3).
:-module_transparent(cli_Eval_Hook/3).

cli_Eval(Engine,Name,Value):- cli_Eval_Hook(Engine,Name,Value),!,cli_debug(cli_Eval(Engine,Name,Value)),!.
cli_Eval(Engine,Name,Value):- Value=cli_Eval(Engine,Name),cli_debug(cli_Eval(Name,Value)),!.

cli_Eval_Hook(Engine,In,Out):- Out = foobar(Engine,In).

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cli_IsDefined/2
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5

cli_IsDefined(_Engine,Name):-cli_debug(cli_not_IsDefined(Name)),!,fail.


%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5
%%% cli_GetSymbol/3
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5

cli_GetSymbol(Engine,Name,Value):- (cli_Interned(Engine,Name,Value);Value=cli_UnDefined(Name)),!,cli_debug(cli_GetSymbol(Name,Value)),!.






:-cli_debug('I am swi_cli.pl in BIN DIR!!').

:-use_module(library(jpl)).
%:-use_module(library(pce)).

%%:-interactor.