run_manual:
	reset && dotnet run new.belt true 10

run_auto:
	reset && dotnet run new.belt false 10 -no-display

run_test:
	reset && dotnet run v_bugs.belt false 500
	reset && dotnet run loop1.belt false 500 -no-display
	reset && dotnet run test2.belt false 100 -containers=600
