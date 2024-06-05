
PROGRAM _INIT
	(* Insert code here *)
	gCounter:=0;
	flag :=TRUE;
	struct1.enum1 := Option1;
	struct1.float := 0.123;
	e1 := Option1;
END_PROGRAM

PROGRAM _CYCLIC
	IF flag = TRUE THEN
		gCounter := gCounter + 1;		
	END_IF
END_PROGRAM

PROGRAM _EXIT
	(* Insert code here *)
	 
END_PROGRAM

