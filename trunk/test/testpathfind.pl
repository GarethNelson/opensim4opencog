:-module(testpathfind, [tpf_method/1, tpf/0, tpf/1, tpf1/0,tpf2/0,tpfi/0, makePipe/2]).


:- use_module(test(testsupport)).
:-use_module(library(clipl)).
:-use_module(library('simulator/cogrobot')).

:-discontiguous(test_desc/2).
:-discontiguous(test/1).

:- dynamic(goMethod/1).

% set the method of movement we're testing

%%goMethod('moveto').  % low level turnTo and move
%%goMethod(autopilot). % only use autoilot
goMethod('astargoto').  % only use A*
%%goMethod('follow*').  % do the best case


% using the test method of movement, go to Location
goByMethod(Location) :-
	goMethod(GM),!, % slight ugliness, just want first one
	Goal =.. [GM, Location],
	apiBotClientCmd(Goal).


%%  teleportTo('annies haven/129.044327/128.206070/81.519630/')
%% teleportTo('annies haven/129.044327/128.206070/80')
%% apiBotClientCmd(autopilot('annies haven/127.044327/128.206070/81.519630/')).
teleportTo(StartName):-
        apiBotClientCmd(stopmoving),
        cogrobot:toGlobalVect(StartName,Start),
        cogrobot:vectorAdd(Start,v3d(0,0,0.7),Start2),
        apiBotClientCmd(teleport(Start2)).
        /*
        %%apiBotClientCmd(waitpos(2,Start)),
        cogrobot:distanceTo(Start,Dist),
        %% if fallthru floor try to get closer
        (Dist < 3 -> apiBotClientCmd(moveto(Start)) ; ( cogrobot:vectorAdd(Start2,v3d(0,0,1),Start3),apiBotClientCmd(teleport(Start3)) )),!.*/

% convenience method that does the 'normal' thing -
% tp to Start, move using the standard method to
%  move_test(10 , start_swim_surface , stop_swim_surface).

move_test(Name):-atom_concat('start_',Name,Start),atom_concat('stop_',Name,Stop),move_test(10,Start,Stop).

move_test(Time , Start , End) :-
        apiBotClientCmd(stopmoving),
	teleportTo(Start),
        apiBotClientCmd('remeshprim'),
        botClient(['TheSimAvatar','KillPipes'],_),
	goByMethod(End),
        apiBotClientCmd(waitpos(Time,End)),
        apiBotClientCmd(stopmoving).


% this is just to debug the test framework with
test_desc(easy , 'insanely easy test').
test(N) :-
	N = easy,
	writeq('in easy'),nl.


test_desc(clear , 'clear path 10 meters').
test(N) :-
	N = clear,
	start_test(N),
        move_test(15,
		'annies haven/129.044327/128.206070/81.519630/',
	        'annies haven/133.630234/132.717392/81.546028/'),
	std_end(N , 17 ,2).


test_desc(zero , 'Zero Distance').
test(N) :-
	N = zero,
	start_test(N),
	move_test(3 , start_test_2 , stop_test_2),
        std_end(N , 2 , 0).


test_desc(obstacle , 'Go around obstacle').
test(N) :-
	 N = obstacle,
         start_test(N),
	 move_test(25 , start_test_3 , stop_test_3),
         std_end(N , 27 , 2).

test_desc(other_side_wall , 'Goal Other Side Of Wall').
test(N) :-
	N = other_side_wall,
	start_test(N),
        move_test(25 , start_test_4 , stop_test_4),
        std_end(N , 27 , 1).

test_desc(elev_path , 'On Elevated Path').
test(N) :-
	N = elev_path,
	start_test(N),
	move_test(15 ,
		  'annies haven/149.389313/129.028732/85.411255/',
		  'annies haven/156.894470/137.385620/85.394775/'),
	std_end(N , 17 , 0).

test_desc(ridge , 'On Elevated land Path').
test(N) :-
	N = ridge,
	start_test(N),
	move_test(34 ,
		  'start_ridge',
		  'stop_ridge'),
	std_end(N , 40 , 0).

test_desc(ahill , 'Arround Elevated hill').
test(N) :-
	N = ahill,
	start_test(N),
	move_test(34 ,
		  'start_ahill',
		  'stop_ahill'),
	std_end(N , 40 , 0).


test_desc(swim_surface , 'Swim arround the island').
test(N) :-
	N = swim_surface,
	start_test(N),
	move_test(34 ,
		  'start_swim_surface',
		  'stop_swim_surface'),
	std_end(N , 40 , 0).


test_desc(grnd_maze , 'Ground maze simple').
test(N) :-
	N = grnd_maze,
	start_test(N),
	move_test(30 ,
		  'annies haven/4.813091/6.331439/27.287579/',
		  'annies haven/26.930264/12.801470/27.149252/'),
	std_end(N , 34 , 2).

test_desc(island_hop , 'Island hop').
test(N) :-
	N = island_hop,
	start_test(N),
	move_test(45 , start_island_hop , stop_island_hop),
	std_end(N , 45 , 2).

test_desc(hill_walk , 'Hill Walk').
test(N) :-
	N = hill_walk,
	start_test(N),
	move_test(60 , start_hill_walk , stop_hill_walk),
	std_end(N , 72 , 1).



test_desc1(spiral , 'Spiral Tube').
test1(N) :-
	N = spiral,
	start_test(N),
	move_test(60,
		  'annies haven/188.477066/142.809982/81.559509/',
		  'annies haven/181.878403/140.768723/101.555061/'),
	std_end(N , 65 , 1).


/*

	keep this stuff, it's from the old opensim build, but it's a record
	of what tests we were doing

test(3, N) :-
	N= 'Rotating Obstacle',
	start_test(N),
	apiBotClientCmd(teleport('annies haven/137.404724/187.234711/1000.985291/')),
	time_limit(15 , apiBotClientCmd('follow*'('annies haven/139.016434/206.675934/1000.985229/'))),
	needed(_,3,1),
	\+ obstacle(_),
	end_test.


test(6, N) :-
	N= 'narrowest gap we can go through',
	start_test(N),
	apiBotClientCmd(teleport('annies haven/150.241486/131.945526/1000.985291/')),
	time_limit(15 , apiBotClientCmd('follow*'('annies haven/148.898590/146.752121/1000.988281/'))),
	\+ obstacle(_),
	\+ forbidden(_,_,_),
	end_test.

test(6, N) :-
	N= 'tortured prim tube',
	start_test(N),
	apiBotClientCmd(teleport('annies haven/236.392776/245.958130/1000.986572/')),
	time_limit(20 , apiBotClientCmd('follow*'('annies haven/239.544891/232.117767/1000.987122/'))),
	end_test.

test(7, N) :-
	N= 'jagged maze',
	start_test(N),
	apiBotClientCmd(teleport('annies haven/233.436218/221.673218/1000.988770/')),
	time_limit(60 , apiBotClientCmd('follow*'('annies haven/248.193939/190.898941/1000.985291/'))),
	\+ obstacle(_),
	end_test.

*/

tpf_method(GoMethod) :-
	retractall(goMethod(_)),
	asserta(goMethod(GoMethod)),
	cliSet('SimAvatarImpl' , 'UseTeleportFallback' , '@'(false)),
%	clause(testpathfind:test(Name) , _),
	test_desc(Name , Desc),
        'format'('~n~ndoing test: ~q',[test_desc(Name , Desc)]),
	doTest(Name , testpathfind:test(Name) , Results),
	ppTest([name(Name),
		desc(Desc) ,
		results(Results) ,
		option('goMethod ' , GoMethod)]),
	fail.

tpf_method(_) :- !.

tpf :-
	member(Method , [astargoto /* 'follow*' astargoto*/]),
	tpf_method(Method),
	fail.

tpf :- !.

tpfi :- tpf(island_hop).
tpf1 :- repeat,once(tpf(island_hop)),sleep(10),fail.
tpf2 :- repeat,once(tpf),sleep(10),fail.


%% example: ?- tpf(clear).
tpf(Name) :-
        goMethod(GoMethod),
	cliSet('SimAvatarImpl' , 'UseTeleportFallback' , '@'(false)),
	test_desc(Name , Desc),
	doTest(Name , testpathfind:test(Name) , Results),
	ppTest([name(Name),
		desc(Desc) ,
		results(Results) ,
		option('goMethod ' , GoMethod)]).


 makePipe(S,E):-toLocalVect(S,v3(SX,SY,SZ)),toLocalVect(E,v3(EX,EY,EZ)),
    sformat(SF,'~w,~w,~w,~w,~w,~w,~w,~w,~w',[255,0,0,SX,SY,SZ,EX,EY,EZ]),
    botClient(BC),cliCall(BC,talk(SF,100,'Normal'),_).

end_of_file.

 botClient(X),cliCall(X,talk(hi),V)

