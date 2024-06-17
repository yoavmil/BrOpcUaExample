using System.Text.Json.Serialization;

namespace TDOs
{
    // these classes reflect the global structs at the PLC,
    // and they must have the same names

    /*
     * 

    TYPE
	    Enum1 : 
		    (
		    Option1,
		    Option2
		    );
	    Struct2 : 	STRUCT 
		    myFloat : REAL;
		    myByte : USINT;
	    END_STRUCT;
	    Struct1 : 	STRUCT 
		    enum1 : Enum1;
		    inner_struct : Struct2;
		    myFloat : REAL;
		    str : STRING[80];
		    int_array : ARRAY[0..9]OF USINT;
	    END_STRUCT;
    END_TYPE

     * 
     */

    public enum Enum1
    {
        Option1_0, // note that I need to append the index "_0" to the name
        Option2_1
    }
    
    public class Struct1
    {
        public Enum1 enum1 { get; set; }
        public Struct2 inner_struct { get; set; } = new Struct2();
        public float myFloat { get; set; }
        public string str { get; set; } = "";
        public byte[] int_array { get; set; } = new byte[10];
    }

    public class Struct2
    {
        public float myFloat { get; set; }
        public byte myByte { get; set; }
    }
}
