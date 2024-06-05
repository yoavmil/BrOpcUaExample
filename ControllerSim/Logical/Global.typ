
TYPE
	Enum1 : 
		(
		Option1,
		Option2
		);
	Struct2 : 	STRUCT 
		usint : USINT;
	END_STRUCT;
	Struct1 : 	STRUCT 
		enum1 : Enum1;
		inner_struct : Struct2;
		float : REAL;
		str : STRING[80];
		int_array : ARRAY[0..9]OF USINT;
	END_STRUCT;
END_TYPE
