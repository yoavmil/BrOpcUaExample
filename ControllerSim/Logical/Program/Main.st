
PROGRAM _INIT

	gCounter:=0;
	flag := TRUE;
	
	// just fill in some random data to read it at the PC side later
	struct1.enum1 := Option2;
	struct1.myFloat := 0.123;
	struct1.str := WSTRING_TO_STRING("FOO-BAR");
	FOR gCounter := 1 TO 9 DO
		struct1.int_array[gCounter] := struct1.int_array[gCounter] + gCounter;
	END_FOR;
	struct1.inner_struct.myByte := 100;
	struct1.inner_struct.myFloat := 3.1415;
	
	gCounter := 0;
	
	e1 := Option2;
	
	struct2.myByte := 42;
	struct2.myFloat := 2.345;
END_PROGRAM

PROGRAM _CYCLIC
	IF flag = TRUE THEN
		gCounter := gCounter + 1;		
	END_IF
END_PROGRAM

PROGRAM _EXIT
	(* Insert code here *)
	 
END_PROGRAM

