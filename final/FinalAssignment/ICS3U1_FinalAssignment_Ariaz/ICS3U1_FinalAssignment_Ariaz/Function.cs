/*
 * Function class - simple data structure
 * June 31st - Rudy Ariaz
 * Stores information about the parameters of a function (type of function, transformations applied, etc.)
 * Represents a function to be plotted. 
 */

using System.Collections.Generic;

namespace ICS3U1_FinalAssignment_Ariaz
{
    class Function
    {
        // Type of function
        private string desc;
        // Transformation parameters
        private int a, b, h, k;
        // Extra parameters used in some functions (bases of exponentials, exponents of power functions)
        private int additional;
        // Specific type of some functions (sin, cos, tan for trigonometric functions, various logarithms)
        private string specialType;

        // Constructor for trigonometric and logarithmic functions
        // specialType represents the specific type of trigonometric/logarithmic function
        public Function(int a, int b, int h, int k, string type, string specialType)
        {
            // Update instance variables
            this.a = a;
            this.b = b;
            this.h = h;
            this.k = k;
            desc = type;
            this.specialType = specialType;
        }

        // Constructor for exponential and power functions (base/exponent)
        public Function(int a, int b, int h, int k, string type, int optional)
        {
            // Update instance variables
            this.a = a;
            this.b = b;
            this.h = h;
            this.k = k;
            desc = type;
            additional = optional;
        }

        // Accessor to get the type of the function
        // Not named GetType to not override default method Object.GetType
        public string GetDesc() { return desc; }

        // Returns a dictionary associating each transformation to a letter that represents the transformation
        public Dictionary<string, int> GetTransforms()
        {
            return new Dictionary<string, int> { { "a", a }, { "b", b }, { "h", h }, { "k", k } };
        }

        // Gets the additional parameter of the function
        public int GetAdditional() { return additional; }

        // Gets the special type of the function
        public string GetSpecialType()
        {
            return specialType;
        }
    }
}
